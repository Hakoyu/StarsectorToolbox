using Panuon.WPF.UI;

namespace StarsectorToolbox.Views.CrashReporter;

/// <summary>
/// CrashReporterWindow.xaml 的交互逻辑
/// </summary>
internal partial class CrashReporterWindow : WindowX
{
    public CrashReporterWindow()
    {
        InitializeComponent();
    }
    //protected override void OnClosing(CancelEventArgs e)
    //{
    //    if (CloseToHide is false)
    //        return;
    //    e.Cancel = true;
    //    Hide();
    //}
}
