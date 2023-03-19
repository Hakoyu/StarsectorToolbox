using System.Windows.Controls;
using StarsectorTools.ViewModels.SettingsPage;

namespace StarsectorTools.Views.SettingsPage;

/// <summary>
/// SettingsPage.xaml 的交互逻辑
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