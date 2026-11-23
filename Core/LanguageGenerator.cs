using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// 收集语言文本，导出 language.json 与 TS 端的 _language 访问器
/// </summary>
public class LanguageGenerator
{
    private readonly Dictionary<string, string> _texts = new();
    private int _nextId = 1;

    public void CollectLanguages(List<SheetData> sheets, int startId = 1)
    {
        _nextId = startId;

        foreach (var sheet in sheets)
        {
            // 参数表在读取阶段已把 language 单元格替换成 key
            foreach (var entry in sheet.LanguageEntries)
            {
                _texts[entry.Key] = entry.Value;
            }

            if (sheet.Kind == TableKind.Param)
            {
                continue;
            }

            foreach (var field in sheet.Fields.Where(f => f.IsLanguage))
            {
                foreach (var row in sheet.Rows)
                {
                    if (!row.TryGetValue(field.Name, out var value))
                    {
                        continue;
                    }

                    var text = value?.ToString();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    var key = $"{_nextId++}";
                    _texts[key] = text;
                    row[field.Name] = key;
                }
            }
        }
    }

    public async Task ExportLanguageJson(string outputPath)
    {
        var json = JsonSerializer.Serialize(_texts, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        await FileUtil.WriteTextAsync(outputPath, json);
        Logger.Success($"生成语言文件: {outputPath}");
    }

    public async Task GenerateLanguageClass(string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("/***");
        sb.AppendLine("* 该文件由工具自动生成，请勿手动修改");
        sb.AppendLine("*/");
        sb.AppendLine("export class _language {");
        sb.AppendLine("    private _texts: Record<string, string> = {};");
        sb.AppendLine();
        sb.AppendLine("    public load(texts: Record<string, string>): void {");
        sb.AppendLine("        this._texts = texts || {};");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 取多语言文本，缺失时回退为 key 本身 */");
        sb.AppendLine("    public t(key: string): string {");
        sb.AppendLine("        if (key === null || key === undefined) {");
        sb.AppendLine("            return \"\";");
        sb.AppendLine("        }");
        sb.AppendLine("        const text = this._texts[key];");
        sb.AppendLine("        return text === undefined ? key : text;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /** 把文本中的 {0} {1} 替换为参数 */");
        sb.AppendLine("    public replaceTemplate(template: string, ...values: any[]): string {");
        sb.AppendLine("        return template.replace(/\\{(\\d+)\\}/g, (match, index) => {");
        sb.AppendLine("            const idx = parseInt(index);");
        sb.AppendLine("            return idx < values.length ? String(values[idx]) : match;");
        sb.AppendLine("        });");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export const _lang = new _language();");

        await FileUtil.WriteTextAsync(outputPath, sb.ToString());
        Logger.Success($"生成语言类: {outputPath}");
    }
}
