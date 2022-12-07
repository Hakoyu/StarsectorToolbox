using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HKW.TomlParse;
using StarsectorTools.Lib;
using StarsectorTools.Windows;
using I18n = StarsectorTools.Langs.Pages.Settings_I18n;

namespace StarsectorTools.Pages
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Page
    {
        bool isFirst = true;
        public Settings()
        {
            InitializeComponent();
            ShowCurrentLanguage();
        }
        void ShowCurrentLanguage()
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

        private void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isFirst)
            {
                isFirst = false;
                return;
            }
            if (e.AddedItems[0] is ComboBoxItem item)
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(item.ToolTip.ToString()!);
                SaveLanguage();
                ChangeLanguage();
                ((MainWindow)Application.Current.MainWindow).ChangeLanguage();
            }
        }
        void ChangeLanguage()
        {
            GroupBox_Settings.Header = I18n.Settings;
            Button_OpenLogFile.Content = I18n.OpenLogFile;
        }
        void SaveLanguage()
        {
            try
            {
                using TomlTable toml = TOML.Parse(ST.configPath);
                toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                toml.SaveTo(ST.configPath);
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
                ST.OpenFile(ST.logPath);
            }
        }
    }
}
