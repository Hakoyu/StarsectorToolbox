using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using HKW.Management;
using StarsectorTools.Langs.MessageBox;
using StarsectorTools.Lib;

namespace StarsectorTools.Pages
{
    /// <summary>
    /// Page_Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Page_Settings : Page
    {
        public Page_Settings()
        {
            InitializeComponent();
            Global.totalMemory = Management.GetMemoryMetricsNow().Total;
            Label_GamePath.Content = Global.gamePath;
            GetVmparamsData();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            do
            {
                Global.GetGamePath();
                if (!Global.TestGamePath())
                    MessageBox.Show("游戏本体路径出错\n请重新选择", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK);
            } while (!Global.TestGamePath());
            Label_GamePath.Content = Global.gamePath;
        }
        private void TextBox_SetMemory_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Keyboard.ClearFocus();
        }
        private void TextBox_SetMemory_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.Text = Global.MemorySizeParse(int.Parse(textBox.Text)).ToString();
        }
        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");

        private void Button_SetMemory_Click(object sender, RoutedEventArgs e)
        {
            SetVmparamsData();
        }

    }
}
