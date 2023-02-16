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
            ShowCurrentLanguage();
            ShowCurrentLogLevel();
            DataContext = new SettingsPageViewModel(true);
            isFirst = false;
        }

        private void ShowCurrentLanguage()
        {
            //foreach (ComboBoxItem item in ComboBox_Language.Items)
            //{
            //    if (item.ToolTip.ToString() == Thread.CurrentThread.CurrentUICulture.Name)
            //    {
            //        ComboBox_Language.SelectedItem = item;
            //        return;
            //    }
            //}
            //ComboBox_Language.SelectedIndex = 0;
        }

        private void ShowCurrentLogLevel()
        {
            //foreach (ComboBoxItem item in ComboBox_LogLevel.Items)
            //{
            //    if (item.ToolTip.ToString() == STLog.ComboBox_LogLevel.ToString())
            //    {
            //        ComboBox_LogLevel.SelectedItem = item;
            //        return;
            //    }
            //}
            //ComboBox_LogLevel.SelectedIndex = 1;
        }

        private void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isFirst)
                return;
            if (e.AddedItems[0] is ComboBoxItem item)
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(item.ToolTip.ToString()!);
                SaveLanguage(Thread.CurrentThread.CurrentUICulture);
                ChangeLanguage();
                //MainWindow.ViewModel.ChangeLanguage();
                STLog.WriteLine($"{I18n.LanguageSwitch}: {Thread.CurrentThread.CurrentUICulture.Name}");
            }
        }

        private void ChangeLanguage()
        {
            GroupBox_Settings.Header = I18n.Settings;
            Button_OpenLogFile.Content = I18n.OpenLogFile;
            Label_LogLevel.Content = I18n.LogLevel;
            Label_Language.Content = I18n.Language;
        }

        private void SaveLanguage(CultureInfo cultureInfo)
        {
            try
            {
                TomlTable toml = TOML.Parse(ST.ConfigTomlFile);
                toml["Extras"]["Lang"] = cultureInfo.Name;
                toml.SaveTo(ST.ConfigTomlFile);
            }
            catch
            {
                MessageBox.Show(I18n.ConfigFileError);
            }
        }

        private void Button_OpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                Utils.OpenLink(STLog.LogFile);
            }
        }

        private void ComboBox_LogLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isFirst)
                return;
            if (ComboBox_LogLevel.SelectedItem is ComboBoxItem item)
            {
                SaveLogLevel(item.ToolTip.ToString()!);
                STLog.WriteLine($"{I18n.LogLevelSwitch}: {item.ToolTip}");
            }
        }

        private void SaveLogLevel(string level)
        {
            try
            {
                STLog.SetLogLevel(STLog.GetSTLogLevel(level));
                TomlTable toml = TOML.Parse(ST.ConfigTomlFile);
                toml["Extras"]["LogLevel"] = level;
                toml.SaveTo(ST.ConfigTomlFile);
            }
            catch
            {
                MessageBox.Show(I18n.ConfigFileError);
            }
        }

        private void Button_SetExtensionDebugPath_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = I18n.SelectDebugFile,
                Filter = $"Toml {I18n.File}|Extension.toml"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                string path = Path.GetDirectoryName(openFileDialog.FileName)!;
                TextBox_ExtensionDebugPath.Text = path;
                TomlTable toml = TOML.Parse(ST.ConfigTomlFile);
                toml["Extension"]["DebugPath"] = path;
                toml.SaveTo(ST.ConfigTomlFile);
                STLog.WriteLine($"{I18n.SetExtensionDebugPath}: {path}");
                //if (Utils.ShowMessageBox(I18nRes.EffectiveAfterReload, MessageBoxButton.YesNo, STMessageBoxIcon.Question) == MessageBoxResult.Yes)
                //    MainWindowViewModel.Instance.RefreshExtensionMenu();
            }
        }

        private void Button_ClearExtensionDebugPath_Click(object sender, RoutedEventArgs e)
        {
            TextBox_ExtensionDebugPath.Text = "";
            TomlTable toml = TOML.Parse(ST.ConfigTomlFile);
            toml["Extension"]["DebugPath"] = "";
            toml.SaveTo(ST.ConfigTomlFile);
            STLog.WriteLine(I18n.ClearExtensionDebugPath);
        }
    }
}