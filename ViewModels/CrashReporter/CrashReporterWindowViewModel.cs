using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.HKWViewModels;
using HKW.HKWViewModels.Controls;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;

namespace StarsectorToolbox.ViewModels.CrashReporter;

internal partial class CrashReporterWindowViewModel : WindowVM
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private bool _closeToHide = true;

    [ObservableProperty]
    private string _crashReport = string.Empty;

    [ObservableProperty]
    private string _lastLog = string.Empty;

    public CrashReporterWindowViewModel()
        : base(new()) { }

    public CrashReporterWindowViewModel(object window)
        : base(window)
    {
        DataContext = this;
        Closing += (s, e) =>
        {
            if (CloseToHide is false)
                return;
            e.Cancel = true;
            Hide();
        };
    }

    public void ForcedClose()
    {
        CloseToHide = false;
        Close();
    }

    [RelayCommand]
    public void RefreshCrashReport()
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
