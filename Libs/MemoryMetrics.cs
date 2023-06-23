using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32.System.SystemInformation;

namespace StarsectorToolbox.Libs;

///<summary>内存指标</summary>
[DebuggerDisplay("TotalPhys = {TotalPhys}, Load = {Load}")]
public readonly struct MemoryMetrics
{
    /// <summary>
    /// 内存使用百分比
    /// </summary>
    public readonly uint Load;

    /// <summary>
    /// 物理内存总量
    /// </summary>
    public readonly ulong TotalPhys;

    /// <summary>
    /// 物理内存使用量
    /// </summary>
    public readonly ulong UsedPhys;

    /// <summary>
    /// 物理内存可用量
    /// </summary>
    public readonly ulong AvailPhys;

    /// <summary>
    /// 虚拟内存总量
    /// </summary>
    public readonly ulong TotalVirtual;

    /// <summary>
    /// 虚拟内存使用量
    /// </summary>
    public readonly ulong UsedVirtual;

    /// <summary>
    /// 虚拟内存可用量
    /// </summary>
    public readonly ulong AvailVirtual;

    /// <summary>
    /// 总页数
    /// </summary>
    public readonly ulong TotalPageFile;

    /// <summary>
    /// 已用页数
    /// </summary>
    public readonly ulong UsedPageFile;

    /// <summary>
    /// 可用页数
    /// </summary>
    public readonly ulong AvailPageFile;

    private const double c_size = 1024.0;

    internal MemoryMetrics(Windows.Win32.System.SystemInformation.MEMORYSTATUSEX memoryStatusEx)
    {
        Load = memoryStatusEx.dwMemoryLoad;
        TotalPhys = memoryStatusEx.ullTotalPhys;
        UsedPhys = memoryStatusEx.ullTotalPhys - memoryStatusEx.ullAvailPhys;
        AvailPhys = memoryStatusEx.ullAvailPhys;
        TotalVirtual = memoryStatusEx.ullTotalVirtual;
        UsedVirtual = memoryStatusEx.ullTotalVirtual - memoryStatusEx.ullAvailVirtual;
        AvailVirtual = memoryStatusEx.ullAvailVirtual;
        TotalPageFile = memoryStatusEx.ullTotalPageFile;
        UsedPageFile = memoryStatusEx.ullTotalPageFile - memoryStatusEx.ullAvailPageFile;
        AvailPageFile = memoryStatusEx.ullAvailPageFile;
    }

    /// <summary>
    /// 获取内存信息
    /// </summary>
    /// <returns>内存信息</returns>
    public static MemoryMetrics? GetMemoryMetrics()
    {
        var memoryStatusEx = new MEMORYSTATUSEX();
        memoryStatusEx.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        if (Windows.Win32.PInvoke.GlobalMemoryStatusEx(out memoryStatusEx))
            return new MemoryMetrics(memoryStatusEx);
        else
            return null;
    }

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
    public static double ByteToKB(ulong size)
    {
        return size / c_size;
    }

    public static double ByteToMB(ulong size)
    {
        return size / c_size / c_size;
    }

    public static double ByteToGB(ulong size)
    {
        return size / c_size / c_size / c_size;
    }

    public static double ByteToTB(ulong size)
    {
        return size / c_size / c_size / c_size / c_size;
    }

    public override string ToString()
    {
        return $"TotalPhys = {TotalPhys}, Load = {Load}";
    }
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释
}
