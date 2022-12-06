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
using StarsectorTools.Lib;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using I18n = StarsectorTools.Langs.Tools.GameSettings.GameSettings_I18n;

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
        string gameLogPath = @$"{ST.gamePath}\starsector-core\starsector.log";
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
                    MessageBox.Show(I18n.GameNotFound_SelectAgain, "", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show(I18n.ReplicationSuccess, "", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (File.Exists(gameLogPath))
                ST.OpenFile(gameLogPath);
            else
            {
                STLog.Instance.WriteLine($"{I18n.LogFilesNotExist} {I18n.Path}: {gameLogPath}", STLogLevel.WARN);
                MessageBox.Show($"{I18n.LogFilesNotExist}\n{I18n.Path}: {gameLogPath}");
            }
        }

        private void Button_ClearLogFile_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameLogPath))
            {
                ST.DeleteFileToRecycleBin(gameLogPath);
                File.Create(gameLogPath).Close();
                STLog.Instance.WriteLine(I18n.LogFileCleanCompleted);
                MessageBox.Show(I18n.LogFileCleanCompleted);
            }
            else
            {
                STLog.Instance.WriteLine($"{I18n.LogFilesNotExist} {I18n.Path}: {gameLogPath}", STLogLevel.WARN);
                MessageBox.Show($"{I18n.LogFilesNotExist}\n{I18n.Path}: {gameLogPath}");
            }
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
                            ST.DeleteDirToRecycleBin(dirParh);
                    }
                    else
                    {
                        ST.DeleteDirToRecycleBin($"{item.ToolTip}");
                        ComboBox_MissionsLoadouts.Items.Remove(item);
                    }
                }
                catch
                {
                    STLog.Instance.WriteLine($"{I18n.MissionsLoadoutsNotExist} {I18n.Path}: {item.ToolTip}");
                    MessageBox.Show($"{I18n.MissionsLoadoutsNotExist}\n{I18n.Path}: {item.ToolTip}");
                    return;
                }
                STLog.Instance.WriteLine(I18n.MissionsLoadoutsClearCompleted);
                MessageBox.Show(I18n.MissionsLoadoutsClearCompleted);
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
                ST.DeleteDirToRecycleBin(list.ElementAt(i).Key);
            STLog.Instance.WriteLine(I18n.SaveCleanCompleted);
            MessageBox.Show(I18n.SaveCleanCompleted);
        }

        private void Button_OpenMissionsLoadoutsDirectory_Click(object sender, RoutedEventArgs e)
        {
            string dirParh = $"{ST.gameSavePath}\\missions";
            if (Directory.Exists(dirParh))
                ST.OpenFile(dirParh);
            else
            {
                STLog.Instance.WriteLine($"{I18n.MissionsLoadoutsNotExist} {I18n.Path}: {dirParh}");
                MessageBox.Show($"{I18n.MissionsLoadoutsNotExist}\n{I18n.Path}: {dirParh}");
            }
        }

        private void Button_OpenSaveDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ST.gameSavePath))
                ST.OpenFile(ST.gameSavePath);
            else
            {
                STLog.Instance.WriteLine($"{I18n.SaveNotExist} {I18n.Path}: {ST.gameSavePath}");
                MessageBox.Show($"{I18n.SaveNotExist}\n{I18n.Path}: {ST.gameSavePath}");
            }
        }

        private void Button_OpenGamePath_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ST.gamePath))
                ST.OpenFile(ST.gamePath);
        }
    }
}
