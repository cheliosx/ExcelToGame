namespace ExcelToGame.Models;

/// <summary>
/// 表格类型枚举
/// </summary>
public enum TableType
{
    /// <summary>
    /// 参数表 - 单条配置（如全局参数）
    /// </summary>
    Param,
    
    /// <summary>
    /// 枚举表 - 枚举定义
    /// </summary>
    Enum,
    
    /// <summary>
    /// 主键表 - 以ID为主键的配置表
    /// </summary>
    PrimaryKey,
    
    /// <summary>
    /// 数组表 - 数组结构配置
    /// </summary>
    Array,
    
    /// <summary>
    /// 分组表 - 按组分类的配置
    /// </summary>
    Group
}

/// <summary>
/// 表格数据模型
/// </summary>
public class TableData
{
    /// <summary>
    /// 表格文件名（不含扩展名，如 Item[道具]）
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// 表格类名（从文件名解析，如 Item）
    /// </summary>
    public string ClassName { get; set; } = string.Empty;
    
    /// <summary>
    /// 表格中文名（从文件名解析，如 道具）
    /// </summary>
    public string? ChineseName { get; set; }
    
    /// <summary>
    /// 父表名（继承关系，格式：子表:父表）
    /// </summary>
    public string? ParentTableName { get; set; }
    
    /// <summary>
    /// 表格类型（参数表/枚举表/主键表/数组表/分组表）
    /// </summary>
    public TableType TableType { get; set; } = TableType.PrimaryKey;
    
    /// <summary>
    /// 页签名称（如 main、skillData、buffData 等）
    /// </summary>
    public string SheetName { get; set; } = "main";
    
    /// <summary>
    /// 是否导出到客户端
    /// </summary>
    public bool ExportToClient { get; set; } = true;
    
    /// <summary>
    /// 是否导出到服务端
    /// </summary>
    public bool ExportToServer { get; set; } = true;
    
    /// <summary>
    /// 字段定义列表
    /// </summary>
    public List<FieldInfo> Fields { get; set; } = new();
    
    /// <summary>
    /// 数据行列表
    /// </summary>
    public List<RowData> Rows { get; set; } = new();
    
    /// <summary>
    /// 解析错误列表
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// 解析警告列表
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// 是否解析成功
    /// </summary>
    public bool IsValid { get; set; } = true;
    
    /// <summary>
    /// 获取主键字段
    /// </summary>
    public FieldInfo? GetPrimaryKeyField()
    {
        return Fields.FirstOrDefault(f => f.IsPrimaryKey);
    }
    
    /// <summary>
    /// 获取导出开关字段
    /// </summary>
    public FieldInfo? GetExportSwitchField()
    {
        return Fields.FirstOrDefault(f => f.IsExportSwitch);
    }
    
    /// <summary>
    /// 获取非开关字段列表
    /// </summary>
    public List<FieldInfo> GetDataFields()
    {
        return Fields.Where(f => !f.IsExportSwitch).ToList();
    }
    
    /// <summary>
    /// 获取导出开关为true的数据行
    /// </summary>
    public List<RowData> GetExportableRows()
    {
        return Rows.Where(r => r.ShouldExport).ToList();
    }
    
    /// <summary>
    /// 获取枚举字段列表
    /// </summary>
    public List<FieldInfo> GetEnumFields()
    {
        return Fields.Where(f => f.IsEnum).ToList();
    }
    
    /// <summary>
    /// 获取自定义类型字段列表
    /// </summary>
    public List<FieldInfo> GetCustomTypeFields()
    {
        return Fields.Where(f => f.IsCustomType).ToList();
    }
    
    /// <summary>
    /// 添加错误信息
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }
    
    /// <summary>
    /// 添加警告信息
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}
