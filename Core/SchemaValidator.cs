using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// 导出前的结构与类型校验：字段类型、枚举定义、主键一致性。
/// 任何一项不通过都直接中止导出，避免把问题留到运行时。
/// </summary>
public sealed class SchemaValidator
{
    private readonly List<string> _errors = new();

    public TypeRegistry Types { get; } = new();

    public bool Validate(List<ExcelFileData> files, ExportTarget target)
    {
        _errors.Clear();
        // 类型注册用全量页签：客户端表也可能引用只在服务端定义的类型
        Types.Register(files.SelectMany(f => f.DataSheets));

        ValidateUniqueNames(files);

        foreach (var file in files)
        {
            foreach (var sheet in file.DataSheets)
            {
                ValidateTypes(sheet);
                ValidateEnum(sheet);

                // 只校验本次真正要导出的表
                if (sheet.IsExportedTo(target))
                {
                    ValidateKey(sheet, target);
                }
            }
        }

        return _errors.Count == 0;
    }

    /// <summary>所有文件共用同一个 C# 命名空间，页签名不能重复</summary>
    private void ValidateUniqueNames(List<ExcelFileData> files)
    {
        var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            foreach (var sheet in file.DataSheets)
            {
                if (owners.TryGetValue(sheet.SheetName, out var owner))
                {
                    Error($"页签 {sheet.SheetName} 在 {owner} 与 {file.OutputName} 中重复，生成的类型会冲突");
                }
                else
                {
                    owners[sheet.SheetName] = file.OutputName;
                }
            }
        }
    }

    private void ValidateTypes(SheetData sheet)
    {
        foreach (var field in sheet.Fields)
        {
            var resolved = Types.Resolve(field);
            if (resolved.Kind == ValueKind.Unknown)
            {
                Error($"表 {sheet.SheetName}.{field.Name} 使用了未定义类型 '{field.Type}'");
            }
        }
    }

    private void ValidateEnum(SheetData sheet)
    {
        if (sheet.Kind != TableKind.Enum)
        {
            return;
        }

        if (sheet.EnumEntries.Count == 0)
        {
            Error($"枚举表 {sheet.SheetName} 没有任何枚举项");
            return;
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var values = new HashSet<int>();
        foreach (var entry in sheet.EnumEntries)
        {
            if (!names.Add(entry.Name))
            {
                Error($"枚举表 {sheet.SheetName} 的枚举名重复: {entry.Name}");
            }

            if (!values.Add(entry.Value))
            {
                Error($"枚举表 {sheet.SheetName} 的枚举值重复: {entry.Value}");
            }
        }
    }

    /// <summary>按主键/分组键访问的表，键列必须导出到目标端</summary>
    private void ValidateKey(SheetData sheet, ExportTarget target)
    {
        if (sheet.Kind is not (TableKind.Keyed or TableKind.Group))
        {
            return;
        }

        var key = sheet.KeyField;
        if (key == null)
        {
            Error($"表 {sheet.SheetName} 没有任何字段，无法确定主键");
            return;
        }

        if (!key.IsExportedTo(target))
        {
            Error($"表 {sheet.SheetName} 的主键 {key.Name} 未导出到{target.ToDisplayName()}，无法生成按 id 访问的容器");
        }

        // 主键/分组键用于二分查找与懒加载索引，必须是数值列
        var kind = Types.Resolve(key).Kind;
        if (kind is not (ValueKind.Number or ValueKind.Enum or ValueKind.RowRef))
        {
            Error($"表 {sheet.SheetName} 的主键 {key.Name} 必须是数字类型（number/int/枚举），当前为 '{key.Type}'");
        }
    }

    private void Error(string message)
    {
        _errors.Add(message);
        Logger.Error(message);
    }
}
