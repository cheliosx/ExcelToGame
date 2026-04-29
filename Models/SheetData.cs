namespace ExcelToGame.Models;

public enum TableType
{
    Normal,
    Config,
    Aspect,
    Enum
}

public class LanguageEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public int RowId { get; set; }
    public string FieldName { get; set; } = string.Empty;
}

public class SheetData
{
    public string SheetName { get; set; } = string.Empty;
    public string ChineseName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public TableType TableType { get; set; } = TableType.Normal;
    public string? Extend { get; set; }
    public bool ExportToClient { get; set; } = true;
    public bool ExportToServer { get; set; } = true;
    
    public List<FieldInfo> Fields { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<LanguageEntry> LanguageEntries { get; set; } = new();
    
    public bool IsConfigTable => SheetName.EndsWith("Config", StringComparison.OrdinalIgnoreCase);
    public bool IsAspectTable => SheetName.EndsWith("Aspect", StringComparison.OrdinalIgnoreCase);
    public bool IsEnumTable => ChineseName.Contains("枚举") || SheetName.EndsWith("Type", StringComparison.OrdinalIgnoreCase);
}
