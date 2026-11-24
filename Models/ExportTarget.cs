namespace ExcelToGame.Models;

/// <summary>导出目标端：由 main 页签的 client / server 列决定每张表、每个字段进哪一端</summary>
public enum ExportTarget
{
    /// <summary>客户端（Cocos Creator / Unity / Godot 运行时）</summary>
    Client,

    /// <summary>服务端</summary>
    Server
}

public static class ExportTargetExtensions
{
    /// <summary>命令行/日志里的短名：client / server</summary>
    public static string ToKey(this ExportTarget target) =>
        target == ExportTarget.Server ? "server" : "client";

    /// <summary>日志里的中文名</summary>
    public static string ToDisplayName(this ExportTarget target) =>
        target == ExportTarget.Server ? "服务端" : "客户端";
}
