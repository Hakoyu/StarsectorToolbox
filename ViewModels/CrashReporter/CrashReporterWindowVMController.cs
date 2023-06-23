using System.Diagnostics;
using System.IO;
using System.Text;
using HKW.HKWViewModels;
using HKW.HKWViewModels.Dialogs;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.Models.ModInfo;
using StarsectorToolbox.Models.System;
using I18nRes = StarsectorToolbox.Langs.Windows.CrashReporter.CrashReporterI18nRes;

namespace StarsectorToolbox.ViewModels.CrashReporter;

internal partial class CrashReporterWindowViewModel
{
    private Process? _gameProcess = null;
    private static int s_idMaxLength;
    private static Dictionary<
        string,
        (int IdLength, int NameLength, int VersionLength)
    > s_ModsBytesLength = null!;
    private static int s_nameMaxLength = 0;
    private static int s_versionMaxLength = 0;

    public async Task ListeningGameAsync()
    {
        if (_gameProcess is not null)
            return;
        _gameProcess ??= GetGameProcess();
        if (_gameProcess is null)
            return;
        _gameProcess.EnableRaisingEvents = true;
        var exitCode = 0;
        _gameProcess.Exited += (s, e) =>
        {
            exitCode = _gameProcess.ExitCode;
        };
        await _gameProcess.WaitForExitAsync();
        sr_logger.Info($"Game exit code: {exitCode}");
        GameExited(exitCode);
        _gameProcess = null;
    }

    private void GameExited(int exitCode)
    {
        if (exitCode is 0)
            return;
        if (
            MessageBoxVM.Show(
                new(I18nRes.GameAbnormalExit)
                {
                    Icon = MessageBoxVM.Icon.Question,
                    Button = MessageBoxVM.Button.YesNo
                }
            ) is MessageBoxVM.Result.No
        )
            return;
        SetCrashReport();
        Show();
    }

    private void SetCrashReport()
    {
        CrashReport = CreateCrashReport();
        LastLog = GetLastLog(GameInfo.LogFile);
    }

    private static Process? GetGameProcess()
    {
        try
        {
            return Process
                .GetProcessesByName("java")
                .FirstOrDefault(p => p.MainModule?.FileName == GameInfo.JavaFile);
        }
        catch
        {
            return null;
        }
    }

