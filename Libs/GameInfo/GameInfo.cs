using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HKW.Libs.Log4Cs;
using HKW.ViewModels.Dialogs;
using I18n = StarsectorTools.Langs.Libs.UtilsI18nRes;

namespace StarsectorTools.Libs.GameInfo;

/// <summary>游戏信息</summary>
public static class GameInfo
{
    /// <summary>游戏目录</summary>
    public static string BaseDirectory { get; private set; } = null!;

    /// <summary>游戏Core目录</summary>
    public static string CoreDirectory { get; private set; } = null!;

    /// <summary>游戏exe文件</summary>
    public static string ExeFile { get; private set; } = null!;

    /// <summary>游戏模组文件夹</summary>
    public static string ModsDirectory { get; private set; } = null!;

    /// <summary>游戏存档文件夹</summary>
    public static string SaveDirectory { get; private set; } = null!;

    /// <summary>游戏已启用模组文件</summary>
    public static string EnabledModsJsonFile { get; private set; } = null!;

    /// <summary>游戏日志文件</summary>
    public static string LogFile { get; private set; } = null!;

    /// <summary>游戏版本</summary>
    public static string Version { get; private set; } = null!;

    /// <summary>游戏设置文件</summary>
    public static string SettingsFile { get; private set; } = null!;

    private static readonly Regex _checkLauncher = new(@"Starting Starsector [^ ]+ launcher");
    private static readonly Regex _checkVersion = new(@"[0-9]+.[0-9]+[^ ]*");

    /// <summary>
    /// 设置游戏信息
    /// </summary>
    /// <param name="directoryName">游戏目录</param>
    internal static bool SetGameData(string directoryName)
    {
        if (string.IsNullOrWhiteSpace(directoryName))
        {
            Logger.Error(I18n.GameDirectoryIsEmpty);
            MessageBoxVM.Show(new(I18n.GameDirectoryIsEmpty) { Icon = MessageBoxVM.Icon.Error });
            return false;
        }
        var exeFile = Path.Combine(directoryName, "starsector.exe");
        if (File.Exists(exeFile) is false)
        {
            Logger.Error($"{I18n.GameDirectoryError} {I18n.Path}: {directoryName}");
            MessageBoxVM.Show(
                new($"{I18n.GameDirectoryError}\n{I18n.Path}") { Icon = MessageBoxVM.Icon.Error }
            );
            return false;
        }
        ExeFile = exeFile;
        BaseDirectory = directoryName;
        ModsDirectory = Path.Combine(directoryName, "mods");
        SaveDirectory = Path.Combine(directoryName, "saves");
        CoreDirectory = Path.Combine(directoryName, "starsector-core");
        EnabledModsJsonFile = Path.Combine(ModsDirectory, "enabled_mods.json");
        LogFile = Path.Combine(CoreDirectory, "starsector.log");
        SettingsFile = Path.Combine(CoreDirectory, "data", "config", "settings.json");
        Version = TryGetGameVersion(LogFile);
        if (string.IsNullOrWhiteSpace(Version))
        {
            Logger.Info(I18n.GameVersionAccessFailed);
            MessageBoxVM.Show(new(I18n.GameVersionAccessFailedMessage));
        }
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
            Logger.Error("", ex);
        }
        return string.Empty;

        static string? CheckGameVersion(string logFile)
        {
            // 因为游戏可能会处于运行状态,所以使用只读打开日志文件
            using (
                var sr = new StreamReader(
                    logFile,
                    new FileStreamOptions()
                    {
                        Access = FileAccess.Read,
                        Mode = FileMode.Open,
                        Share = FileShare.ReadWrite
                    }
                )
            )
            {
                var data = sr.ReadToEnd();
                if (_checkLauncher.Match(data).Value is string launcherData)
                    return _checkVersion.Match(launcherData).Value;
            }
            ;
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
            new() { Filter = $"Exe {I18n.File}|starsector.exe" }
        )!;
        if (fileNames.Any() && fileNames.First() is string fileName)
        {
            string directory = Path.GetDirectoryName(fileName)!;
            if (SetGameData(directory))
            {
                Logger.Info($"{I18n.GameDirectorySetCompleted} {I18n.Path}: {directory}");
                return true;
            }
        }
        return false;
    }
}
