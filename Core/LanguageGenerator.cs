using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// 收集语言文本，导出 language.json 与 TS 端的 _language 访问器。
///
/// key 以「文本内容」为身份：重新生成时优先复用上一次 language.json 里的 key，
/// 保证没有改动文本的条目 id 不变；本次没扫到的旧条目不会写进新文件（等于自动清理）。
/// </summary>
public class LanguageGenerator
{
    /// <summary>多语言产物目录</summary>
    public const string Directory = "i10n";

    /// <summary>默认语言文件名（目前表格里填的原文就是简体中文）</summary>
    public const string DefaultFileName = "zh-Hans.json";

    /// <summary>本次导出实际用到的 key -> 文本</summary>
    private readonly Dictionary<string, string> _texts = new();

    /// <summary>旧文件里每个文本可复用的 key（按 id 升序排队，逐个出队，避免同一 key 用两次）</summary>
    private readonly Dictionary<string, Queue<string>> _reusableKeys = new();

    private int _nextId = 1;

    /// <summary>
    /// 读取上一次生成的 language.json，仅用于复用旧 key。
    /// 文件不存在时从 1 开始分配。
    /// </summary>
    public void LoadExisting(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(path);
            var existing = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (existing == null || existing.Count == 0)
            {
                return;
            }

            foreach (var pair in existing
                         .OrderBy(kv => ParseId(kv.Key))
                         .ThenBy(kv => kv.Key, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                {
                    continue;
                }

                if (!_reusableKeys.TryGetValue(pair.Value, out var keys))
                {
                    keys = new Queue<string>();
                    _reusableKeys[pair.Value] = keys;
                }

                keys.Enqueue(pair.Key);

                if (int.TryParse(pair.Key, out var id) && id >= _nextId)
                {
                    _nextId = id + 1;
                }
            }

            Logger.Info($"读取已有语言文件: {existing.Count} 条，沿用其 id");
        }
        catch (Exception ex)
        {
            // 旧文件损坏时退回重新分配，至少不会中断导出
            Logger.Warning($"读取已有语言文件失败，将重新分配所有 id: {ex.Message}");
        }
    }

    /// <summary>把所有 language 字段替换成 key（参数表与普通表统一处理）</summary>
    public void CollectLanguages(List<SheetData> sheets)
    {
        foreach (var sheet in sheets)
        {
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

                    row[field.Name] = Allocate(text);
                }
            }
        }
    }

    private string Allocate(string text)
    {
        // 同一文本在旧文件里可能对应多个 key，按顺序各用一次
        if (_reusableKeys.TryGetValue(text, out var keys) && keys.Count > 0)
        {
            var reused = keys.Dequeue();
            _texts[reused] = text;
            return reused;
        }

        var key = _nextId++.ToString();
        _texts[key] = text;
        return key;
    }

    public async Task ExportLanguageJson(string outputPath)
    {
        // 按 id 排序，保证输出稳定、便于 diff
        var ordered = _texts
            .OrderBy(kv => ParseId(kv.Key))
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var json = JsonSerializer.Serialize(ordered, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        await FileUtil.WriteTextAsync(outputPath, json);
        Logger.Success($"生成语言文件: {Path.GetFileName(outputPath)}（{ordered.Count} 条）");
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
        Logger.Success($"生成语言类: {Path.GetFileName(outputPath)}");
    }

    private static int ParseId(string key) => int.TryParse(key, out var id) ? id : int.MaxValue;
}
