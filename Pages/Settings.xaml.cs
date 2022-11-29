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
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            ST.totalMemory = Management.GetMemoryMetricsNow().Total;
            Label_GamePath.Content = ST.gamePath;
            GetVmparamsData();
            GetGameKey();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            do
            {
                ST.GetGamePath();
                if (!ST.TestGamePath())
                    MessageBox.Show("游戏本体路径出错\n请重新选择", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK);
            } while (!ST.TestGamePath());
            Label_GamePath.Content = ST.gamePath;
        }
        private void TextBox_SetMemory_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Keyboard.ClearFocus();
        }
        private void TextBox_SetMemory_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.Text = ST.MemorySizeParse(int.Parse(textBox.Text)).ToString();
        }
        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");

        private void Button_SetMemory_Click(object sender, RoutedEventArgs e)
        {
            SetVmparamsData();
        }

        private void Button_DuplicateKey_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(gameKey);
            MessageBox.Show("已成功将游戏序列码复制进剪切板。");
        }
    }
}
