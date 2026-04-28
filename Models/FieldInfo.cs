namespace ExcelToGame.Models;

/// <summary>
/// 字段信息模型
/// </summary>
public class FieldInfo
{
    /// <summary>
    /// 字段中文注释
    /// </summary>
    public string Comment { get; set; } = string.Empty;
    
    /// <summary>
    /// 英文字段名（变量名）
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 原始类型字符串
    /// </summary>
    public string RawType { get; set; } = string.Empty;
    
    /// <summary>
    /// 列索引
    /// </summary>
    public int ColumnIndex { get; set; }
    
    /// <summary>
    /// 是否是数组类型
    /// </summary>
    public bool IsArray { get; set; }
    
    /// <summary>
    /// 是否是枚举类型
    /// </summary>
    public bool IsEnum { get; set; }
    
    /// <summary>
    /// 枚举名称（如果是枚举类型）
    /// </summary>
    public string? EnumName { get; set; }
    
    /// <summary>
    /// 是否是自定义对象类型
    /// </summary>
    public bool IsCustomType { get; set; }
    
    /// <summary>
    /// 自定义类型名称
    /// </summary>
    public string? CustomTypeName { get; set; }
    
    /// <summary>
    /// 基础类型（去除数组标记后的类型）
    /// </summary>
    public string BaseType { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否是主键字段（约定：字段名为Id或ID）
    /// </summary>
    public bool IsPrimaryKey { get; set; }
    
    /// <summary>
    /// 是否是导出开关列
    /// </summary>
    public bool IsExportSwitch { get; set; }
    
    /// <summary>
    /// 获取C#类型字符串
    /// </summary>
    public string GetCSharpType()
    {
        var type = BaseType.ToLower() switch
        {
            "int" => "int",
            "long" => "long",
            "float" => "float",
            "double" => "double",
            "bool" => "bool",
            "boolean" => "bool",
            "string" => "string",
            _ => IsEnum ? EnumName! : IsCustomType ? CustomTypeName! : "string"
        };
        
        return IsArray ? $"List<{type}>" : type;
    }
    
    /// <summary>
    /// 获取TypeScript类型字符串
    /// </summary>
    public string GetTypeScriptType()
    {
        var type = BaseType.ToLower() switch
        {
            "int" => "number",
            "long" => "number",
            "float" => "number",
            "double" => "number",
            "bool" => "boolean",
            "boolean" => "boolean",
            "string" => "string",
            _ => IsEnum ? EnumName! : IsCustomType ? CustomTypeName! : "string"
        };
        
        return IsArray ? $"{type}[]" : type;
    }
    
    /// <summary>
    /// 获取JSON类型字符串
    /// </summary>
    public string GetJsonType()
    {
        return BaseType.ToLower() switch
        {
            "int" => "integer",
            "long" => "integer",
            "float" => "number",
            "double" => "number",
            "bool" => "boolean",
            "boolean" => "boolean",
            "string" => "string",
            _ => IsEnum ? "string" : IsCustomType ? "object" : "string"
        };
    }
    
    /// <summary>
    /// 验证字段类型是否合法
    /// </summary>
    public bool ValidateType(List<string> errors)
    {
        var validBaseTypes = new[] { "int", "long", "float", "double", "bool", "boolean", "string" };
        
        if (IsEnum || IsCustomType)
        {
            return true;
        }
        
        if (!validBaseTypes.Contains(BaseType.ToLower()))
        {
            errors.Add($"字段 '{Name}' 的类型 '{RawType}' 不合法，支持的基础类型: {string.Join(", ", validBaseTypes)}");
            return false;
        }
        
        return true;
    }
}
