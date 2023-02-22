using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Libs;
using HKW.Libs.Log4Cs;
using HKW.Libs.TomlParse;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialogs;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18nRes = StarsectorTools.Langs.Pages.GameSettings.GameSettingsPageI18nRes;

namespace StarsectorTools.ViewModels.GameSettingsPage
{
    internal partial class GameSettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string gamePath = GameInfo.BaseDirectory;

        [ObservableProperty]
        private string gameVersion = GameInfo.Version;

        [ObservableProperty]
        private string gameMemory = string.Empty;

        [ObservableProperty]
        private string retainRecentSaveCount = "5";

        [ObservableProperty]
        private string resolutionWidth = string.Empty;

        [ObservableProperty]
        private string resolutionHeight = string.Empty;

        [ObservableProperty]
        private bool borderlessWindow = false;

        [ObservableProperty]
        private bool customResolutionCanReset = false;

        [ObservableProperty]
        private string gameKey = string.Empty;

        private string realGameKey = string.Empty;
        private string hideGameKey = string.Empty;

        private string missionsLoadoutsDirectory = $"{GameInfo.SaveDirectory}\\missions";

        private int systemTotalMemory = ManagementMemoryMetrics.GetMemoryMetricsNow().Total;

        [ObservableProperty]
        private ComboBoxVM comboBox_MissionsLoadouts =
            new()
            {
                new() { Content = I18nRes.All, ToolTip = nameof(I18nRes.All) }
            };

        [ObservableProperty]
        private ObservableI18n<I18nRes> i18n = ObservableI18n<I18nRes>.Create(new());

        public GameSettingsPageViewModel()
        { }

        public GameSettingsPageViewModel(bool noop)
        {
            GetGameKey();
            GetVmparamsData();
            GetMissionsLoadouts();
            GetCustomResolution();
            I18n.AddPropertyChangedAction(I18nChangedAction);
            ComboBox_MissionsLoadouts.SelectedIndex = 0;
        }

        private void I18nChangedAction()
        {
            ComboBox_MissionsLoadouts[0].Content = I18nRes.All;
        }

        [RelayCommand]
        private void SetGameDirectory()
        {
            while (!GameInfo.GetGameDirectory())
                MessageBoxVM.Show(
                    new(I18nRes.GameNotFound_SelectAgain) { Icon = MessageBoxVM.Icon.Warning }
                );
            var toml = TOML.Parse(ST.ConfigTomlFile);
            toml["Game"]["Path"] = GameInfo.BaseDirectory;
            toml.SaveTo(ST.ConfigTomlFile);
            GamePath = GameInfo.BaseDirectory;
        }

        [RelayCommand]
        private void OpenGameDirectory()
        {
            Utils.OpenLink(GamePath);
        }

        [RelayCommand(CanExecute = nameof(CopyGameKeyCanExecute))]
        private void CopyGameKey()
        {
            ClipboardVM.SetText(GameKey);
            MessageBoxVM.Show(new(I18nRes.ReplicationSuccess));
        }

        private bool CopyGameKeyCanExecute() => !string.IsNullOrWhiteSpace(GameKey);

        [RelayCommand(CanExecute = nameof(ShowGameKeyCanExecute))]
        private void ShowGameKey()
        {
            GameKey = GameKey == hideGameKey ? realGameKey : hideGameKey;
        }

        private bool ShowGameKeyCanExecute() => !string.IsNullOrWhiteSpace(GameKey);

        [RelayCommand]
        private void SetMemory()
        {
            if (!Regex.IsMatch(GameMemory, "^[0-9]+[mg]$"))
            {
                MessageBoxVM.Show(new(I18nRes.FormatError) { Icon = MessageBoxVM.Icon.Warning });
                GameMemory = vmparamsData.xmsx;
                return;
            }
            string unit = GameMemory.Last().ToString();
            int memory = int.Parse(Regex.Match(GameMemory, "[0-9]+").Value);
            int memoryMB = memory * (unit == "m" ? 1 : 1024);
            if (CheckMemorySize(memoryMB) is int sizeMB)
            {
                int size = unit == "m" ? sizeMB : sizeMB / 1024;
                GameMemory = $"{size}{unit}";
                return;
            }
            vmparamsData.xmsx = $"{memory}{unit}";
            File.WriteAllText($"{GameInfo.BaseDirectory}\\vmparams", Regex.Replace(vmparamsData.data, @"(?<=-xm[sx])[0-9]+[mg]", vmparamsData.xmsx, RegexOptions.IgnoreCase));
            Logger.Record($"{I18nRes.VmparamsMemorySet}: {vmparamsData.xmsx}");
            MessageBoxVM.Show(new(I18nRes.VmparamsMemorySetComplete));
        }

        [RelayCommand]
        private void OpenGameLogFile()
        {
            if (!Utils.FileExists(GameInfo.LogFile, false))
                File.Create(GameInfo.LogFile).Close();
            Utils.OpenLink(GameInfo.LogFile);
        }

        [RelayCommand]
        private void ClearGameLogFile()
        {
            if (Utils.FileExists(GameInfo.LogFile, false))
                Utils.DeleteFileToRecycleBin(GameInfo.LogFile);
            File.Create(GameInfo.LogFile).Close();
            Logger.Record(I18nRes.GameLogCleanupCompleted);
        }

