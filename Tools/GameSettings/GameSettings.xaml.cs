using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Microsoft.Win32;
using HKW.Management;
using StarsectorTools.Langs.MessageBox;
using StarsectorTools.Lib;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;

namespace StarsectorTools.Tools.GameSettings
{
    /// <summary>
    /// GameSettings.xaml 的交互逻辑
    /// </summary>
    public partial class GameSettings : Page
    {
        struct VmparamsData
        {
            public string data;
            public string xmsx;
        }
        VmparamsData vmparamsData = new();
        string gameKey = "";
        string hideGameKey = "";
        bool showKey = false;
        public GameSettings()
        {
            InitializeComponent();
            ST.totalMemory = Management.GetMemoryMetricsNow().Total;
            Label_GamePath.Content = ST.gamePath;
            Label_GameVersion.Content = ST.gameVersion;
            GetVmparamsData();
            GetGameKey();
            GetMissionsLoadouts();
        }

        private void Button_SetGamePath_Click(object sender, RoutedEventArgs e)
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
        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        private void TextBox_SetMemory_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
                textBox.Text = ST.MemorySizeParse(int.Parse(textBox.Text)).ToString();
        }

        private void Button_SetMemory_Click(object sender, RoutedEventArgs e)
        {
            SetVmparamsData();
        }

        private void Button_CopyKey_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(gameKey);
            MessageBox.Show("已成功将游戏序列码复制进剪切板。");
        }

        private void Button_ShowKey_Click(object sender, RoutedEventArgs e)
        {
            if (showKey)
            {
                Label_GameKey.Content = hideGameKey;
                showKey = false;
            }
            else
            {
                Label_GameKey.Content = gameKey;
                showKey = true;
            }
        }

        private void Button_OpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            string path = @$"{ST.gamePath}\starsector-core\starsector.log";
            if (File.Exists(path))
                ST.OpenFile(path);
            else
                MessageBox.Show($"Log文件不存在\n位置:{path}");
        }

        private void Button_ClearLogFile_Click(object sender, RoutedEventArgs e)
        {
            string path = @$"{ST.gamePath}\starsector-core\starsector.log";
            if (File.Exists(path))
            {
                ST.DeleteFileToRecycleBin(path);
                File.Create(path).Close();
            }
            else
                MessageBox.Show($"Log文件不存在\n位置:{path}");
        }

        private void Button_ClearMissionsLoadouts_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_MissionsLoadouts.SelectedItem is ComboBoxItem item)
            {
                string dirParh = $"{ST.gameSavePath}\\missions";
                try
                {
                    if (item.Content.ToString() == "All")
                    {
                        if (Directory.Exists(dirParh))
                            ST.DeleteDirectoryToRecycleBin(dirParh);
                    }
                    else
                    {
                        ST.DeleteDirectoryToRecycleBin($"{item.ToolTip}");
                        ComboBox_MissionsLoadouts.Items.Remove(item);
                    }
                }
                catch
                {
                    STLog.Instance.WriteLine($"文件夹不存在 位置: {item.ToolTip}");
                    MessageBox.Show($"战役配装文件不存在\n位置: {item.ToolTip}");
                }
            }
        }

        private void Button_ClearSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(ST.gameSavePath))
                return;
            DirectoryInfo dirs = new(ST.gameSavePath);
            Dictionary<string, DateTime> dirsPath = new();
            foreach (var dir in dirs.GetDirectories())
                dirsPath.Add(dir.FullName, dir.LastWriteTime);
            dirsPath.Remove($"{ST.gameSavePath}\\missions");
            var list = dirsPath.OrderBy(kv => kv.Value);
            int count = list.Count() - int.Parse(TextBox_ReservedSaveSize.Text);
            for (int i = 0; i < count; i++)
                ST.DeleteDirectoryToRecycleBin(list.ElementAt(i).Key);
        }

        private void Button_OpenMissionsLoadoutsDirectory_Click(object sender, RoutedEventArgs e)
        {
            string dirParh = $"{ST.gameSavePath}\\missions";
            if (Directory.Exists(dirParh))
                ST.OpenFile(dirParh);
            else
            {
                STLog.Instance.WriteLine($"战役配装文件夹不存在 位置: {dirParh}");
                MessageBox.Show($"战役配装文件不存在\n位置: {dirParh}");
            }
        }

        private void Button_OpenSaveDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ST.gameSavePath))
                ST.OpenFile(ST.gameSavePath);
            else
            {
                STLog.Instance.WriteLine($"战役配装文件夹不存在 位置: {ST.gameSavePath}");
                MessageBox.Show($"战役配装文件不存在\n位置: {ST.gameSavePath}");
            }
        }
    }
}