    private static string CreateCrashReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{I18nRes.GameInfo}]");
        sb.AppendLine($"{I18nRes.GameVersion}: {GameInfo.Version}");
        sb.AppendLine();
        sb.AppendLine($"[{I18nRes.SystemInfo}]");
        sb.AppendLine($"{I18nRes.Platform}: {SystemInfo.PlatformName}");
        sb.AppendLine($"{I18nRes.PlatformVersion}: {SystemInfo.PlatformVersion}");
        sb.AppendLine($"{I18nRes.PlatformArchitecture}: {SystemInfo.PlatformArchitecture}");
        sb.AppendLine($"{I18nRes.TotalMemory}: {SystemInfo.TotalMemory}M");
        sb.AppendLine();
        sb.AppendLine($"{I18nRes.VmparamsData}: {File.ReadAllText(GameInfo.VmparamsFile)}");
        sb.AppendLine($"{I18nRes.JavaVersion}: {GameInfo.JaveVersion}");
        sb.AppendLine($"{I18nRes.JavaPath}: {GameInfo.JavaFile}");
        sb.AppendLine();
        sb.AppendLine($"[{I18nRes.ModsInfo}]");
        sb.Append(CreateModsInfo(ModInfos.AllModInfos, ModInfos.GetCurrentEnabledModIds()));
        sb.AppendLine();
        return sb.ToString();
    }

    private static StringBuilder CreateModsInfo(
        IReadOnlyDictionary<string, ModInfo> allModInfos,
        IEnumerable<string>? enabledModIds
    )
    {
        if (enabledModIds is null)
            return null!;
        var enabledModIdSet = enabledModIds.ToHashSet();
        TryGetBytesLength(allModInfos.Values);

        var sb = new StringBuilder();
        sb.Append(CheckModContains(allModInfos, enabledModIds));
        sb.Append(GetModInfoHead());

        // 排序: 已启用,名称
        foreach (
            var modInfo in allModInfos.Values
                .OrderByDescending(info => enabledModIdSet.Contains(info.Id))
                .ThenBy(info => info.Name)
        )
        {
            // 拼接模组信息
            sb.Append(
                string.Format(
                    $"{{0,-{s_nameMaxLength - 4 - (s_ModsBytesLength[modInfo.Id].NameLength - modInfo.Name.Length) / 2}}}",
                    modInfo.Name
                )
            );
            sb.Append(" | ");
            sb.Append(
                string.Format(
                    $"{{0,-{s_idMaxLength - (s_ModsBytesLength[modInfo.Id].IdLength - modInfo.Id.Length) / 2}}}",
                    modInfo.Id
                )
            );
            sb.Append(" | ");
            sb.Append(
                string.Format(
                    $"{{0,-{s_versionMaxLength - (s_ModsBytesLength[modInfo.Id].VersionLength - modInfo.Version.Length) / 2}}}",
                    modInfo.Version
                )
            );
            sb.Append(" | ");
            sb.Append(enabledModIdSet.Contains(modInfo.Id) ? I18nRes.Yes : I18nRes.No);
            sb.AppendLine();
        }
        return sb;
    }

    private static StringBuilder? CheckModContains(
        IReadOnlyDictionary<string, ModInfo> allModInfos,
        IEnumerable<string> enabledModIds
    )
    {
        var sb = new StringBuilder();
        foreach (var id in enabledModIds)
        {
            if (allModInfos.ContainsKey(id) is false)
                sb.AppendJoin(", ", id);
        }
        if (sb.Length > 0)
        {
            sb.Remove(sb.Length - 2, 2);
            sb.Insert(0, I18nRes.EnabledModNotInstalled + ":\n");
            sb.AppendLine();
            sb.AppendLine();
            return sb;
        }
        return null;
    }

    private static StringBuilder GetModInfoHead()
    {
        var sb = new StringBuilder();
        // 表头
        sb.Append(
            string.Format(
                $"{{0,-{s_nameMaxLength - 4 - (Encoding.UTF8.GetBytes(I18nRes.ModName).Length - I18nRes.ModName.Length) / 2}}}",
                I18nRes.ModName
            )
        );
        sb.Append(" | ");
        sb.Append(
            string.Format(
                $"{{0,-{s_idMaxLength - (Encoding.UTF8.GetBytes(I18nRes.ModId).Length - I18nRes.ModId.Length) / 2}}}",
                I18nRes.ModId
            )
        );
        sb.Append(" | ");
        sb.Append(
            string.Format(
                $"{{0,-{s_versionMaxLength - (Encoding.UTF8.GetBytes(I18nRes.ModVersion).Length - I18nRes.ModVersion.Length) / 2}}}",
                I18nRes.ModVersion
            )
        );
        sb.Append(" | ");
        sb.Append(I18nRes.ModIsEnabled);
        sb.AppendLine();
        sb.AppendLine(new string('=', s_idMaxLength + s_nameMaxLength + s_versionMaxLength + 16));
        return sb;
    }

    private static void TryGetBytesLength(IEnumerable<ModInfo> allModInfos)
    {
        if (
            s_ModsBytesLength is null
            || s_ModsBytesLength.Count != allModInfos.Count()
            || s_ModsBytesLength.Keys.Except(allModInfos.Select(i => i.Id)).Count() is not 0
        )
        {
            GetBytesLength(allModInfos);
        }
    }

    private static void GetBytesLength(IEnumerable<ModInfo> allModInfos)
    {
        s_ModsBytesLength = allModInfos.ToDictionary(
            info => info.Id,
            info =>
                (
                    Encoding.UTF8.GetBytes(info.Id).Length,
                    Encoding.UTF8.GetBytes(info.Name).Length,
                    Encoding.UTF8.GetBytes(info.Version).Length
                )
        );
        s_idMaxLength = s_ModsBytesLength.Values.Max(i => i.IdLength);
        s_nameMaxLength = s_ModsBytesLength.Values.Max(i => i.NameLength);
        s_versionMaxLength = s_ModsBytesLength.Values.Max(i => i.VersionLength);
    }

    private static string GetLastLog(string file)
    {
        if (File.Exists(file) is false)
            return $"!!! {I18nRes.LogFileNotExist} !!!";
        var lines = GetLines(file);
        if (lines.Length < 100)
            return string.Join("\n", lines);
        else
            return string.Join("\n", lines[^100..]);
    }

    private static string[] GetLines(string file)
    {
        if (ObservableI18n.Language == "zh-CN")
        {
            var EncodingGBK = Encoding.GetEncoding("GBK");
            using var sr = Utils.StreamReaderOnReadOnly(file, EncodingGBK);
            return Utils
                .GetLinesOnStreamReader(sr)
                .Select(s =>
                {
                    var bytes = Encoding.Convert(
                        EncodingGBK,
                        Encoding.UTF8,
                        EncodingGBK.GetBytes(s)
                    );
                    return Encoding.UTF8.GetString(bytes);
                })
                .ToArray();
        }
        else
            return File.ReadLines(file).ToArray();
    }
}
