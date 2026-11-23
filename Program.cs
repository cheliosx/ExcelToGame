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

        // 用法: ExcelToGame <输入目录> <输出目录> [--compact]
        // 默认行数据逐行输出（方便 diff），--compact 时压成单块字符串（文件更小）
        var options = args.Where(a => a.StartsWith("--")).Select(a => a.ToLowerInvariant()).ToHashSet();
        var paths = args.Where(a => !a.StartsWith("--")).ToArray();

        var inputDir = paths.Length > 0 ? paths[0] : Path.Combine(AppContext.BaseDirectory, "Excel");
        var outputDir = paths.Length > 1 ? paths[1] : Path.Combine(AppContext.BaseDirectory, "Output");
        var splitRows = !options.Contains("--compact");

        var jsonDir = Path.Combine(outputDir, "Json");
        var tsDir = Path.Combine(outputDir, "TS");
        var csDir = Path.Combine(outputDir, "CSharp");

        FileUtil.EnsureDirectory(jsonDir);
        FileUtil.EnsureDirectory(tsDir);
        FileUtil.EnsureDirectory(csDir);

        var excelFiles = FileUtil.GetExcelFiles(inputDir);
        if (excelFiles.Count == 0)
        {
            Logger.Error($"No Excel files found: {inputDir}");
            return 1;
        }

        Logger.Info($"Found {excelFiles.Count} Excel files");
        Logger.Info(splitRows ? "JSON 行数据: 逐行输出 (rows)，便于 diff" : "JSON 行数据: 单块文本 (data)，体积更小");

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
        if (!validator.Validate(allExcelData))
        {
            Logger.Error("校验失败，已中止导出");
            return 1;
        }

        // 收集语言文本（会就地替换行内的 language 单元格为 key）
        var allSheets = allExcelData.SelectMany(e => e.DataSheets).ToList();
        var langGenerator = new LanguageGenerator();
        langGenerator.CollectLanguages(allSheets, reader.GetNextLanguageId());

        await langGenerator.ExportLanguageJson(Path.Combine(outputDir, "language.json"));
        await langGenerator.GenerateLanguageClass(Path.Combine(tsDir, "_language.ts"));

        // 合并继承链后生成产物（每个 Excel 文件一套）
        var builder = new SchemaBuilder(validator.Types);
        var codeGenerator = new CodeGenerator();

        for (int i = 0; i < allExcelData.Count; i++)
        {
            var excelData = allExcelData[i];
            Logger.Progress($"Generating {excelData.OutputName}...", i + 1, allExcelData.Count);

            var schemas = builder.Build(excelData);

            await codeGenerator.GenerateJson(excelData, schemas, jsonDir, splitRows);
            await codeGenerator.GenerateTypeScript(excelData, schemas, tsDir);
            await codeGenerator.GenerateCSharp(excelData, schemas, csDir);
        }

        Logger.Info("========================================");
        Logger.Success("Export completed!");
        Logger.Info($"Output directory: {outputDir}");
        Logger.Info("========================================");

        return 0;
    }
}
