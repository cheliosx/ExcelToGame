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

/// <summary>
/// main 页签中的一行页签定义
/// </summary>
public class SheetDefinition
{
    /// <summary>英文页签名，同时作为导出的类型名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Excel 中实际的页签名（中文）</summary>
    public string ChineseName { get; set; } = string.Empty;

    public bool Client { get; set; } = true;
    public bool Server { get; set; } = true;
    public string Type { get; set; } = string.Empty;
}
