using System.Windows.Controls;
using StarsectorToolbox.ViewModels.Settings;

namespace StarsectorToolbox.Views.Settings;

/// <summary>
/// Settings.xaml 的交互逻辑
/// </summary>
internal partial class SettingsPage : Page
{
    internal SettingsPageViewModel ViewModel => (SettingsPageViewModel)DataContext;

    internal SettingsPage()
    {
        InitializeComponent();
        DataContext = new SettingsPageViewModel(true);
    }
}