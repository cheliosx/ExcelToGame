namespace ExcelToGame.Models;

/// <summary>
/// 行数据模型
/// </summary>
public class RowData
{
    /// <summary>
    /// 行号（1-based，用于错误提示）
    /// </summary>
    public int RowNumber { get; set; }
    
    /// <summary>
    /// 是否导出（第一列开关）
    /// </summary>
    public bool ShouldExport { get; set; }
    
    /// <summary>
    /// 字段值字典
    /// </summary>
    public Dictionary<string, object?> Values { get; set; } = new();
    
    /// <summary>
    /// 主键值
    /// </summary>
    public object? PrimaryKeyValue { get; set; }
    
    /// <summary>
    /// 获取指定字段的值
    /// </summary>
    public T? GetValue<T>(string fieldName)
    {
        if (Values.TryGetValue(fieldName, out var value) && value is T t)
        {
            return t;
        }
        return default;
    }
    
    /// <summary>
    /// 获取指定字段的值（非泛型）
    /// </summary>
    public object? GetValue(string fieldName)
    {
        return Values.TryGetValue(fieldName, out var value) ? value : null;
    }
    
    /// <summary>
    /// 设置字段值
    /// </summary>
    public void SetValue(string fieldName, object? value)
    {
        Values[fieldName] = value;
    }
    
    /// <summary>
    /// 检查是否包含指定字段
    /// </summary>
    public bool HasField(string fieldName)
    {
        return Values.ContainsKey(fieldName);
    }
}
