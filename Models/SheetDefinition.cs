namespace ExcelToGame.Models;

/// <summary>
/// 页签定义（从main页签解析得到）
/// </summary>
public class SheetDefinition
{
    /// <summary>
    /// 页签名称（如 skillData、buffData）
    /// </summary>
    public string SheetName { get; set; } = string.Empty;
    
    /// <summary>
    /// 页签中文注释
    /// </summary>
    public string Comment { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否导出到客户端（YES/NO）
    /// </summary>
    public bool ExportToClient { get; set; } = true;
    
    /// <summary>
    /// 是否导出到服务端（YES/NO）
    /// </summary>
    public bool ExportToServer { get; set; } = true;
    
    /// <summary>
    /// 表格类型（参数表、枚举表、主键表、数组表、分组表）
    /// </summary>
    public TableType TableType { get; set; } = TableType.PrimaryKey;
    
    /// <summary>
    /// 导出开关（TRUE表示导出此页签）
    /// </summary>
    public bool ShouldExport { get; set; } = true;
}
