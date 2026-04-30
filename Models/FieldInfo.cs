namespace ExcelToGame.Models;

public class FieldInfo
{
    public string Name { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }
    public bool ExportToClient { get; set; } = true;
    public bool ExportToServer { get; set; } = true;
    
    public bool IsLanguage => Type.Equals("language", StringComparison.OrdinalIgnoreCase);
    public bool IsArray => Type.StartsWith("array<", StringComparison.OrdinalIgnoreCase) && Type.EndsWith(">");
    public bool IsCustomType => Type.Contains(".") && !IsArray;
    
    public string GetArrayElementType()
    {
        if (!IsArray) return Type;
        return Type.Substring(6, Type.Length - 7);
    }
    
    public string GetTSType()
    {
        if (IsLanguage) return "string";
        if (IsArray) return "any[]";
        
        return Type.ToLower() switch
        {
            "number" or "int" or "float" or "double" => "number",
            "string" => "string",
            "bool" or "boolean" => "boolean",
            _ => "any"
        };
    }
    
    public string GetCSharpType()
    {
        if (IsLanguage) return "string";
        if (IsArray) return "List<object>";
        
        return Type.ToLower() switch
        {
            "number" or "int" => "int",
            "float" => "float",
            "double" => "double",
            "string" => "string",
            "bool" or "boolean" => "bool",
            _ => "object"
        };
    }
}
