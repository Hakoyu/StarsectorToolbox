using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using HKW.Libs.TomlParse;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Windows.MainWindow;
using I18n = StarsectorTools.Langs.Pages.Settings.SettingsPageI18nRes;

namespace StarsectorTools.Pages.Settings
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        internal SettingsPageViewModel ViewModel => (SettingsPageViewModel)DataContext;
        internal SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsPageViewModel(true);
        }
    }
}