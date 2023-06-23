using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using HKW.HKWViewModels.Dialogs;
using StarsectorToolbox.Libs;
using I18nRes = StarsectorToolbox.Langs.Libs.UtilsI18nRes;

namespace StarsectorToolbox.Models.GameInfo;

/// <summary>游戏信息</summary>
public static class GameInfo
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>游戏目录</summary>
    public static string BaseDirectory { get; private set; } = string.Empty;

    /// <summary>游戏Core目录</summary>
    public static string CoreDirectory { get; private set; } = string.Empty;

    /// <summary>游戏exe文件</summary>
    public static string ExeFile { get; private set; } = string.Empty;

    /// <summary>游戏模组文件夹</summary>
    public static string ModsDirectory { get; private set; } = string.Empty;

    /// <summary>游戏存档文件夹</summary>
    public static string SaveDirectory { get; private set; } = string.Empty;

    /// <summary>游戏已启用模组文件</summary>
    public static string EnabledModsJsonFile { get; private set; } = string.Empty;

    /// <summary>游戏日志文件</summary>
    public static string LogFile { get; private set; } = string.Empty;

    /// <summary>游戏版本</summary>
    public static string Version { get; private set; } = string.Empty;

    /// <summary>游戏设置文件</summary>
    public static string SettingsFile { get; private set; } = string.Empty;

    /// <summary>虚拟机参数文件</summary>
    public static string VmparamsFile { get; private set; } = string.Empty;

    /// <summary>爪洼文件</summary>
    public static string JavaFile { get; private set; } = string.Empty;

    /// <summary>爪洼版本</summary>
    public static string JaveVersion { get; private set; } = string.Empty;

    private static readonly Regex s_checkLauncher =
        new(@"Starting Starsector [^ ]+ launcher", RegexOptions.Compiled);
    private static readonly Regex s_checkVersion =
        new(@"[0-9]+.[0-9]+[^ ]*", RegexOptions.Compiled);

    /// <summary>
    /// 设置游戏信息
    /// </summary>
    /// <param name="gameDirectory">游戏目录</param>
    internal static bool SetGameData(string gameDirectory)
    {
        if (CheckGameDirectory(gameDirectory, out var exeFile) is false)
        {
            sr_logger.Error($"{I18nRes.GameDirectoryError} {I18nRes.Path}: {gameDirectory}");
            MessageBoxVM.Show(
                new($"{I18nRes.GameDirectoryError}\n{I18nRes.Path}")
                {
                    Icon = MessageBoxVM.Icon.Error
                }
            );
            return false;
        }
        ExeFile = exeFile;
        JavaFile = TryGetJavaFile(gameDirectory);
        JaveVersion = TryGetJaveVersion(JavaFile);
        VmparamsFile = Path.Combine(gameDirectory, "vmparams");
        BaseDirectory = gameDirectory;
        ModsDirectory = Path.Combine(gameDirectory, "mods");
        SaveDirectory = Path.Combine(gameDirectory, "saves");
        CoreDirectory = Path.Combine(gameDirectory, "starsector-core");
        EnabledModsJsonFile = Path.Combine(ModsDirectory, "enabled_mods.json");
        LogFile = Path.Combine(CoreDirectory, "starsector.log");
        SettingsFile = Path.Combine(CoreDirectory, "data", "config", "settings.json");
        Version = TryGetGameVersion(LogFile);
        if (string.IsNullOrWhiteSpace(Version))
        {
            sr_logger.Info(I18nRes.GameVersionAccessFailed);
            MessageBoxVM.Show(new(I18nRes.GameVersionAccessFailedMessage));
        }
        return true;
    }

    private static bool CheckGameDirectory(string gameDirectory, out string exeFile)
    {
        exeFile = string.Empty;
        if (string.IsNullOrWhiteSpace(gameDirectory))
            return false;
        exeFile = Path.Combine(gameDirectory, "starsector.exe");
        if (File.Exists(exeFile) is false)
            return false;
        return true;
    }

    private static string TryGetGameVersion(string logFile)
    {
        if (File.Exists(logFile) is false)
        {
            File.Create(logFile).Close();
            return string.Empty;
        }
        try
        {
            if (CheckGameVersion(logFile) is string version)
                return version;
            for (int i = 1; i < 10; i++)
            {
                var gFile = $"{LogFile}.{i}";
                if (File.Exists(gFile) is false)
                    continue;
                if (CheckGameVersion(gFile) is string versionG)
                    return versionG;
            }
        }
        catch (Exception ex)
        {
            sr_logger.Error(ex, I18nRes.GameVersionAccessFailed);
        }
        return string.Empty;

        static string? CheckGameVersion(string logFile)
        {
            // 因为游戏可能会处于运行状态,所以使用只读打开日志文件
            using var sr = Utils.StreamReaderOnReadOnly(logFile);
            foreach (var line in Utils.GetLinesOnStreamReader(sr))
            {
                if (
                    s_checkLauncher.Match(line).Value is string launcherData
                    && string.IsNullOrWhiteSpace(launcherData) is false
                )
                    return s_checkVersion.Match(launcherData).Value;
            }
            return null;
        }
    }

    /// <summary>
    /// 获取游戏目录
    /// </summary>
    /// <returns>获取成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    internal static bool GetGameDirectory()
    {
        var fileNames = OpenFileDialogVM.Show(
            new() { Filter = $"Exe {I18nRes.File}|starsector.exe" }
        );
        if (fileNames?.Any() is true && fileNames.First() is string fileName)
        {
            string directory = Path.GetDirectoryName(fileName)!;
            if (SetGameData(directory))
            {
                sr_logger.Info($"{I18nRes.GameDirectorySetCompleted} {I18nRes.Path}: {directory}");
                return true;
            }
        }
        return false;
    }

    private static string TryGetJavaFile(string baseDirectory)
    {
        string javeFile = Path.Combine(baseDirectory, "jre", "bin", "java.exe");
        if (File.Exists(javeFile))
            return javeFile;
        return string.Empty;
    }

    private static string TryGetJaveVersion(string javaFile)
    {
        if (string.IsNullOrWhiteSpace(javaFile))
            return string.Empty;
        var psi = new ProcessStartInfo();
        psi.FileName = javaFile;
        psi.CreateNoWindow = true;
        psi.RedirectStandardError = true;
        psi.Arguments = "-version";
        using var p = Process.Start(psi);
        if (p is null)
            return string.Empty;
        var message = p.StandardError.ReadToEnd();
        var version = Regex.Match(message, @"(?<=java version "")[^""]*").Value;
        p.WaitForExit();
        return version;
    }
}
