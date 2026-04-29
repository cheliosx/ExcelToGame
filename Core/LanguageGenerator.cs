using System.Text;
using System.Text.Json;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

public class LanguageGenerator
{
    private readonly Dictionary<string, string> _languageDict = new();
    private int _languageId = 1000;
    
    public void CollectLanguages(List<SheetData> sheets)
    {
        foreach (var sheet in sheets)
        {
            foreach (var field in sheet.Fields)
            {
                if (!field.IsLanguage) continue;
                
                foreach (var row in sheet.Rows)
                {
                    if (!row.TryGetValue(field.Name, out var value)) continue;
                    
                    var text = value?.ToString();
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    
                    var key = GenerateLanguageKey(sheet.SheetName, field.Name, row);
                    
                    _languageDict[key] = text;
                    
                    row[$"__lang_key_{field.Name}"] = key;
                }
            }
        }
    }
    
    private string GenerateLanguageKey(string sheetName, string fieldName, Dictionary<string, object?> row)
    {
        if (row.TryGetValue("Id", out var idObj) || row.TryGetValue("id", out idObj) || row.TryGetValue("ID", out idObj))
        {
            var id = idObj?.ToString() ?? _languageId.ToString();
            return $"{sheetName}_{id}_{fieldName}";
        }
        
        return $"Text_{_languageId++}";
    }
    
    public async Task ExportLanguageJson(string outputPath)
    {
        var json = JsonSerializer.Serialize(_languageDict, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        
        await FileUtil.WriteTextAsync(outputPath, json);
        Logger.Success($"??????: {outputPath}");
    }
    
    public async Task GenerateLanguageClass(string outputPath)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("export class _language {");
        sb.AppendLine("    private _data: any;");
        sb.AppendLine();
        sb.AppendLine("    load(_data: any) {");
        sb.AppendLine("        this._data = _data;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    t(id: string): string {");
        sb.AppendLine("        let data = this._data[id];");
        sb.AppendLine("        if (!data) {");
        sb.AppendLine("            data = id;");
        sb.AppendLine("        }");
        sb.AppendLine("        return data;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    replaceTemplate(template: string, ...values: any[]): string {");
        sb.AppendLine("        return template.replace(/\\$\\{(\\d+)\\}/g, (match, index) => {");
        sb.AppendLine("            const idx = parseInt(index);");
        sb.AppendLine("            return idx < values.length ? values[idx] : match;");
        sb.AppendLine("        });");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export const _lang = new _language();");
        
        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生戝语言�? {outputPath}");
    }
    
    public string? GetLanguageKey(Dictionary<string, object?> row, string fieldName)
    {
        if (row.TryGetValue($"__lang_key_{fieldName}", out var key))
        {
            return key?.ToString();
        }
        return null;
    }
}


