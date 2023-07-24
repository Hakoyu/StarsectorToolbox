using System.Windows.Controls;
using HKW.HKWViewModels.Controls;
using Microsoft.Win32;
using Panuon.WPF.UI;
using StarsectorToolbox.Models.ST;
using StarsectorToolbox.ViewModels.Settings;
using I18nRes = StarsectorToolbox.Langs.Pages.Settings.SettingsPageI18nRes;

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

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // TODO: 监视注册表以跟随系统的黑暗模式切换
        if (e.AddedItems[0] is not ComboBoxItemVM item)
            return;
        if (item.Tag is not string themeName)
            return;
        STSettings.Instance.Theme = themeName;
        STSettings.Save();
        if (themeName == nameof(I18nRes.WindowsDefault))
            themeName = WindowsThemeIsLight() ? nameof(I18nRes.Light) : nameof(I18nRes.Dark);
        GlobalSettings.ChangeTheme(themeName);
    }

    private static bool WindowsThemeIsLight()
    {
        var result = Registry.GetValue(
            "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
            "AppsUseLightTheme",
            -1
        );
        return Convert.ToBoolean(result);
    }
}
