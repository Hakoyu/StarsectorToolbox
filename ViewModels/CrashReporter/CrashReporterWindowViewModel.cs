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
using StarsectorToolbox.Models.ModInfo;

namespace StarsectorToolbox.ViewModels.CrashReporter;

internal partial class CrashReporterWindowViewModel : WindowVM
{
    [ObservableProperty]
    private bool _closeToHide = false;
    [ObservableProperty]
    private string _crashReport = string.Empty;
    [ObservableProperty]
    private string _lastLog = string.Empty;
    public CrashReporterWindowViewModel() : base(new()) { }
    public CrashReporterWindowViewModel(object window) : base(window)
    {
        DataContext = this;
        Closing += (s, e) =>
        {
            if (CloseToHide is false)
                return;
            e.Cancel = true;
            Hide();
        };
        SetCrashReport();
    }

    public void ForcedClose()
    {
        CloseToHide = false;
        Close();
    }

    [RelayCommand]
    private void RefreshCrashReport()
    {
        SetCrashReport();
        //CrashReport = CreateModsInfo(ModInfos.AllModInfos.OrderBy(
        //    s => new Random(Guid.NewGuid().GetHashCode()).Next()
        //).Take(20).ToDictionary(i => i.Key, i => i.Value), ModInfos.AllEnabledModIds).ToString();
    }
    [RelayCommand]
    private static void OpenGameLog()
    {
        Utils.OpenLink(GameInfo.LogFile);
    }

    [RelayCommand]
    private void CopyCrashReport()
    {
        ClipboardVM.SetText(CrashReport + LastLog);
    }
}
