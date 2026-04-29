using ExcelToGame.Core;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Logger.Info("========================================");
        Logger.Info("   Excel to Game Config Tool");
        Logger.Info("   Supports: JSON / TypeScript / C#");
        Logger.Info("========================================");

        var inputDir = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "Excel");
        var outputDir = args.Length > 1 ? args[1] : Path.Combine(AppContext.BaseDirectory, "Output");

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
            return;
        }

        Logger.Info($"Found {excelFiles.Count} Excel files");

        var allExcelData = new List<ExcelFileData>();
        var reader = new ExcelReader();

        for (int i = 0; i < excelFiles.Count; i++)
        {
            Logger.Progress($"Reading...", i + 1, excelFiles.Count);
            var excelData = reader.ReadExcelFile(excelFiles[i]);
            if (excelData != null)
            {
                allExcelData.Add(excelData);
            }
        }

        Logger.Info($"Successfully read {allExcelData.Count} Excel files");

        // 收集所有Sheet用于类型检�?
        var allSheets = allExcelData.SelectMany(e => e.DataSheets).ToList();

        var typeChecker = new TypeChecker();
        typeChecker.RegisterTypes(allSheets);

        if (!typeChecker.CheckTypes(allSheets))
        {
            Logger.Error("Type check failed, aborting export");
            return;
        }

        // 收集语言文本
        var langGenerator = new LanguageGenerator();
        langGenerator.CollectLanguages(allSheets);

        // 导出语言文件
        await langGenerator.ExportLanguageJson(Path.Combine(outputDir, "language.json"));
        await langGenerator.GenerateLanguageClass(Path.Combine(tsDir, "_language.ts"));

        // 生成代码文件（每个Excel文件生成一个）
        var codeGenerator = new CodeGenerator();

        for (int i = 0; i < allExcelData.Count; i++)
        {
            var excelData = allExcelData[i];
            Logger.Progress($"Generating {excelData.OutputName}...", i + 1, allExcelData.Count);

            await codeGenerator.GenerateJson(excelData, jsonDir);
            await codeGenerator.GenerateTypeScript(excelData, tsDir);
            await codeGenerator.GenerateCSharp(excelData, csDir);
        }

        Logger.Info("========================================");
        Logger.Success("Export completed!");
        Logger.Info($"Output directory: {outputDir}");
        Logger.Info("========================================");
    }
}


