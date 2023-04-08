using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HKW.Libs.Log4Cs;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.Models.ModInfo;
using StarsectorToolbox.Models.System;
using I18nRes = StarsectorToolbox.Langs.Windows.CrashReporter.CrashReporterI18nRes;

namespace StarsectorToolbox.ViewModels.CrashReporter;

internal partial class CrashReporterWindowViewModel
{
    public void ListeningProcess()
    {
        var process = GetGameProcess();
        if (process is null)
            return;
        process.EnableRaisingEvents = true;
        process.Exited += (s, e) =>
        {
            var exitCode = process.ExitCode;
        };
    }

    private static Process? GetGameProcess()
    {
        try
        {
            return Process.GetProcessesByName("java").FirstOrDefault(p => p.MainModule?.FileName == GameInfo.JavaFile);
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
        sb.AppendLine($"{I18nRes.GameVersion}:    {GameInfo.Version}");
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
        sb.AppendLine(CreateModsInfo());
        return sb.ToString();
    }

    private static string CreateModsInfo()
    {
        if (ModInfos.GetCurrentEnabledModIds() is not string[] modIdsArray)
            return string.Empty;
        var modIds = modIdsArray.ToHashSet();
        // 获取内容宽度
        var idMaxLength = ModInfos.AllModInfos.Keys.Max(s => Encoding.UTF8.GetBytes(s).Length);
        var nameMaxLength = ModInfos.AllModInfos.Values.Max(info => Encoding.UTF8.GetBytes(info.Name).Length);
        var versionMaxLength = ModInfos.AllModInfos.Values.Max(info => Encoding.UTF8.GetBytes(info.Version).Length);
        var sb = new StringBuilder("");

        sb.Append(string.Format($"{{0,-{nameMaxLength}}}", I18nRes.ModName));
        sb.Append(" | ");
        sb.Append(string.Format($"{{0,-{idMaxLength}}}", I18nRes.ModId));
        sb.Append(" | ");
        sb.Append(string.Format($"{{0,-{versionMaxLength}}}", I18nRes.ModVersion));
        sb.Append(" | ");
        sb.Append(I18nRes.ModIsEnabled);
        sb.AppendLine();
        sb.AppendLine(new string('=', idMaxLength + nameMaxLength + versionMaxLength + 9));

        foreach (var modInfo in ModInfos.AllModInfos.Values)
        {
            sb.Append(string.Format($"{{0,-{nameMaxLength - (Encoding.UTF8.GetBytes(modInfo.Name).Length - modInfo.Name.Length) / 2}}}", modInfo.Name));
            sb.Append(" | ");
            sb.Append(string.Format($"{{0,-{idMaxLength - (Encoding.UTF8.GetBytes(modInfo.Id).Length - modInfo.Id.Length) / 2}}}", modInfo.Id));
            sb.Append(" | ");
            sb.Append(string.Format($"{{0,-{versionMaxLength - (Encoding.UTF8.GetBytes(modInfo.Version).Length - modInfo.Version.Length) / 2}}}", modInfo.Version));
            sb.Append(" | ");
            sb.Append(modIds.Contains(modInfo.Id) ? I18nRes.Yes : I18nRes.No);
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }
}
