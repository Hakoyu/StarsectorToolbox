using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HKW.Management;
using HKW.TomlParse;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Tools.GameSettings.GameSettings_I18n;

namespace StarsectorTools.Tools.GameSettings
{
    /// <summary>
    /// GameSettings.xaml 的交互逻辑
    /// </summary>
    public partial class GameSettings : Page
    {
        internal GameSettings()
        {
            InitializeComponent();
            Label_GamePath.Content = GameInfo.GameDirectory;
            Label_GameVersion.Content = GameInfo.Version;
            systemTotalMemory = Management.GetMemoryMetricsNow().Total;
            GetVmparamsData();
            GetGameKey();
            GetMissionsLoadouts();
            GetCustomResolution();
        }

        private void Button_SetGameDirectory_Click(object sender, RoutedEventArgs e)
        {
            while (!GameInfo.GetGameDirectory())
                Utils.ShowMessageBox(I18n.GameNotFound_SelectAgain, STMessageBoxIcon.Warning);
            var toml = TOML.Parse(ST.STConfigTomlFile);
            toml["Game"]["Path"] = GameInfo.GameDirectory;
            toml.SaveTo(ST.STConfigTomlFile);
            Label_GamePath.Content = GameInfo.GameDirectory;
        }

        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");

        private void Button_SetMemory_Click(object sender, RoutedEventArgs e)
        {
            if (!Regex.IsMatch(TextBox_Memory.Text, "^[0-9]+[mg]$"))
            {
                Utils.ShowMessageBox(I18n.FormatError, STMessageBoxIcon.Warning);
                TextBox_Memory.Text = vmparamsData.xmsx;
                return;
            }
            string unit = TextBox_Memory.Text.Last().ToString();
            int memory = int.Parse(Regex.Match(TextBox_Memory.Text, "[0-9]+").Value);
            int memoryMB = memory * (unit == "m" ? 1 : 1024);
            if (CheckMemorySize(memoryMB) is int sizeMB)
            {
                int size = unit == "m" ? sizeMB : sizeMB / 1024;
                TextBox_Memory.Text = $"{size}{unit}";
                return;
            }
            vmparamsData.xmsx = $"{memory}{unit}";
            File.WriteAllText($"{GameInfo.GameDirectory}\\vmparams", Regex.Replace(vmparamsData.data, @"(?<=-xm[sx])[0-9]+[mg]", vmparamsData.xmsx, RegexOptions.IgnoreCase));
            STLog.WriteLine($"{I18n.VmparamsMemorySet}: {vmparamsData.xmsx}");
            Utils.ShowMessageBox(I18n.VmparamsMemorySetSuccess);
        }

        private void Button_CopyKey_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(gameKey);
            Utils.ShowMessageBox(I18n.ReplicationSuccess, STMessageBoxIcon.Info);
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
            if (!Utils.FileExists(GameInfo.LogFile, false))
                File.Create(GameInfo.LogFile).Close();
            Utils.OpenLink(GameInfo.LogFile);
        }

