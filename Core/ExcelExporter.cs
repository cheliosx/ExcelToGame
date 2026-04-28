using System.Diagnostics;
using ExcelToGame.Config;
using ExcelToGame.Models;
using ExcelToGame.Utils;

namespace ExcelToGame.Core;

/// <summary>
/// Excel导出结果
/// </summary>
public class ExportResult
{
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Excel导出器 - 主控制器
/// </summary>
public class ExcelExporter
{
    private readonly AppConfig _config;
    private readonly ExcelReader _excelReader;
    private readonly DataValidator _validator;
    private readonly EnumManager _enumManager;
    private readonly JsonExporter _jsonExporter;
    private readonly CSharpCodeGenerator _csharpGenerator;
    private readonly TypeScriptGenerator _tsGenerator;
    
    public ExcelExporter(AppConfig config)
    {
        _config = config;
        _excelReader = new ExcelReader(config);
        _validator = new DataValidator();
        _enumManager = new EnumManager();
        _jsonExporter = new JsonExporter(config);
        _csharpGenerator = new CSharpCodeGenerator(config);
        _tsGenerator = new TypeScriptGenerator(config);
    }
    
    /// <summary>
    /// 导出所有Excel文件
    /// </summary>
    public async Task<ExportResult> ExportAllAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ExportResult();
        
        try
        {
            // 1. 准备输出目录
            PrepareOutputDirectories();
            
            // 2. 获取所有Excel文件
            var excelFiles = FileUtils.GetExcelFiles(_config.InputDirectory);
            Logger.Info($"找到 {excelFiles.Count} 个Excel文件");
            
            if (excelFiles.Count == 0)
            {
                Logger.Warning("没有找到任何Excel文件");
                return result;
            }
            
            // 3. 读取所有表格
            var tables = new List<TableData>();
            for (int i = 0; i < excelFiles.Count; i++)
            {
                var file = excelFiles[i];
                Logger.Progress($"读取中...", i + 1, excelFiles.Count);
                
                try
                {
                    var tableData = _excelReader.ReadExcel(file);
                    tables.Add(tableData);
                    
                    // 收集枚举定义
                    _enumManager.CollectEnums(tableData);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"读取文件失败 [{file}]: {ex.Message}");
                    result.FailCount++;
                }
            }
            
            Logger.NewLine();
            
            // 4. 验证数据
            Logger.Info("开始验证数据...");
            foreach (var table in tables)
            {
                _validator.Validate(table);
                _enumManager.ValidateTableEnums(table);
            }
            
            // 5. 导出枚举（在所有表格验证后统一导出）
            await ExportEnumsAsync();
            
            // 6. 导出各表格
            var validTables = tables.Where(t => t.IsValid).ToList();
            var invalidTables = tables.Where(t => !t.IsValid).ToList();
            
            // 输出验证失败的表格
            foreach (var table in invalidTables)
            {
                Logger.Error($"表格验证失败 [{table.FileName}]，跳过导出");
                foreach (var error in table.Errors)
                {
                    Logger.Error($"  - {error}");
                }
                result.FailCount++;
            }
            
            // 导出有效表格
            for (int i = 0; i < validTables.Count; i++)
            {
                var table = validTables[i];
                Logger.Progress($"导出 [{table.FileName}]...", i + 1, validTables.Count);
                
                var success = await ExportTableAsync(table);
                if (success)
                {
                    result.SuccessCount++;
                }
                else
                {
                    result.FailCount++;
                    result.Errors.Add($"导出失败: {table.FileName}");
                }
            }
            
            Logger.NewLine();
            
            // 7. 生成配置管理器
            if (validTables.Count > 0)
            {
                await _csharpGenerator.GenerateConfigManagerAsync(validTables);
                await _tsGenerator.GenerateConfigManagerAsync(validTables);
            }
            
            // 8. 输出警告信息
            foreach (var table in tables.Where(t => t.Warnings.Count > 0))
            {
                Logger.Warning($"表格 [{table.FileName}] 警告:");
                foreach (var warning in table.Warnings)
                {
                    Logger.Warning($"  - {warning}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"导出过程异常: {ex.Message}");
            result.Errors.Add($"导出过程异常: {ex.Message}");
        }
        
        stopwatch.Stop();
        result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        
        return result;
    }
    
    /// <summary>
    /// 准备输出目录
    /// </summary>
    private void PrepareOutputDirectories()
    {
        FileUtils.EnsureDirectory(_config.GetFullJsonPath());
        FileUtils.EnsureDirectory(_config.GetFullTypeScriptPath());
        FileUtils.EnsureDirectory(_config.GetFullCSharpPath());
        FileUtils.EnsureDirectory(_config.GetFullEnumPath());
        
        Logger.Info("输出目录准备完成");
    }
    
    /// <summary>
    /// 导出单个表格
    /// </summary>
    private async Task<bool> ExportTableAsync(TableData tableData)
    {
        try
        {
            // 并行导出JSON、C#、TypeScript
            var tasks = new List<Task<bool>>
            {
                _jsonExporter.ExportAsync(tableData),
                _csharpGenerator.GenerateClassAsync(tableData),
                _tsGenerator.GenerateInterfaceAsync(tableData)
            };
            
            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        catch (Exception ex)
        {
            Logger.Error($"导出表格 [{tableData.FileName}] 异常: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 导出枚举文件
    /// </summary>
    private async Task ExportEnumsAsync()
    {
        var enums = _enumManager.GetAllEnums();
        if (enums.Count == 0)
        {
            Logger.Info("没有枚举需要导出");
            return;
        }
        
        Logger.Info($"导出 {enums.Count} 个枚举...");
        
        // 导出C#枚举
        var csharpEnumCode = _enumManager.GenerateCSharpEnums(_config.CSharpNamespace);
        var csharpEnumPath = Path.Combine(_config.GetFullEnumPath(), "Enums.cs");
        await FileUtils.WriteTextAsync(csharpEnumPath, csharpEnumCode);
        Logger.Success($"导出C#枚举: {csharpEnumPath}");
        
        // 导出TypeScript枚举
        var tsEnumCode = _enumManager.GenerateTypeScriptEnums();
        var tsEnumPath = Path.Combine(_config.GetFullTypeScriptPath(), "Enums.ts");
        await FileUtils.WriteTextAsync(tsEnumPath, tsEnumCode);
        Logger.Success($"导出TypeScript枚举: {tsEnumPath}");
    }
}
