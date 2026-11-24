namespace ExcelToGame.Models;

/// <summary>
/// 表格类型（main 页签的 type 列）
/// </summary>
public enum TableKind
{
    /// <summary>主键表：以 Id 为键，一行一个实体</summary>
    Keyed,

    /// <summary>数组表：按下标顺序访问的列表</summary>
    Array,

    /// <summary>分组表：按第一个字段分组，组内多行</summary>
    Group,

    /// <summary>参数表：key/value 转置，只有一行配置</summary>
    Param,

    /// <summary>枚举表：key/value 定义枚举项</summary>
    Enum
}

public class EnumEntry
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class SheetData
{
    public string SheetName { get; set; } = string.Empty;
    public string ChineseName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public TableKind Kind { get; set; } = TableKind.Keyed;

    public bool ExportToClient { get; set; } = true;
    public bool ExportToServer { get; set; } = true;

    public List<FieldInfo> Fields { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<EnumEntry> EnumEntries { get; set; } = new();

    /// <summary>该表是否导出到目标端（main 页签的 client / server 标记）</summary>
    public bool IsExportedTo(ExportTarget target) =>
        target == ExportTarget.Server ? ExportToServer : ExportToClient;

    /// <summary>目标端可见字段（按表内声明顺序）</summary>
    public IEnumerable<FieldInfo> FieldsFor(ExportTarget target) => Fields.Where(f => f.IsExportedTo(target));

    /// <summary>主键字段：名为 Id 的字段优先，否则取第一个字段</summary>
    public FieldInfo? KeyField =>
        Fields.FirstOrDefault(f => f.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
        ?? Fields.FirstOrDefault();
}