        [RelayCommand]
        private void ClearMissionsLoadouts()
        {
            var selected = ComboBox_MissionsLoadouts.SelectedItem?.ToolTip?.ToString();
            if (selected is null)
                return;
            if (selected is nameof(I18nRes.All))
            {
                if (Utils.DirectoryExists(missionsLoadoutsDirectory))
                {
                    Utils.DeleteDirToRecycleBin(missionsLoadoutsDirectory);
                    Directory.CreateDirectory(missionsLoadoutsDirectory);
                }
            }
            else
            {
                if (!Utils.DirectoryExists(missionsLoadoutsDirectory))
                {
                    MessageBoxVM.Show(
                        new(
                            $"{I18nRes.MissionsLoadoutsNotExist}\n{I18nRes.Path}: {missionsLoadoutsDirectory}"
                        )
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                    return;
                }
                if (!Utils.DeleteDirToRecycleBin(selected))
                {
                    Logger.Record(
                        $"{I18nRes.MissionsLoadoutsNotExist} {I18nRes.Path}: {selected}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.MissionsLoadoutsNotExist}\n{I18nRes.Path}: {selected}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                }
                ComboBox_MissionsLoadouts.Remove(ComboBox_MissionsLoadouts.SelectedItem!);
            }
            Logger.Record(I18nRes.MissionsLoadoutsClearCompleted);
            MessageBoxVM.Show(new(I18nRes.MissionsLoadoutsClearCompleted));
        }

        [RelayCommand]
        private void OpenMissionsLoadoutsDirectory()
        {
            if (Utils.DirectoryExists(missionsLoadoutsDirectory))
                Utils.OpenLink(missionsLoadoutsDirectory);
            else
            {
                MessageBoxVM.Show(
                    new(
                        $"{I18nRes.MissionsLoadoutsNotExist}\n{I18nRes.Path}: {missionsLoadoutsDirectory}"
                    )
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
            }
        }

        [RelayCommand]
        private void ClearSave()
        {
            if (!Utils.DirectoryExists(GameInfo.SaveDirectory))
                return;
            Dictionary<string, DateTime> dirsPath = new();
            foreach (var dir in new DirectoryInfo(GameInfo.SaveDirectory).GetDirectories())
                dirsPath.Add(dir.FullName, dir.LastWriteTime);
            dirsPath.Remove(Path.Combine(GameInfo.SaveDirectory, "missions"));
            var list = dirsPath.OrderBy(kv => kv.Value);
            int count = string.IsNullOrEmpty(RetainRecentSaveCount) ? 0 : dirsPath.Count - int.Parse(RetainRecentSaveCount);
            for (int i = 0; i < count; i++)
                Utils.DeleteDirToRecycleBin(list.ElementAt(i).Key);
            Logger.Record(I18nRes.SaveCleanComplete);
            MessageBoxVM.Show(new(I18nRes.SaveCleanComplete));
        }

        [RelayCommand]
        private void OpenSaveDirectory()
        {
            if (Utils.DirectoryExists(GameInfo.SaveDirectory))
                Utils.OpenLink(GameInfo.SaveDirectory);
            else
            {
                Logger.Record($"{I18nRes.SaveNotExist} {I18nRes.Path}: {GameInfo.SaveDirectory}", LogLevel.WARN);
                MessageBoxVM.Show(new($"{I18nRes.SaveNotExist}\n{I18nRes.Path}: {GameInfo.SaveDirectory}") { Icon = MessageBoxVM.Icon.Warning });
            }
        }

        [RelayCommand]
        private void SetCustomResolution()
        {
            if (string.IsNullOrEmpty(ResolutionWidth) || string.IsNullOrEmpty(ResolutionHeight))
            {
                MessageBoxVM.Show(new(I18nRes.WidthAndHeightCannotBeEmpty) { Icon = MessageBoxVM.Icon.Warning });
                return;
            }
            try
            {
                string data = File.ReadAllText(GameInfo.SettingsFile);
                // 启用无边框
                if (BorderlessWindow)
                    data = Regex.Replace(data, @"(?<=undecoratedWindow"":)(?:false|true)", "true");
                // 设置自定义分辨率
                data = Regex.Replace(data, @"(?:#|)""resolutionOverride"":""[0-9]+x[0-9]+"",", @$"""resolutionOverride"":""{ResolutionWidth}x{ResolutionHeight}"",");
                File.WriteAllText(GameInfo.SettingsFile, data);
                CustomResolutionCanReset = true;
                Logger.Record($"{I18nRes.CustomResolutionSetComplete} {ResolutionWidth}x{ResolutionHeight}");
                MessageBoxVM.Show(new($"{I18nRes.CustomResolutionSetComplete}\n{I18nRes.CustomResolutionHelp}"));
            }
            catch (Exception ex)
            {
                Logger.Record(I18nRes.CustomResolutionSetupError, ex);
                MessageBoxVM.Show(new(I18nRes.CustomResolutionSetupError) { Icon = MessageBoxVM.Icon.Error });
            }
        }

        [RelayCommand]
        private void ResetCustomResolution()
        {
            try
            {
                string data = File.ReadAllText(GameInfo.SettingsFile);
                // 取消无边框
                data = Regex.Replace(data, @"(?<=undecoratedWindow"":)(?:false|true)", "false");
                // 注释吊自定义分辨率
                data = Regex.Replace(data, @"(?<=[ \t]+)""resolutionOverride""", @"#""resolutionOverride""");
                File.WriteAllText(GameInfo.SettingsFile, data);
                BorderlessWindow = false;
                CustomResolutionCanReset = false;
                ResolutionWidth = string.Empty;
                ResolutionHeight = string.Empty;
                Logger.Record(I18nRes.ResetSuccessful);
                MessageBoxVM.Show(new(I18nRes.ResetSuccessful));
            }
            catch (Exception ex)
            {
                Logger.Record(I18nRes.CustomResolutionResetError, ex);
                MessageBoxVM.Show(new(I18nRes.CustomResolutionResetError) { Icon = MessageBoxVM.Icon.Error });
            }
        }
    }
}