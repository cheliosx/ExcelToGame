namespace ExcelToGame.Models;

/// <summary>
/// 字段类型解析结果
/// </summary>
public enum ValueKind
{
    Number,
    Bool,
    String,
    Language,

    /// <summary>引用枚举表</summary>
    Enum,

    /// <summary>引用其它表的行（导出为 id）</summary>
    RowRef,

    /// <summary>不限类型（[] / any / object）</summary>
    Any,

    Unknown
}

public sealed record ResolvedType(ValueKind Kind, string Name = "", bool IsArray = false);

/// <summary>
/// 导出表中的一列
/// </summary>
public sealed class SchemaColumn
{
    public required FieldInfo Field { get; init; }

    /// <summary>列在行数组中的下标</summary>
    public required int Index { get; init; }

    public required ResolvedType Type { get; init; }

    public string Name => Field.Name;
}

/// <summary>
/// 一张表的导出结构：一个页签一张表
/// </summary>
public sealed class TableSchema
{
    public required SheetData Sheet { get; init; }
    public required TableKind Kind { get; init; }
    public required List<SchemaColumn> Columns { get; init; }
    public required List<object?[]> Rows { get; init; }

    public string Name => Sheet.SheetName;

    /// <summary>主键列（主键表/分组表才有）</summary>
    public SchemaColumn? KeyColumn { get; init; }
}
