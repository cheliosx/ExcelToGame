namespace ExcelToGame.Models;

/// <summary>
/// 表格数据模型
/// </summary>
public class TableData
{
    /// <summary>
    /// 表格文件名（不含扩展名）
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// 表格类名（从文件名解析）
    /// </summary>
    public string ClassName { get; set; } = string.Empty;
    
    /// <summary>
    /// 父表名（继承关系，格式：子表:父表）
    /// </summary>
    public string? ParentTableName { get; set; }
    
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
