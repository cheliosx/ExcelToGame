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

        // 用法: ExcelToGame <输入目录> <输出目录>
        // 输入目录下的所有 .xlsx 会被一次性读取，枚举与多语言全局汇总
        var paths = args.Where(a => !a.StartsWith("--")).ToArray();

        var inputDir = paths.Length > 0 ? paths[0] : Path.Combine(AppContext.BaseDirectory, "Excel");
        var outputDir = paths.Length > 1 ? paths[1] : Path.Combine(AppContext.BaseDirectory, "Output");

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
        if (!validator.Validate(allExcelData))
        {
            Logger.Error("校验失败，已中止导出");
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
        langGenerator.LoadExisting(Path.Combine(outputDir, "language.json"));
        langGenerator.CollectLanguages(allSheets);

        // 全部产物先写进临时目录，成功后再整体替换正式目录
        var stagingDir = OutputPublisher.CreateStaging(outputDir);
        Logger.Info($"临时输出目录: {stagingDir}");

        var jsonDir = Path.Combine(stagingDir, "Json");
        var tsDir = Path.Combine(stagingDir, "TS");
        var csDir = Path.Combine(stagingDir, "CSharp");

        try
        {
            FileUtil.EnsureDirectory(jsonDir);
            FileUtil.EnsureDirectory(tsDir);
            FileUtil.EnsureDirectory(csDir);

            await langGenerator.ExportLanguageJson(Path.Combine(stagingDir, "language.json"));
            await langGenerator.GenerateLanguageClass(Path.Combine(tsDir, "_language.ts"));

            // 全局产物：运行时 + 所有枚举各汇总成一份 TS / C# 文件，另生成 TS 统一入口
            var codeGenerator = new CodeGenerator();
            await codeGenerator.GenerateRuntime(tsDir, csDir);
            await codeGenerator.GenerateEnums(allSheets, tsDir, csDir);
            await codeGenerator.GenerateEntry(allExcelData, tsDir);

            // 每个 Excel 文件一套数据与实体
            var builder = new SchemaBuilder(validator.Types);

            for (int i = 0; i < allExcelData.Count; i++)
            {
                var excelData = allExcelData[i];
                Logger.Progress($"Generating {excelData.OutputName}...", i + 1, allExcelData.Count);

                var schemas = builder.Build(excelData);

                await codeGenerator.GenerateJson(excelData, schemas, jsonDir);
                await codeGenerator.GenerateTypeScript(excelData, schemas, tsDir);
                await codeGenerator.GenerateCSharp(excelData, schemas, csDir);
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
}
