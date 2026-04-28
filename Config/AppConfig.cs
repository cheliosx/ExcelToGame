using System.Text.Json;
using ExcelToGame.Utils;

namespace ExcelToGame.Config;

/// <summary>
/// 应用程序配置类
/// </summary>
public class AppConfig
{
    /// <summary>
    /// 输入目录（Excel文件所在目录）
    /// </summary>
    public string InputDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Excel");
    
    /// <summary>
    /// 输出根目录
    /// </summary>
    public string OutputDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Output");
    
    /// <summary>
    /// JSON输出子目录
    /// </summary>
    public string JsonOutputPath { get; set; } = "Json";
    
    /// <summary>
    /// TypeScript输出子目录
    /// </summary>
    public string TypeScriptOutputPath { get; set; } = "TypeScript";
    
    /// <summary>
    /// C#输出子目录
    /// </summary>
    public string CSharpOutputPath { get; set; } = "CSharp";
    
    /// <summary>
    /// 枚举输出子目录
    /// </summary>
    public string EnumOutputPath { get; set; } = "Enums";
    
    /// <summary>
    /// C#命名空间
    /// </summary>
    public string CSharpNamespace { get; set; } = "Game.Config";
    
    /// <summary>
    /// 导出开关列名称
    /// </summary>
    public string ExportSwitchColumnName { get; set; } = "Export";
    
    /// <summary>
    /// 行结束标记
    /// </summary>
    public string RowEndMarker { get; set; } = "END";
    
    /// <summary>
    /// 列结束标记
    /// </summary>
    public string ColumnEndMarker { get; set; } = "END";
    
    /// <summary>
    /// 表头行数（注释、字段名、类型）
    /// </summary>
    public int HeaderRowCount { get; set; } = 3;
    
    /// <summary>
    /// 数据起始行（0-based）
    /// </summary>
    public int DataStartRow { get; set; } = 3;
    
    /// <summary>
    /// JSON缩进大小
    /// </summary>
    public int JsonIndentSize { get; set; } = 2;
    
    /// <summary>
    /// 是否格式化JSON输出
    /// </summary>
    public bool FormatJson { get; set; } = true;
    
    /// <summary>
    /// 获取完整JSON输出路径
    /// </summary>
    public string GetFullJsonPath() => Path.Combine(OutputDirectory, JsonOutputPath);
    
    /// <summary>
    /// 获取完整TypeScript输出路径
    /// </summary>
    public string GetFullTypeScriptPath() => Path.Combine(OutputDirectory, TypeScriptOutputPath);
    
    /// <summary>
    /// 获取完整C#输出路径
    /// </summary>
    public string GetFullCSharpPath() => Path.Combine(OutputDirectory, CSharpOutputPath);
    
    /// <summary>
    /// 获取完整枚举输出路径
    /// </summary>
    public string GetFullEnumPath() => Path.Combine(OutputDirectory, EnumOutputPath);
    
    /// <summary>
    /// 加载配置文件，如果不存在则创建默认配置
    /// </summary>
    public static AppConfig Load(string? configPath = null)
    {
        configPath ??= Path.Combine(AppContext.BaseDirectory, "config.json");
        
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (config != null)
                {
                    Logger.Info($"已加载配置文件: {configPath}");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"加载配置文件失败: {ex.Message}，使用默认配置");
            }
        }
        
        // 创建默认配置
        var defaultConfig = new AppConfig();
        defaultConfig.Save(configPath);
        Logger.Info($"已创建默认配置文件: {configPath}");
        
        return defaultConfig;
    }
    
    /// <summary>
    /// 保存配置文件
    /// </summary>
    public void Save(string? configPath = null)
    {
        configPath ??= Path.Combine(AppContext.BaseDirectory, "config.json");
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(configPath, json);
    }
}
