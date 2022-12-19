using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HKW.Management;
using StarsectorTools.Libs;
using I18n = StarsectorTools.Langs.Tools.GameSettings.GameSettings_I18n;

namespace StarsectorTools.Tools.GameSettings
{
    /// <summary>
    /// GameSettings.xaml 的交互逻辑
    /// </summary>
    public partial class GameSettings : Page
    {
        private struct VmparamsData
        {
            public string data;
            public string xmsx;
        }

        private VmparamsData vmparamsData = new();
        private string gameKey = "";
        private string hideGameKey = "";
        private bool showKey = false;
        private string gameLogFile = @$"{ST.gameDirectory}\starsector-core\starsector.log";
        private string gameSettingsFile = $"{ST.gameDirectory}\\starsector-core\\data\\config\\settings.json";

        public GameSettings()
        {
            InitializeComponent();
            ST.totalMemory = Management.GetMemoryMetricsNow().Total;
            Label_GamePath.Content = ST.gameDirectory;
            Label_GameVersion.Content = ST.gameVersion;
            GetVmparamsData();
            GetGameKey();
            GetMissionsLoadouts();
            GetCustomResolution();
        }

        private void Button_SetGameDirectory_Click(object sender, RoutedEventArgs e)
        {
            do
            {
                ST.GetGameDirectory();
                if (!ST.CheckGameDirectory())
                    MessageBox.Show(I18n.GameNotFound_SelectAgain, "", MessageBoxButton.OK, MessageBoxImage.Warning);
            } while (!ST.CheckGameDirectory());
            Label_GamePath.Content = ST.gameDirectory;
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
            if (File.Exists(gameLogFile))
                ST.OpenFile(gameLogFile);
            else
            {
                STLog.Instance.WriteLine($"{I18n.LogFilesNotExist} {I18n.Path}: {gameLogFile}", STLogLevel.WARN);
                MessageBox.Show($"{I18n.LogFilesNotExist}\n{I18n.Path}: {gameLogFile}");
            }
        }

        private void Button_ClearLogFile_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameLogFile))
            {
                ST.DeleteFileToRecycleBin(gameLogFile);
                File.Create(gameLogFile).Close();
                STLog.Instance.WriteLine(I18n.LogFileCleanCompleted);
                MessageBox.Show(I18n.LogFileCleanCompleted);
            }
            else
            {
                STLog.Instance.WriteLine($"{I18n.LogFilesNotExist} {I18n.Path}: {gameLogFile}", STLogLevel.WARN);
                MessageBox.Show($"{I18n.LogFilesNotExist}\n{I18n.Path}: {gameLogFile}");
            }
        }

        private void Button_ClearMissionsLoadouts_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_MissionsLoadouts.SelectedItem is ComboBoxItem item)
            {
                string dirParh = $"{ST.gameSaveDirectory}\\missions";
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
            if (!Directory.Exists(ST.gameSaveDirectory))
                return;
            DirectoryInfo dirs = new(ST.gameSaveDirectory);
            Dictionary<string, DateTime> dirsPath = new();
            foreach (var dir in dirs.GetDirectories())
                dirsPath.Add(dir.FullName, dir.LastWriteTime);
            dirsPath.Remove($"{ST.gameSaveDirectory}\\missions");
            var list = dirsPath.OrderBy(kv => kv.Value);
            int count = TextBox_ReservedSaveSize.Text.Length > 0 ? list.Count() - int.Parse(TextBox_ReservedSaveSize.Text) : 0;
            for (int i = 0; i < count; i++)
                ST.DeleteDirToRecycleBin(list.ElementAt(i).Key);
            STLog.Instance.WriteLine(I18n.SaveCleanCompleted);
            MessageBox.Show(I18n.SaveCleanCompleted);
        }

        private void Button_OpenMissionsLoadoutsDirectory_Click(object sender, RoutedEventArgs e)
        {
            string dirParh = $"{ST.gameSaveDirectory}\\missions";
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
            if (Directory.Exists(ST.gameSaveDirectory))
                ST.OpenFile(ST.gameSaveDirectory);
            else
            {
                STLog.Instance.WriteLine($"{I18n.SaveNotExist} {I18n.Path}: {ST.gameSaveDirectory}");
                MessageBox.Show($"{I18n.SaveNotExist}\n{I18n.Path}: {ST.gameSaveDirectory}");
            }
        }

        private void Button_OpenGameDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ST.gameDirectory))
                ST.OpenFile(ST.gameDirectory);
        }

        private void Button_CustomResolutionReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string data = File.ReadAllText(gameSettingsFile);
                // 取消无边框
                data = Regex.Replace(data, @"(?<=undecoratedWindow"":)(?:false|true)", "false");
                // 注释吊自定义分辨率
                data = Regex.Replace(data, @"(?<=[ \t]+)""resolutionOverride""", @"#""resolutionOverride""");
                File.WriteAllText(gameSettingsFile, data);
                CheckBox_BorderlessWindow.IsChecked = false;
                Button_CustomResolutionReset.IsEnabled = false;
                TextBox_ResolutionWidth.Text = string.Empty;
                TextBox_ResolutionHeight.Text = string.Empty;
                MessageBox.Show(I18n.ResetSuccessful);
            }
            catch
            {
                STLog.Instance.WriteLine(I18n.CustomResolutionResetError, STLogLevel.ERROR);
                MessageBox.Show(I18n.CustomResolutionResetError, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_CustomResolutionSetup_Click(object sender, RoutedEventArgs e)
        {
            if (TextBox_ResolutionWidth.Text.Length == 0 || TextBox_ResolutionHeight.Text.Length == 0)
            {
                return;
            }
            try
            {
                string data = File.ReadAllText(gameSettingsFile);
                // 启用无边框
                if (CheckBox_BorderlessWindow.IsChecked is true)
                    data = Regex.Replace(data, @"(?<=undecoratedWindow"":)(?:false|true)", "true");
                // 设置自定义分辨率
                data = Regex.Replace(data, @"(?:#|)""resolutionOverride"":""[0-9]+x[0-9]+"",", @$"""resolutionOverride"":""{TextBox_ResolutionWidth.Text}x{TextBox_ResolutionHeight.Text}"",");
                File.WriteAllText(gameSettingsFile, data);
                Button_CustomResolutionReset.IsEnabled = true;
                MessageBox.Show(I18n.SetupSuccessful);
            }
            catch
            {
                STLog.Instance.WriteLine(I18n.CustomResolutionSetupError, STLogLevel.ERROR);
                MessageBox.Show(I18n.CustomResolutionSetupError, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}