using ExcelToGame.Config;
using ExcelToGame.Core;
using ExcelToGame.Utils;
using System.Text;

namespace ExcelToGame;

/// <summary>
/// Excel转配置工具主程序入口
/// 支持Unity/Cocos Creator双引擎
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // 设置UTF-8编码，确保中文不乱码
        Console.OutputEncoding = Encoding.UTF8;
        
        Logger.Info("==============================================");
        Logger.Info("   Excel转配置工具 - Unity/Cocos Creator");
        Logger.Info("   版本: 1.0.0");
        Logger.Info("==============================================");
        
        try
        {
            // 加载配置
            var config = AppConfig.Load();
            Logger.Info($"输入目录: {config.InputDirectory}");
            Logger.Info($"输出目录: {config.OutputDirectory}");
            
            // 创建导出器并执行
            var exporter = new ExcelExporter(config);
            var result = await exporter.ExportAllAsync();
            
            // 输出统计结果
            Logger.Info("==============================================");
            Logger.Info("   导出完成统计");
            Logger.Info("==============================================");
            Logger.Info($"成功: {result.SuccessCount} 个文件");
            Logger.Info($"失败: {result.FailCount} 个文件");
            Logger.Info($"总耗时: {result.ElapsedMilliseconds}ms");
            
            if (result.Errors.Count > 0)
            {
                Logger.Warning("\n错误详情:");
                foreach (var error in result.Errors)
                {
                    Logger.Error($"  - {error}");
                }
            }
            
            Environment.ExitCode = result.FailCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            Logger.Error($"程序异常: {ex.Message}");
            Logger.Error($"堆栈: {ex.StackTrace}");
            Environment.ExitCode = 1;
        }
        
        Logger.Info("\n按任意键退出...");
        Console.ReadKey();
    }
}
