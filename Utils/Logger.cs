namespace ExcelToGame.Utils;

/// <summary>
/// 日志工具类 - 控制台彩色输出
/// </summary>
public static class Logger
{
    private static readonly object LockObj = new();
    private static bool _enableDebug = true;
    
    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    public static bool EnableDebug
    {
        get => _enableDebug;
        set => _enableDebug = value;
    }
    
    /// <summary>
    /// 输出信息日志（白色）
    /// </summary>
    public static void Info(string message)
    {
        Log(message, ConsoleColor.White, "INFO");
    }
    
    /// <summary>
    /// 输出成功日志（绿色）
    /// </summary>
    public static void Success(string message)
    {
        Log(message, ConsoleColor.Green, "SUCCESS");
    }
    
    /// <summary>
    /// 输出警告日志（黄色）
    /// </summary>
    public static void Warning(string message)
    {
        Log(message, ConsoleColor.Yellow, "WARNING");
    }
    
    /// <summary>
    /// 输出错误日志（红色）
    /// </summary>
    public static void Error(string message)
    {
        Log(message, ConsoleColor.Red, "ERROR");
    }
    
    /// <summary>
    /// 输出调试日志（灰色）
    /// </summary>
    public static void Debug(string message)
    {
        if (!_enableDebug) return;
        Log(message, ConsoleColor.Gray, "DEBUG");
    }
    
    /// <summary>
    /// 输出表格处理相关日志
    /// </summary>
    public static void Table(string tableName, string message)
    {
        Log($"[{tableName}] {message}", ConsoleColor.Cyan, "TABLE");
    }
    
    /// <summary>
    /// 输出字段相关日志
    /// </summary>
    public static void Field(string tableName, string fieldName, string message)
    {
        Log($"[{tableName}.{fieldName}] {message}", ConsoleColor.DarkCyan, "FIELD");
    }
    
    /// <summary>
    /// 输出进度日志
    /// </summary>
    public static void Progress(string message, int current, int total)
    {
        var percentage = total > 0 ? (current * 100 / total) : 0;
        var progressBar = new string('█', percentage / 5).PadRight(20, '░');
        Log($"[{progressBar}] {percentage}% ({current}/{total}) {message}", ConsoleColor.Magenta, "PROGRESS");
    }
    
    /// <summary>
    /// 输出分隔线
    /// </summary>
    public static void Separator(char c = '-', int length = 50)
    {
        lock (LockObj)
        {
            Console.WriteLine(new string(c, length));
        }
    }
    
    /// <summary>
    /// 输出空行
    /// </summary>
    public static void NewLine()
    {
        lock (LockObj)
        {
            Console.WriteLine();
        }
    }
    
    private static void Log(string message, ConsoleColor color, string level)
    {
        lock (LockObj)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var originalColor = Console.ForegroundColor;
            
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{timestamp}] ");
            
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            
            Console.ForegroundColor = originalColor;
        }
    }
}