        private void Button_ClearLogFile_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.FileExists(GameInfo.LogFile, false))
                Utils.DeleteFileToRecycleBin(GameInfo.LogFile);
            File.Create(GameInfo.LogFile).Close();
            STLog.WriteLine(I18n.GameLogCleanupCompleted);
        }

        private void Button_ClearMissionsLoadouts_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_MissionsLoadouts.SelectedItem is ComboBoxItem item)
            {
                string dirParh = $"{GameInfo.SaveDirectory}\\missions";
                try
                {
                    if (item.Content.ToString() == "All")
                    {
                        if (Utils.DirectoryExists(dirParh))
                            Utils.DeleteDirToRecycleBin(dirParh);
                    }
                    else
                    {
                        Utils.DeleteDirToRecycleBin($"{item.ToolTip}");
                        ComboBox_MissionsLoadouts.Items.Remove(item);
                    }
                    STLog.WriteLine(I18n.MissionsLoadoutsClearCompleted);
                    Utils.ShowMessageBox(I18n.MissionsLoadoutsClearCompleted);
                }
                catch (Exception ex)
                {
                    STLog.WriteLine($"{I18n.MissionsLoadoutsNotExist} {I18n.Path}: {item.ToolTip}", ex);
                    Utils.ShowMessageBox($"{I18n.MissionsLoadoutsNotExist}\n{I18n.Path}: {item.ToolTip}", STMessageBoxIcon.Error);
                }
            }
        }

        private void Button_ClearSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Utils.DirectoryExists(GameInfo.SaveDirectory))
                return;
            DirectoryInfo dirs = new(GameInfo.SaveDirectory);
            Dictionary<string, DateTime> dirsPath = new();
            foreach (var dir in dirs.GetDirectories())
                dirsPath.Add(dir.FullName, dir.LastWriteTime);
            dirsPath.Remove($"{GameInfo.SaveDirectory}\\missions");
            var list = dirsPath.OrderBy(kv => kv.Value);
            int count = TextBox_ReservedSaveSize.Text.Length > 0 ? list.Count() - int.Parse(TextBox_ReservedSaveSize.Text) : 0;
            for (int i = 0; i < count; i++)
                Utils.DeleteDirToRecycleBin(list.ElementAt(i).Key);
            STLog.WriteLine(I18n.SaveCleanCompleted);
            Utils.ShowMessageBox(I18n.SaveCleanCompleted);
        }

        private void Button_OpenMissionsLoadoutsDirectory_Click(object sender, RoutedEventArgs e)
        {
            string dirParh = $"{GameInfo.SaveDirectory}\\missions";
            if (Utils.DirectoryExists(dirParh))
                Utils.OpenLink(dirParh);
            else
            {
                STLog.WriteLine($"{I18n.MissionsLoadoutsNotExist} {I18n.Path}: {dirParh}", STLogLevel.WARN);
                Utils.ShowMessageBox($"{I18n.MissionsLoadoutsNotExist}\n{I18n.Path}: {dirParh}", STMessageBoxIcon.Warning);
            }
        }

        private void Button_OpenSaveDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.DirectoryExists(GameInfo.SaveDirectory))
                Utils.OpenLink(GameInfo.SaveDirectory);
            else
            {
                STLog.WriteLine($"{I18n.SaveNotExist} {I18n.Path}: {GameInfo.SaveDirectory}", STLogLevel.WARN);
                Utils.ShowMessageBox($"{I18n.SaveNotExist}\n{I18n.Path}: {GameInfo.SaveDirectory}", STMessageBoxIcon.Warning);
            }
        }

        private void Button_OpenGameDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.DirectoryExists(GameInfo.GameDirectory))
                Utils.OpenLink(GameInfo.GameDirectory);
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
                STLog.WriteLine(I18n.ResetSuccessful);
                Utils.ShowMessageBox(I18n.ResetSuccessful);
            }
            catch (Exception ex)
            {
                STLog.WriteLine(I18n.CustomResolutionResetError, ex);
                Utils.ShowMessageBox(I18n.CustomResolutionResetError, STMessageBoxIcon.Error);
            }
        }

        private void Button_CustomResolutionSetup_Click(object sender, RoutedEventArgs e)
        {
            if (TextBox_ResolutionWidth.Text.Length == 0 || TextBox_ResolutionHeight.Text.Length == 0)
            {
                Utils.ShowMessageBox(I18n.WidthAndHeightCannotBeEmpty, STMessageBoxIcon.Warning);
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
                STLog.WriteLine($"{I18n.SetupSuccessful} {TextBox_ResolutionWidth.Text}x{TextBox_ResolutionHeight.Text}");
                Utils.ShowMessageBox(I18n.SetupSuccessful);
            }
            catch (Exception ex)
            {
                STLog.WriteLine(I18n.CustomResolutionSetupError, ex);
                Utils.ShowMessageBox(I18n.CustomResolutionSetupError, STMessageBoxIcon.Error);
            }
        }
    }
}