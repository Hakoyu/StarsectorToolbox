﻿using System.Reflection;

namespace StarsectorToolbox.Models.ST;

/// <summary>StarsectorToolbox数据</summary>
public static class ST
{
    /// <summary>核心文件夹</summary>
    public const string CoreDirectory = "STCore";

    /// <summary>拓展目录</summary>
    public const string ExtensionDirectories = "STExtension";

    /// <summary>配置文件</summary>
    public const string SettingsTomlFile = $"{CoreDirectory}\\Settings.toml";

    /// <summary>日志文件</summary>
    public const string LogFile = $"{CoreDirectory}\\{nameof(StarsectorToolbox)}.log";

    /// <summary>调试模式日志文件</summary>
    public const string DebugLogFile = $"{CoreDirectory}\\{nameof(StarsectorToolbox)}.Debug.log";

    /// <summary>拓展信息文件</summary>
    public const string ExtensionInfoFile = "Extension.toml";

    private static string s_version = string.Empty;

    /// <summary>工具箱版本</summary>
    public static string Version => GetVersion();

    private static string GetVersion()
    {
        if (string.IsNullOrWhiteSpace(s_version))
            return s_version = Assembly.GetEntryAssembly()!.GetName().Version!.ToString()[..^2];
        else
            return s_version;
    }
}
