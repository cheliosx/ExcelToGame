namespace ExcelToGame.Utils;

/// <summary>
/// 输出发布器：先生成到临时目录，全部成功后再整体替换正式目录。
/// 生成中途失败时原目录保持不变，避免出现新旧混杂的半成品。
/// </summary>
public static class OutputPublisher
{
    /// <summary>在系统临时目录下创建本次导出的暂存目录（与正式目录同名的子目录）</summary>
    public static string CreateStaging(string outputDir)
    {
        var full = Path.GetFullPath(outputDir);
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(full));
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "output";
        }

        var root = Path.Combine(Path.GetTempPath(), "ExcelToGame");
        Directory.CreateDirectory(root);
        CleanStale(root, name);

        var staging = Path.Combine(root, $"{name}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        return staging;
    }

    /// <summary>清理同名输出历史遗留的临时目录（进程被强杀时会残留）</summary>
    private static void CleanStale(string root, string name)
    {
        try
        {
            foreach (var dir in Directory.GetDirectories(root))
            {
                if (!Path.GetFileName(dir).StartsWith(name + "-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 只清理一小时前的，避免误删正在运行的另一份导出
                if (DateTime.UtcNow - Directory.GetLastWriteTimeUtc(dir) < TimeSpan.FromHours(1))
                {
                    continue;
                }

                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                    // 被占用就跳过，不影响本次导出
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"清理历史临时目录失败: {ex.Message}");
        }
    }

    /// <summary>检查输出目录能否安全清空；返回 null 表示允许</summary>
    public static string? Validate(string inputDir, string outputDir)
    {
        var output = Trim(Path.GetFullPath(outputDir));
        var input = Trim(Path.GetFullPath(inputDir));

        if (output.Equals(input, StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return "输出目录不能是输入目录本身，也不能是它的父目录";
        }

        var root = Trim(Path.GetPathRoot(output) ?? string.Empty);
        if (root.Length == 0 || output.Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            return "输出目录不能是磁盘根目录";
        }

        if (Directory.Exists(Path.Combine(output, ".git")))
        {
            return "输出目录是 Git 仓库根目录（含 .git），拒绝清空";
        }

        return null;
    }

    /// <summary>删除正式目录，把暂存目录的内容整体复制过去</summary>
    public static void Publish(string stagingDir, string outputDir)
    {
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        CopyDirectory(stagingDir, outputDir);
    }

    /// <summary>清理暂存目录（失败只记日志，不影响导出结果）</summary>
    public static void Cleanup(string stagingDir)
    {
        try
        {
            if (Directory.Exists(stagingDir))
            {
                Directory.Delete(stagingDir, true);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"清理临时目录失败: {stagingDir} ({ex.Message})");
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, dir)));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), true);
        }
    }

    private static string Trim(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
