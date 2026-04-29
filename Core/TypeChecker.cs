using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

public class TypeChecker
{
    private readonly HashSet<string> _definedTypes = new();
    private readonly List<string> _errors = new();
    
    public void RegisterTypes(List<SheetData> sheets)
    {
        foreach (var sheet in sheets)
        {
            _definedTypes.Add(sheet.SheetName);
            
            if (sheet.SheetName.Contains("_"))
            {
                var parts = sheet.SheetName.Split('_');
                if (parts.Length >= 2)
                {
                    _definedTypes.Add($"example.{parts[0]}");
                }
            }
        }
        
        _definedTypes.Add("number");
        _definedTypes.Add("int");
        _definedTypes.Add("float");
        _definedTypes.Add("double");
        _definedTypes.Add("string");
        _definedTypes.Add("bool");
        _definedTypes.Add("boolean");
        _definedTypes.Add("language");
    }
    
    public bool CheckTypes(List<SheetData> sheets)
    {
        bool hasError = false;
        
        foreach (var sheet in sheets)
        {
            foreach (var field in sheet.Fields)
            {
                if (field.IsCustomType)
                {
                    if (!IsTypeDefined(field.Type))
                    {
                        Logger.Error($"表 {sheet.SheetName} 字段 {field.Name} 使用了未定义类型 {field.Type}");
                        hasError = true;
                    }
                }
                else if (field.IsArray)
                {
                    var elementType = field.GetArrayElementType();
                    if (elementType.Contains(".") && !IsTypeDefined(elementType))
                    {
                        Logger.Error($"表 {sheet.SheetName} 字段 {field.Name} 使用了未定义数组元素类型 {elementType}");
                        hasError = true;
                    }
                }
            }
        }
        
        return !hasError;
    }
    
    private bool IsTypeDefined(string type)
    {
        if (_definedTypes.Contains(type))
            return true;
        
        if (type.StartsWith("example."))
        {
            var shortName = type.Substring(8);
            
            foreach (var definedType in _definedTypes)
            {
                if (definedType.StartsWith(shortName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        
        return false;
    }
    
    public List<string> GetErrors() => _errors;
}
