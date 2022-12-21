using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using HKW.TomlParse;
using StarsectorTools.Libs;
using StarsectorTools.Windows;
using I18n = StarsectorTools.Langs.Pages.Settings_I18n;

namespace StarsectorTools.Pages
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Page
    {
        private bool isFirst = true;

        public Settings()
        {
            InitializeComponent();
            ShowCurrentLanguage();
            ShowCurrentLogLevel();
            isFirst = false;
        }

        private void ShowCurrentLanguage()
        {
            foreach (ComboBoxItem item in ComboBox_Language.Items)
            {
                if (item.ToolTip.ToString() == Thread.CurrentThread.CurrentUICulture.Name)
                {
                    ComboBox_Language.SelectedItem = item;
                    return;
                }
            }
            ComboBox_Language.SelectedIndex = 0;
        }

        private void ShowCurrentLogLevel()
        {
            foreach (ComboBoxItem item in ComboBox_LogLevel.Items)
            {
                if (item.ToolTip.ToString() == STLog.Instance.LogLevel.ToString())
                {
                    ComboBox_LogLevel.SelectedItem = item;
                    return;
                }
            }
            ComboBox_LogLevel.SelectedIndex = 1;
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
                ((MainWindow)Application.Current.MainWindow).ChangeLanguage();
                STLog.Instance.WriteLine($"{I18n.LanguageSwitch}: {Thread.CurrentThread.CurrentUICulture.Name}");
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
                TomlTable toml = TOML.Parse(ST.configFile);
                toml["Extras"]["Lang"] = cultureInfo.Name;
                toml.SaveTo(ST.configFile);
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
                ST.OpenFile(STLog.logFile);
            }
        }

        private void ComboBox_LogLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isFirst)
                return;
            if (ComboBox_LogLevel.SelectedItem is ComboBoxItem item)
            {
                SaveLogLevel(item.ToolTip.ToString()!);
                STLog.Instance.WriteLine($"{I18n.LogLevelSwitch}: {item.ToolTip}");
            }
        }

        private void SaveLogLevel(string level)
        {
            try
            {
                STLog.Instance.LogLevel = STLog.Str2STLogLevel(level);
                TomlTable toml = TOML.Parse(ST.configFile);
                toml["Extras"]["LogLevel"] = level;
                toml.SaveTo(ST.configFile);
            }
            catch
            {
                MessageBox.Show(I18n.ConfigFileError);
            }
        }
    }
}