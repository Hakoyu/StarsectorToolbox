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
        var checkLauncher = new Regex(@"Starting Starsector [^ ]+ launcher");
        var checkVersion = new Regex(@"[0-9]+.[0-9]+[^ ]*");
        try
        {
            using (var sr = new StreamReader(new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                if (sr.ReadLine() is string s && checkLauncher.IsMatch(s))
                    return checkVersion.Match(s).Value;
            };
            for (int i = 1; i < 10; i++)
            {
                var gFile = $"{LogFile}.{i}";
                if (File.Exists(gFile) is false)
                    continue;
                using (var sr = new StreamReader(new FileStream(gFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    if (sr.ReadLine() is string s && checkLauncher.IsMatch(s))
                        return checkVersion.Match(s).Value;
                };
            }
        }
        catch (Exception ex)
        {
            Logger.Error("", ex);
        }
        return string.Empty;
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
