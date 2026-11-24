namespace ExcelToGame.Models;

/// <summary>
/// 表格字段定义（数据页签第三行）
/// </summary>
public class FieldInfo
{
    public string Name { get; set; } = string.Empty;

    /// <summary>中文注释</summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>原始类型声明，如 number / language / array&lt;example.item&gt;</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Excel 列下标（仅读取阶段使用）</summary>
    public int ColumnIndex { get; set; }

    public bool ExportToClient { get; set; } = true;
    public bool ExportToServer { get; set; } = true;

    /// <summary>该字段是否进目标端</summary>
    public bool IsExportedTo(ExportTarget target) =>
        target == ExportTarget.Server ? ExportToServer : ExportToClient;

    public bool IsLanguage => Type.Equals("language", StringComparison.OrdinalIgnoreCase);

    /// <summary>是否为数组声明（array&lt;T&gt; / T[] / []）</summary>
    public bool IsArray =>
        Type is "[]" or "array" ||
        (Type.StartsWith("array<", StringComparison.OrdinalIgnoreCase) && Type.EndsWith('>')) ||
        Type.EndsWith("[]", StringComparison.OrdinalIgnoreCase);
}
