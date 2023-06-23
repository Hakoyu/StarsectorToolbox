using System.Management;
using System.Runtime.InteropServices;
using StarsectorToolbox.Libs;
using I18nRes = StarsectorToolbox.Langs.Models.ModelsI18nRes;

namespace StarsectorToolbox.Models.System;

/// <summary>
/// 系统信息
/// </summary>
public class SystemInfo
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 平台名称
    /// </summary>
    public static string PlatformName { get; private set; } = string.Empty;

    /// <summary>
    /// 平台结构
    /// </summary>
    public static string PlatformArchitecture { get; private set; } = string.Empty;

    /// <summary>
    /// 平台版本
    /// </summary>
    public static string PlatformVersion { get; private set; } = string.Empty;

    /// <summary>
    /// 内存总量 (MB为单位)
    /// </summary>
    public static uint TotalMemory { get; private set; } = 0;

    internal static void Initialize()
    {
        if (MemoryMetrics.GetMemoryMetrics() is MemoryMetrics memoryMetrics)
            TotalMemory = (uint)MemoryMetrics.ByteToMB(memoryMetrics.TotalVirtual);
        // TODO: GetMemoryMetrics Fail
        TryGetPlatformInfo();
    }

    private static void TryGetPlatformInfo()
    {
        GetPlatformInfo(
            out var platformName,
            out var platformArchitecture,
            out var platformVersion
        );
        PlatformName = platformName;
        PlatformArchitecture = platformArchitecture;
        PlatformVersion = platformVersion;
    }

    private static void GetPlatformInfo(
        out string platformName,
        out string platformArchitecture,
        out string platformVersion
    )
    {
        platformName = string.Empty;
        platformArchitecture = string.Empty;
        platformVersion = string.Empty;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                GetWindowsInfo(out platformName, out platformArchitecture, out platformVersion);
            return;
        }
        catch (Exception ex)
        {
            sr_logger.Error(I18nRes.SystemInfoAcquisitionFailed, ex);
            return;
        }
    }

    //#pragma warning disable CA1416
    private static void GetWindowsInfo(
        out string platformName,
        out string platformArchitecture,
        out string platformVersion
    )
    {
        platformName = string.Empty;
        platformArchitecture = string.Empty;
        platformVersion = string.Empty;
        using var mos = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
        var moc = mos.Get();
        if (moc is null)
            return;
        foreach (var obj in moc)
        {
            platformName = obj["Caption"].ToString()!;
            platformArchitecture = obj["OSArchitecture"].ToString()!;
            platformVersion = obj["Version"].ToString()!;
        }
    }
    //#pragma warning restore CA1416
}
