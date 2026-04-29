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
    
    public static async Task WriteTextAsync(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            EnsureDirectory(dir);
        }
        
        await File.WriteAllTextAsync(path, content, System.Text.Encoding.UTF8);
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
