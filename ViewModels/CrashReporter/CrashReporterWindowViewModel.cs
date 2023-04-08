using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;

namespace StarsectorToolbox.ViewModels.CrashReporter;

internal partial class CrashReporterWindowViewModel : WindowVM
{
    [ObservableProperty]
    private string _crashReport = string.Empty;
    public CrashReporterWindowViewModel() : base(new()) { }
    public CrashReporterWindowViewModel(object window) : base(window)
    {
        DataContext = this;
        //ListeningProcess();
        SetCrashReport();
    }
    [RelayCommand]
    private void SetCrashReport()
    {
        CrashReport = CreateCrashReport();
    }
    [RelayCommand]
    private static void OpenGameLog()
    {
        Utils.OpenLink(GameInfo.LogFile);
    }

    [RelayCommand]
    private void CopyCrashReport()
    {
        ClipboardVM.SetText(CrashReport);
    }
}
