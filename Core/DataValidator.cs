using ExcelToGame.Models;

namespace ExcelToGame.Core;

/// <summary>
/// 数据校验模块 - 验证表格数据
/// </summary>
public class DataValidator
{
    /// <summary>
    /// 校验表格数据
    /// </summary>
    public bool Validate(TableData tableData)
    {
        var isValid = true;
        
        // 1. 字段名校重检测
        if (!ValidateDuplicateFieldNames(tableData))
        {
            isValid = false;
        }
        
        // 2. 类型合法性校验
        if (!ValidateFieldTypes(tableData))
        {
            isValid = false;
        }
        
        // 3. 主键重复校验
        if (!ValidatePrimaryKeyDuplicates(tableData))
        {
            isValid = false;
        }
        
        // 4. 数据行校验
        if (!ValidateDataRows(tableData))
        {
            isValid = false;
        }
        
        tableData.IsValid = isValid;
        return isValid;
    }
    
    /// <summary>
    /// 校验字段名重复
    /// </summary>
    private bool ValidateDuplicateFieldNames(TableData tableData)
    {
        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasDuplicate = false;
        
        foreach (var field in tableData.Fields)
        {
            if (!fieldNames.Add(field.Name))
            {
                tableData.AddError($"字段名重复: {field.Name}");
                hasDuplicate = true;
            }
        }
        
        return !hasDuplicate;
    }
    
    /// <summary>
    /// 校验字段类型
    /// </summary>
    private bool ValidateFieldTypes(TableData tableData)
    {
        var isValid = true;
        var typeParser = new TypeParser();
        
        foreach (var field in tableData.Fields)
        {
            if (!typeParser.IsValidType(field.RawType, out var error))
            {
                tableData.AddError($"字段 '{field.Name}' 类型错误: {error}");
                isValid = false;
            }
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 校验主键重复
    /// </summary>
    private bool ValidatePrimaryKeyDuplicates(TableData tableData)
    {
        var pkField = tableData.GetPrimaryKeyField();
        if (pkField == null)
        {
            // 没有主键字段，不校验
            return true;
        }
        
        var pkValues = new HashSet<object?>();
        var hasDuplicate = false;
        
        foreach (var row in tableData.GetExportableRows())
        {
            var pkValue = row.PrimaryKeyValue;
            
            if (pkValue == null)
            {
                tableData.AddError($"第 {row.RowNumber} 行主键值为空");
                hasDuplicate = true;
                continue;
            }
            
            if (!pkValues.Add(pkValue))
            {
                tableData.AddError($"第 {row.RowNumber} 行主键值重复: {pkValue}");
                hasDuplicate = true;
            }
        }
        
        return !hasDuplicate;
    }
    
    /// <summary>
    /// 校验数据行
    /// </summary>
    private bool ValidateDataRows(TableData tableData)
    {
        var isValid = true;
        
        foreach (var row in tableData.GetExportableRows())
        {
            foreach (var field in tableData.GetDataFields())
            {
                var value = row.GetValue(field.Name);
                
                // 校验数字格式
                if (!ValidateNumberFormat(value, field, row.RowNumber, tableData))
                {
                    isValid = false;
                }
                
                // 校验数组格式
                if (field.IsArray && value is List<object?> arrayValue)
                {
                    if (!ValidateArrayFormat(arrayValue, field, row.RowNumber, tableData))
                    {
                        isValid = false;
                    }
                }
            }
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 校验数字格式
    /// </summary>
    private bool ValidateNumberFormat(object? value, FieldInfo field, int rowNumber, TableData tableData)
    {
        if (value == null)
        {
            return true;
        }
        
        var baseType = field.BaseType.ToLower();
        
        if (baseType == "int" && value is not int)
        {
            tableData.AddError($"第 {rowNumber} 行，字段 '{field.Name}' 应为整数类型");
            return false;
        }
        
        if (baseType == "long" && value is not long)
        {
            tableData.AddError($"第 {rowNumber} 行，字段 '{field.Name}' 应为长整数类型");
            return false;
        }
        
        if (baseType == "float" && value is not float)
        {
            tableData.AddError($"第 {rowNumber} 行，字段 '{field.Name}' 应为浮点数类型");
            return false;
        }
        
        if (baseType == "double" && value is not double)
        {
            tableData.AddError($"第 {rowNumber} 行，字段 '{field.Name}' 应为双精度浮点数类型");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 校验数组格式
    /// </summary>
    private bool ValidateArrayFormat(List<object?> array, FieldInfo field, int rowNumber, TableData tableData)
    {
        // 数组元素类型校验
        var baseType = field.BaseType.ToLower();
        
        for (int i = 0; i < array.Count; i++)
        {
            var item = array[i];
            if (item == null) continue;
            
            if (baseType == "int" && item is not int)
            {
                tableData.AddError($"第 {rowNumber} 行，字段 '{field.Name}' 第 {i + 1} 个数组元素应为整数类型");
                return false;
            }
            
            if (baseType == "float" && item is not float)
            {
                tableData.AddError($"第 {rowNumber} 行，字段 '{field.Name}' 第 {i + 1} 个数组元素应为浮点数类型");
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 校验枚举值
    /// </summary>
    public bool ValidateEnumValue(string enumName, string value, HashSet<string> validValues, TableData tableData, int rowNumber, string fieldName)
    {
        if (!validValues.Contains(value))
        {
            tableData.AddError($"第 {rowNumber} 行，字段 '{fieldName}' 的枚举值 '{value}' 不合法，有效值: {string.Join(", ", validValues)}");
            return false;
        }
        
        return true;
    }
}
