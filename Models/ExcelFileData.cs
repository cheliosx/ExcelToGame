namespace ExcelToGame.Models;

public class ExcelFileData
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OutputName { get; set; } = string.Empty;
    public SheetData? MainSheet { get; set; }
    public List<SheetData> DataSheets { get; set; } = new();
    public List<SheetDefinition> SheetDefinitions { get; set; } = new();
}

public class SheetDefinition
{
    public string File { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Client { get; set; } = true;
    public bool Server { get; set; } = true;
    public string Type { get; set; } = string.Empty;
    public string? Extend { get; set; }
}
