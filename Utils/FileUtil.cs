namespace ExcelToGame.Utils;

public static class FileUtil
{
    public static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
    
    /// <summary>不带 BOM 的 UTF-8：JSON 带 BOM 会让 JSON.parse / 部分引擎直接解析失败</summary>
    private static readonly System.Text.UTF8Encoding Utf8NoBom = new(false);

    public static async Task WriteTextAsync(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            EnsureDirectory(dir);
        }

        await File.WriteAllTextAsync(path, content, Utf8NoBom);
    }
    
    public static List<string> GetExcelFiles(string directory)
    {
        var result = new List<string>();
        
        if (!Directory.Exists(directory))
        {
            Console.WriteLine($"[警告] 目录不存在: {directory}");
            return result;
        }
        
        var files = Directory.GetFiles(directory, "*.xlsx", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(directory, "*.xls", SearchOption.TopDirectoryOnly));
        
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            
            if (fileName.StartsWith("~$"))
                continue;
            
            var attr = File.GetAttributes(file);
            if ((attr & FileAttributes.Hidden) == FileAttributes.Hidden)
                continue;
            
            result.Add(file);
        }
        
        return result;
    }
}
