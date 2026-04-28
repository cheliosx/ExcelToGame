namespace ExcelToGame.Utils;

/// <summary>
/// 文件工具类
/// </summary>
public static class FileUtils
{
    /// <summary>
    /// 确保目录存在
    /// </summary>
    public static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Logger.Debug($"创建目录: {path}");
        }
    }
    
    /// <summary>
    /// 异步写入文本文件
    /// </summary>
    public static async Task WriteTextAsync(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            EnsureDirectory(directory);
        }
        
        await File.WriteAllTextAsync(path, content, System.Text.Encoding.UTF8);
    }
    
    /// <summary>
    /// 同步写入文本文件
    /// </summary>
    public static void WriteText(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            EnsureDirectory(directory);
        }
        
        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
    }
    
    /// <summary>
    /// 获取所有Excel文件
    /// </summary>
    public static List<string> GetExcelFiles(string directory)
    {
        var result = new List<string>();
        
        if (!Directory.Exists(directory))
        {
            Logger.Warning($"目录不存在: {directory}");
            return result;
        }
        
        var files = Directory.GetFiles(directory, "*.xlsx", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(directory, "*.xls", SearchOption.TopDirectoryOnly));
        
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            
            // 排除临时文件（以~$开头的Excel缓存文件）
            if (fileName.StartsWith("~$"))
            {
                Logger.Debug($"跳过临时文件: {fileName}");
                continue;
            }
            
            // 排除隐藏文件
            var attributes = File.GetAttributes(file);
            if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            {
                Logger.Debug($"跳过隐藏文件: {fileName}");
                continue;
            }
            
            result.Add(file);
        }
        
        return result;
    }
    
    /// <summary>
    /// 清空目录
    /// </summary>
    public static void ClearDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }
        
        // 删除所有文件
        foreach (var file in Directory.GetFiles(path))
        {
            File.Delete(file);
        }
        
        // 递归删除子目录
        foreach (var dir in Directory.GetDirectories(path))
        {
            Directory.Delete(dir, true);
        }
        
        Logger.Debug($"清空目录: {path}");
    }
    
    /// <summary>
    /// 复制目录
    /// </summary>
    public static void CopyDirectory(string sourceDir, string destDir)
    {
        EnsureDirectory(destDir);
        
        // 复制文件
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        
        // 递归复制子目录
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
    
    /// <summary>
    /// 获取相对路径
    /// </summary>
    public static string GetRelativePath(string fromPath, string toPath)
    {
        var fromUri = new Uri(fromPath.EndsWith(Path.DirectorySeparatorChar.ToString()) 
            ? fromPath 
            : fromPath + Path.DirectorySeparatorChar);
        var toUri = new Uri(toPath);
        
        var relativeUri = fromUri.MakeRelativeUri(toUri);
        return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
    }
}
