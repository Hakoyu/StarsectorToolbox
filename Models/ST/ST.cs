using System.Reflection;

namespace StarsectorToolbox.Models.ST;

/// <summary>StarsectorToolbox数据</summary>
public class ST
{
    /// <summary>核心文件夹</summary>
    public static string CoreDirectory => "STCore";

    /// <summary>拓展目录</summary>
    public static string ExtensionDirectories => "STExtension";

    /// <summary>配置文件</summary>
    public static string SettingsTomlFile => $"{CoreDirectory}\\Settings.toml";

    /// <summary>日志文件</summary>
    public static string LogFile => $"{CoreDirectory}\\{nameof(StarsectorToolbox)}.log";

    /// <summary>拓展信息文件</summary>
    public static string ExtensionInfoFile => "Extension.toml";

    /// <summary>工具箱版本</summary>
    public static string Version => Assembly.GetEntryAssembly()!.GetName().Version!.ToString();
}