using ExcelToGame.Core;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Logger.Info("========================================");
        Logger.Info("   Excel to Game Config Tool");
        Logger.Info("   Supports: JSON / TypeScript / C#");
        Logger.Info("========================================");

        // 用法: ExcelToGame <输入目录> <输出目录> [--target client|server] [--ts] [--cs] [--lang ts,cs]
        // 输入目录下的所有 .xlsx 会被一次性读取，枚举与多语言全局汇总
        var positionals = new List<string>();
        var target = ExportTarget.Client;
        var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var languageSpecified = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--"))
            {
                positionals.Add(arg);
                continue;
            }

            var (name, inlineValue) = SplitOption(arg);
            switch (name)
            {
                case "target":
                    var value = inlineValue ?? (i + 1 < args.Length ? args[++i] : string.Empty);
                    if (!TryParseTarget(value, out target))
                    {
                        Logger.Error($"未知的导出目标: '{value}'，可选 client / server");
                        return 1;
                    }
                    break;

                case "client":
                    target = ExportTarget.Client;
                    break;

                case "server":
                    target = ExportTarget.Server;
                    break;

                case "ts":
                case "typescript":
                    languages.Add("ts");
                    languageSpecified = true;
                    break;

                case "cs":
                case "csharp":
                    languages.Add("cs");
                    languageSpecified = true;
                    break;

                case "lang":
                    var spec = inlineValue ?? (i + 1 < args.Length ? args[++i] : string.Empty);
                    languageSpecified = true;

                    foreach (var part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (IsTypescript(part))
                        {
                            languages.Add("ts");
                        }
                        else if (IsCSharp(part))
                        {
                            languages.Add("cs");
                        }
                        else
                        {
                            Logger.Error($"未知的导出语言: '{part}'，可选 ts / cs");
                            return 1;
                        }
                    }

                    if (languages.Count == 0)
                    {
                        Logger.Error("--lang 不能为空，可选 ts / cs");
                        return 1;
                    }
                    break;

                default:
                    Logger.Warning($"忽略未知参数: {arg}");
                    break;
            }
        }

        // 不指定就两种语言都导
        if (!languageSpecified)
        {
            languages.Add("ts");
            languages.Add("cs");
        }

        var emitTs = languages.Contains("ts");
        var emitCs = languages.Contains("cs");

        var inputDir = positionals.Count > 0 ? positionals[0] : Path.Combine(AppContext.BaseDirectory, "Excel");
        var outputDir = positionals.Count > 1 ? positionals[1] : Path.Combine(AppContext.BaseDirectory, "Output");

        Logger.Info($"导出目标: {target.ToDisplayName()} ({target.ToKey()})");
        Logger.Info($"导出内容: JSON + i10n{(emitTs ? " + TypeScript" : string.Empty)}{(emitCs ? " + C#" : string.Empty)}");

        // 读取与校验都在写盘之前完成：这一步失败不会动到已有产物
        var excelFiles = FileUtil.GetExcelFiles(inputDir);
        if (excelFiles.Count == 0)
        {
            Logger.Error($"No Excel files found: {inputDir}");
            return 1;
        }

        Logger.Info($"Found {excelFiles.Count} Excel files");

        var allExcelData = new List<ExcelFileData>();
        var reader = new ExcelReader();

        for (int i = 0; i < excelFiles.Count; i++)
        {
            Logger.Progress("Reading...", i + 1, excelFiles.Count);
            var excelData = reader.ReadExcelFile(excelFiles[i]);
            if (excelData != null)
            {
                allExcelData.Add(excelData);
            }
        }

        if (allExcelData.Count == 0)
        {
            // 常见原因：文件被 Excel 占用，或全部读取失败；不能静默产出空配置
            Logger.Error("没有成功读取任何 Excel 文件，已中止导出");
            return 1;
        }

        Logger.Info($"Successfully read {allExcelData.Count} Excel files");

        // 类型与结构校验（不通过直接中止，避免把问题带到运行时）
        var validator = new SchemaValidator();
        if (!validator.Validate(allExcelData, target))
        {
            Logger.Error("校验失败，已中止导出");
            return 1;
        }

        // 按 main 页签的 client / server 标记筛出本次要导出的表
        var targetFiles = allExcelData
            .Select(f => f.ForTarget(target))
            .Where(f => f.DataSheets.Count > 0)
            .ToList();

        var targetSheets = targetFiles.SelectMany(f => f.DataSheets).ToList();
        var totalSheets = allExcelData.Sum(f => f.DataSheets.Count);
        Logger.Info($"参与导出 {targetSheets.Count} 张表（共 {totalSheets} 张）");

        foreach (var file in allExcelData)
        {
            var skipped = file.DataSheets.Where(s => !s.IsExportedTo(target)).Select(s => s.SheetName).ToList();
            if (skipped.Count > 0)
            {
                Logger.Info($"  跳过 {file.OutputName}: {string.Join(", ", skipped)}");
            }
        }

        if (targetSheets.Count == 0)
        {
            Logger.Error($"没有任何表标记为导出到{target.ToDisplayName()}，请检查 main 页签的 {target.ToKey()} 列");
            return 1;
        }

        var outputError = OutputPublisher.Validate(inputDir, outputDir);
        if (outputError != null)
        {
            Logger.Error(outputError);
            return 1;
        }

        // 语言 key 复用上一次的结果，避免表格改动导致已有条目 id 漂移
        var allSheets = allExcelData.SelectMany(e => e.DataSheets).ToList();
        var langGenerator = new LanguageGenerator();
        langGenerator.LoadExisting(FindLanguageFile(outputDir));
        langGenerator.CollectLanguages(allSheets);

        // 全部产物先写进临时目录，成功后再整体替换正式目录
        var stagingDir = OutputPublisher.CreateStaging(outputDir);
        Logger.Info($"临时输出目录: {stagingDir}");

        var jsonDir = Path.Combine(stagingDir, "Json");
        var tsDir = Path.Combine(stagingDir, "TS");
        var csDir = Path.Combine(stagingDir, "CSharp");
        var langDir = Path.Combine(stagingDir, LanguageGenerator.Directory);

        try
        {
            FileUtil.EnsureDirectory(jsonDir);
            FileUtil.EnsureDirectory(langDir);

            if (emitTs)
            {
                FileUtil.EnsureDirectory(tsDir);
            }

            if (emitCs)
            {
                FileUtil.EnsureDirectory(csDir);
            }

            await langGenerator.ExportLanguageJson(Path.Combine(langDir, LanguageGenerator.DefaultFileName));

            // 全局产物：只生成被选中的语言
            var codeGenerator = new CodeGenerator();

            if (emitTs)
            {
                await langGenerator.GenerateLanguageClass(Path.Combine(tsDir, "_language.ts"));
                await codeGenerator.GenerateTsRuntime(tsDir);
                await codeGenerator.GenerateTsEnums(targetSheets, tsDir);
                await codeGenerator.GenerateEntry(targetFiles, tsDir);
            }

            if (emitCs)
            {
                await codeGenerator.GenerateCsRuntime(csDir);
                await codeGenerator.GenerateCsEnums(targetSheets, csDir);
                await codeGenerator.GenerateCsEntry(targetFiles, csDir);
            }

            // 每个 Excel 文件一套数据与实体
            var builder = new SchemaBuilder(validator.Types, target);

            for (int i = 0; i < targetFiles.Count; i++)
            {
                var excelData = targetFiles[i];
                Logger.Progress($"Generating {excelData.OutputName}...", i + 1, targetFiles.Count);

                var schemas = builder.Build(excelData);

                await codeGenerator.GenerateJson(excelData, schemas, jsonDir);

                if (emitTs)
                {
                    await codeGenerator.GenerateTypeScript(excelData, schemas, tsDir);
                }

                if (emitCs)
                {
                    await codeGenerator.GenerateCSharp(excelData, schemas, csDir);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"生成失败: {ex.Message}");
            OutputPublisher.Cleanup(stagingDir);
            return 1;
        }

        // 全部生成成功，才动正式目录
        try
        {
            OutputPublisher.Publish(stagingDir, outputDir);
        }
        catch (Exception ex)
        {
            Logger.Error($"替换输出目录失败: {ex.Message}");
            Logger.Warning($"已生成的文件保留在临时目录: {stagingDir}");
            return 1;
        }

        OutputPublisher.Cleanup(stagingDir);

        Logger.Info("========================================");
        Logger.Success("Export completed!");
        Logger.Info($"Output directory: {outputDir}");
        Logger.Info("========================================");

        return 0;
    }

    /// <summary>
    /// 上一次导出的语言文件：优先 i10n/zh-Hans.json，
    /// 兼容旧版本放在输出目录根部的 language.json（避免升级时 id 全部重排）。
    /// </summary>
    private static string FindLanguageFile(string outputDir)
    {
        var current = Path.Combine(outputDir, LanguageGenerator.Directory, LanguageGenerator.DefaultFileName);
        if (File.Exists(current))
        {
            return current;
        }

        var legacy = Path.Combine(outputDir, "language.json");
        return File.Exists(legacy) ? legacy : current;
    }

    /// <summary>把 --name=value 拆成名字与内联值</summary>
    private static (string Name, string? Value) SplitOption(string arg)
    {
        var body = arg[2..];
        var index = body.IndexOf('=');

        return index < 0
            ? (body.ToLowerInvariant(), null)
            : (body[..index].ToLowerInvariant(), body[(index + 1)..]);
    }

    private static bool IsTypescript(string value) =>
        value.Equals("ts", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("typescript", StringComparison.OrdinalIgnoreCase);

    private static bool IsCSharp(string value) =>
        value.Equals("cs", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("csharp", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("c#", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseTarget(string value, out ExportTarget target)
    {
        switch ((value ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "client":
            case "c":
                target = ExportTarget.Client;
                return true;

            case "server":
            case "s":
                target = ExportTarget.Server;
                return true;

            default:
                target = ExportTarget.Client;
                return false;
        }
    }
}
