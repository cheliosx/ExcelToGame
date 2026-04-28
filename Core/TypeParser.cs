using ExcelToGame.Models;

namespace ExcelToGame.Core;

/// <summary>
/// 类型解析模块 - 解析字段类型
/// </summary>
public class TypeParser
{
    /// <summary>
    /// 解析字段类型
    /// </summary>
    public void ParseFieldType(FieldInfo field)
    {
        var rawType = field.RawType.Trim();
        
        // 检查是否为数组类型（以[]结尾）
        if (rawType.EndsWith("[]"))
        {
            field.IsArray = true;
            rawType = rawType.Substring(0, rawType.Length - 2);
        }
        
        // 检查是否为枚举类型（以enum_开头）
        if (rawType.StartsWith("enum_", StringComparison.OrdinalIgnoreCase))
        {
            field.IsEnum = true;
            field.EnumName = rawType.Substring(5); // 去掉"enum_"前缀
            field.BaseType = "string";
            return;
        }
        
        // 检查是否为自定义类型（首字母大写且不是基础类型）
        var baseTypes = new[] { "int", "long", "float", "double", "bool", "boolean", "string" };
        if (!baseTypes.Contains(rawType.ToLower()) && char.IsUpper(rawType[0]))
        {
            field.IsCustomType = true;
            field.CustomTypeName = rawType;
            field.BaseType = rawType;
            return;
        }
        
        // 基础类型
        field.BaseType = rawType.ToLower() switch
        {
            "boolean" => "bool",
            _ => rawType.ToLower()
        };
    }
    
    /// <summary>
    /// 验证类型是否合法
    /// </summary>
    public bool IsValidType(string type, out string error)
    {
        error = string.Empty;
        
        if (string.IsNullOrWhiteSpace(type))
        {
            error = "类型不能为空";
            return false;
        }
        
        // 去掉数组标记
        var baseType = type;
        if (baseType.EndsWith("[]"))
        {
            baseType = baseType.Substring(0, baseType.Length - 2);
        }
        
        // 检查枚举类型
        if (baseType.StartsWith("enum_", StringComparison.OrdinalIgnoreCase))
        {
            var enumName = baseType.Substring(5);
            if (string.IsNullOrWhiteSpace(enumName))
            {
                error = "枚举名称不能为空";
                return false;
            }
            return true;
        }
        
        // 检查基础类型
        var validTypes = new[] { "int", "long", "float", "double", "bool", "boolean", "string" };
        if (validTypes.Contains(baseType.ToLower()))
        {
            return true;
        }
        
        // 自定义类型（首字母大写）
        if (char.IsUpper(baseType[0]))
        {
            return true;
        }
        
        error = $"未知类型: {type}";
        return false;
    }
    
    /// <summary>
    /// 获取类型的默认值
    /// </summary>
    public object? GetDefaultValue(string type)
    {
        var isArray = type.EndsWith("[]");
        var baseType = isArray ? type.Substring(0, type.Length - 2) : type;
        
        if (isArray)
        {
            return new List<object?>();
        }
        
        return baseType.ToLower() switch
        {
            "int" => 0,
            "long" => 0L,
            "float" => 0f,
            "double" => 0.0,
            "bool" or "boolean" => false,
            "string" => string.Empty,
            _ => null
        };
    }
}
