using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HKW.ViewModels.Controls;

namespace StarsectorToolbox.ViewModels.CrashReporter;

internal class CrashReporterWindowViewModel : WindowVM
{
    public CrashReporterWindowViewModel(object window) : base(window)
    {
        ListeningProcess();
    }

    public void ListeningProcess()
    {
        var process = Process.GetProcessesByName("java").FirstOrDefault(p => p.MainModule?.FileName == @"D:\Games\Starsector\jre\bin\java.exe");
        if (process is null)
            return;
        process.EnableRaisingEvents = true;
        process.Exited += (s, e) =>
        {
            var exitCode = process.ExitCode;
        };
    }
}
