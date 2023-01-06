using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace HKW.Management
{
    ///<summary>内存指标</summary>
    internal class MemoryMetrics
    {
        ///<summary>内存总量</summary>
        public int Total;

        ///<summary>内存使用量</summary>
        public int Used;

        ///<summary>内存剩余量</summary>
        public int Free;
    }

    ///<summary>系统</summary>
    internal partial class Management
    {
        public Management()
        {
            if (!WMIInitialize())
                throw new ArgumentNullException(ToString(), "Initialization failure");
            if (!GetsSystemType)
                throw new ArgumentNullException(ToString(), "This is not Windows");
        }

        public void Close()
        {
            process?.Close();
            process = null!;
        }

        private Process process = null!;
        private bool GetsSystemType => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private bool WMIInitialize()
        {
            process = new();
            process.StartInfo.FileName = "wmic";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            if (process.Start())
                return true;
            return false;
        }

        public MemoryMetrics? GetMemoryMetrics()
        {
            if (process == null)
                return null;
            MemoryMetrics mm = new();
            process.StandardInput.WriteLine("OS get FreePhysicalMemory,TotalVisibleMemorySize /Value");
            List<string> lines = new();
            string output = "";
            for (int i = 0; i < 14; output = process.StandardOutput.ReadLine()!, i++)
                if (output.Any())
                    lines.Add(output.Split("=")[^1]);
            mm.Total = int.Parse(lines[2]) / 1024;
            mm.Free = int.Parse(lines[1]) / 1024;
            mm.Used = mm.Total - mm.Free;
            return mm;
        }

        public static MemoryMetrics GetMemoryMetricsNow()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new ArgumentNullException(GetMemoryMetricsNow().ToString(), "This is not Windows");
            Process process = new();
            process.StartInfo.FileName = "wmic";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            if (!process.Start())
                throw new ArgumentNullException(GetMemoryMetricsNow().ToString(), "Initialization failure");
            process.StandardInput.WriteLine("OS get FreePhysicalMemory,TotalVisibleMemorySize /Value");
            MemoryMetrics mm = new();
            List<string> lines = new();
            string output = "";
            for (int i = 0; i < 14; output = process.StandardOutput.ReadLine()!, i++)
                if (output.Any())
                    lines.Add(output.Split("=")[^1]);
            mm.Total = int.Parse(lines[2]) / 1024;
            mm.Free = int.Parse(lines[1]) / 1024;
            mm.Used = mm.Total - mm.Free;
            return mm;
        }
    }
}