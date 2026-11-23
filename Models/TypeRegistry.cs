namespace ExcelToGame.Models;

/// <summary>
/// 全部页签的类型登记表：把字段声明的类型字符串解析成可判定的类型
/// </summary>
public sealed class TypeRegistry
{
    private readonly Dictionary<string, SheetData> _sheets = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IEnumerable<SheetData> sheets)
    {
        foreach (var sheet in sheets)
        {
            _sheets[sheet.SheetName] = sheet;
        }
    }

    public SheetData? Find(string name) =>
        _sheets.TryGetValue(NormalizeName(name), out var sheet) ? sheet : null;

    public ResolvedType Resolve(FieldInfo field) => Resolve(field.Type);

    public ResolvedType Resolve(string rawType)
    {
        var type = (rawType ?? string.Empty).Trim();

        // 只写 [] / array 表示"元素类型不限的数组"
        if (type is "[]" or "array" or "array<>")
        {
            return new ResolvedType(ValueKind.Any, string.Empty, true);
        }

        var isArray = false;
        if (type.StartsWith("array<", StringComparison.OrdinalIgnoreCase) && type.EndsWith('>'))
        {
            isArray = true;
            type = type.Substring(6, type.Length - 7).Trim();
        }
        else if (type.EndsWith("[]", StringComparison.OrdinalIgnoreCase))
        {
            isArray = true;
            type = type[..^2].Trim();
        }

        if (type.Length == 0)
        {
            return new ResolvedType(ValueKind.Any, string.Empty, isArray);
        }

        return ResolveScalar(type) with { IsArray = isArray };
    }

    private ResolvedType ResolveScalar(string type)
    {
        switch (type.ToLowerInvariant())
        {
            case "number":
            case "int":
                return new ResolvedType(ValueKind.Number, "int");
            case "long":
                return new ResolvedType(ValueKind.Number, "long");
            case "float":
                return new ResolvedType(ValueKind.Number, "float");
            case "double":
                return new ResolvedType(ValueKind.Number, "double");
            case "bool":
            case "boolean":
                return new ResolvedType(ValueKind.Bool);
            case "string":
                return new ResolvedType(ValueKind.String);
            case "language":
                return new ResolvedType(ValueKind.Language);
            case "any":
            case "object":
                return new ResolvedType(ValueKind.Any);
        }

        var name = NormalizeName(type);
        var sheet = Find(name);
        if (sheet == null)
        {
            return new ResolvedType(ValueKind.Unknown, name);
        }

        return sheet.Kind == TableKind.Enum
            ? new ResolvedType(ValueKind.Enum, sheet.SheetName)
            : new ResolvedType(ValueKind.RowRef, sheet.SheetName);
    }

    /// <summary>去掉 enum_ 前缀与 example. 命名空间前缀</summary>
    private static string NormalizeName(string type)
    {
        var name = type.Trim();

        if (name.StartsWith("enum_", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(5);
        }

        var dot = name.LastIndexOf('.');
        if (dot >= 0)
        {
            name = name[(dot + 1)..];
        }

        return name;
    }
}
