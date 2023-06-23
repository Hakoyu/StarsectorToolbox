using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HKW.HKWViewModels;
using HKW.HKWViewModels.Controls;
using HKW.HKWViewModels.Dialogs;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.Models.Messages;
using StarsectorToolbox.Models.ST;
using I18nRes = StarsectorToolbox.Langs.Pages.GameSettings.GameSettingsPageI18nRes;

namespace StarsectorToolbox.ViewModels.GameSettings;

internal partial class GameSettingsPageViewModel : ObservableObject
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private ObservableI18n<I18nRes> _i18n = ObservableI18n<I18nRes>.Create(new());

    [ObservableProperty]
    private string _gamePath = GameInfo.BaseDirectory;

    [ObservableProperty]
    private string _gameVersion = GameInfo.Version;

    [ObservableProperty]
    private string _gameMemory = string.Empty;

    [ObservableProperty]
    private string _retainRecentSaveCount = "5";

    [ObservableProperty]
    private string _resolutionWidth = string.Empty;

    [ObservableProperty]
    private string _resolutionHeight = string.Empty;

    [ObservableProperty]
    private bool _borderlessWindow = false;

    [ObservableProperty]
    private bool _customResolutionCanReset = false;

    [ObservableProperty]
    private string _gameKey = string.Empty;

    private string _realGameKey = string.Empty;
    private string _hideGameKey = string.Empty;

    private static readonly string sr_missionsLoadoutsDirectory =
        $"{GameInfo.SaveDirectory}\\missions";

    [ObservableProperty]
    private ComboBoxVM _comboBox_MissionsLoadouts =
        new()
        {
            new() { Content = I18nRes.All, ToolTip = nameof(I18nRes.All) }
        };

    public GameSettingsPageViewModel() { }

    public GameSettingsPageViewModel(bool noop)
    {
        GetGameKey();
        GetVmparamsData();
        GetMissionsLoadouts();
        GetCustomResolution();
        I18n.AddCultureChangedAction(CultureChangedAction);
        ComboBox_MissionsLoadouts.SelectedIndex = 0;
    }

    private void CultureChangedAction(CultureInfo cultureInfo)
    {
        ComboBox_MissionsLoadouts[0].Content = I18nRes.All;
    }

    [RelayCommand]
    private static void SetGameDirectory()
    {
        if (GameInfo.GetGameDirectory() is false)
        {
            MessageBoxVM.Show(
                new(I18nRes.GameNotFound_SelectAgain) { Icon = MessageBoxVM.Icon.Warning }
            );
            return;
        }
        STSettings.Instance.Game.Path = GameInfo.BaseDirectory;
        STSettings.Save();
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
        GameKey = GameKey == _hideGameKey ? _realGameKey : _hideGameKey;
    }

    private bool ShowGameKeyCanExecute() => !string.IsNullOrWhiteSpace(GameKey);

    [RelayCommand]
    private void SetMemory()
    {
        if (Regex.IsMatch(GameMemory, "^[0-9]+[mg]$") is false)
        {
            GameMemory = _vmparamsData.xmsx;
            MessageBoxVM.Show(new(I18nRes.FormatError) { Icon = MessageBoxVM.Icon.Warning });
            return;
        }
        string unit = GameMemory.Last().ToString();
        int memory = int.Parse(Regex.Match(GameMemory, "[0-9]+").Value);
        int memoryMB = memory * (unit == "m" ? 1 : 1024);
        if (CheckMemorySize(memoryMB) is uint sizeMB)
        {
            var size = unit == "m" ? sizeMB : sizeMB / 1024;
            GameMemory = $"{size}{unit}";
            return;
        }
        _vmparamsData.xmsx = $"{memory}{unit}";
        File.WriteAllText(
            $"{GameInfo.BaseDirectory}\\vmparams",
            Regex.Replace(
                _vmparamsData.data,
                @"(?<=-xm[sx])[0-9]+[mg]",
                _vmparamsData.xmsx,
                RegexOptions.IgnoreCase
            )
        );
        sr_logger.Info($"{I18nRes.VmparamsMemorySet}: {_vmparamsData.xmsx}");
        MessageBoxVM.Show(new(I18nRes.VmparamsMemorySetComplete));
    }

    [RelayCommand]
    private static void OpenGameLogFile()
    {
        if (File.Exists(GameInfo.LogFile) is false)
            File.Create(GameInfo.LogFile).Close();
        Utils.OpenLink(GameInfo.LogFile);
    }

    [RelayCommand]
    private static void ClearGameLogFile()
    {
        Utils.ClearGameLog();
    }

    [RelayCommand]
    private void ClearMissionsLoadouts()
    {
        var selected = ComboBox_MissionsLoadouts.SelectedItem?.ToolTip?.ToString();
        if (selected is null)
            return;
        if (selected is nameof(I18nRes.All))
        {
            if (Utils.DirectoryExists(sr_missionsLoadoutsDirectory))
            {
                Utils.DeleteDirectoryToRecycleBin(sr_missionsLoadoutsDirectory);
                Directory.CreateDirectory(sr_missionsLoadoutsDirectory);
            }
        }
        else
        {
            if (Utils.DirectoryExists(sr_missionsLoadoutsDirectory) is false)
            {
                MessageBoxVM.Show(
                    new(
                        $"{I18nRes.MissionsLoadoutsNotExist}\n{I18nRes.Path}: {sr_missionsLoadoutsDirectory}"
                    )
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
                return;
            }
            if (Utils.DeleteDirectoryToRecycleBin(selected) is false)
            {
                sr_logger.Warn($"{I18nRes.MissionsLoadoutsNotExist} {I18nRes.Path}: {selected}");
                MessageBoxVM.Show(
                    new($"{I18nRes.MissionsLoadoutsNotExist}\n{I18nRes.Path}: {selected}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
            }
            ComboBox_MissionsLoadouts.Remove(ComboBox_MissionsLoadouts.SelectedItem!);
        }
        sr_logger.Info(I18nRes.MissionsLoadoutsClearCompleted);
        MessageBoxVM.Show(new(I18nRes.MissionsLoadoutsClearCompleted));
    }

    [RelayCommand]
    private static void OpenMissionsLoadoutsDirectory()
    {
        if (Utils.DirectoryExists(sr_missionsLoadoutsDirectory))
            Utils.OpenLink(sr_missionsLoadoutsDirectory);
        else
        {
            MessageBoxVM.Show(
                new(
                    $"{I18nRes.MissionsLoadoutsNotExist}\n{I18nRes.Path}: {sr_missionsLoadoutsDirectory}"
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
        if (Utils.DirectoryExists(GameInfo.SaveDirectory) is false)
            return;
        Dictionary<string, DateTime> dirsPath = new();
        foreach (var dir in new DirectoryInfo(GameInfo.SaveDirectory).GetDirectories())
            dirsPath.Add(dir.FullName, dir.LastWriteTime);
        dirsPath.Remove(Path.Combine(GameInfo.SaveDirectory, "missions"));
        var list = dirsPath.OrderBy(kv => kv.Value);
        int count = string.IsNullOrEmpty(RetainRecentSaveCount)
            ? 0
            : dirsPath.Count - int.Parse(RetainRecentSaveCount);
        for (int i = 0; i < count; i++)
            Utils.DeleteDirectoryToRecycleBin(list.ElementAt(i).Key);
        sr_logger.Info(I18nRes.SaveCleanComplete);
        MessageBoxVM.Show(new(I18nRes.SaveCleanComplete));
    }

    [RelayCommand]
    private static void OpenSaveDirectory()
    {
        if (Utils.DirectoryExists(GameInfo.SaveDirectory))
            Utils.OpenLink(GameInfo.SaveDirectory);
        else
        {
            sr_logger.Warn($"{I18nRes.SaveNotExist} {I18nRes.Path}: {GameInfo.SaveDirectory}");
            MessageBoxVM.Show(
                new($"{I18nRes.SaveNotExist}\n{I18nRes.Path}: {GameInfo.SaveDirectory}")
                {
                    Icon = MessageBoxVM.Icon.Warning
                }
            );
        }
    }

    [RelayCommand]
    private void SetCustomResolution()
    {
        if (string.IsNullOrEmpty(ResolutionWidth) || string.IsNullOrEmpty(ResolutionHeight))
        {
            MessageBoxVM.Show(
                new(I18nRes.WidthAndHeightCannotBeEmpty) { Icon = MessageBoxVM.Icon.Warning }
            );
            return;
        }
        try
        {
            string data = File.ReadAllText(GameInfo.SettingsFile);
            // 设置无边框
            data = Regex.Replace(
                data,
                @"(?<=undecoratedWindow"":)(?:false|true)",
                BorderlessWindow.ToString().ToLower()
            );
            // 设置自定义分辨率
            data = Regex.Replace(
                data,
                @"(?:#|)""resolutionOverride"":""[0-9]+x[0-9]+"",",
                @$"""resolutionOverride"":""{ResolutionWidth}x{ResolutionHeight}"","
            );
            File.WriteAllText(GameInfo.SettingsFile, data);
            CustomResolutionCanReset = true;
            sr_logger.Info(
                $"{I18nRes.CustomResolutionSetComplete} {ResolutionWidth}x{ResolutionHeight}"
            );
            MessageBoxVM.Show(
                new($"{I18nRes.CustomResolutionSetComplete}\n{I18nRes.CustomResolutionHelp}")
            );
        }
        catch (Exception ex)
        {
            sr_logger.Error(ex, I18nRes.CustomResolutionSetupError);
            MessageBoxVM.Show(
                new(I18nRes.CustomResolutionSetupError) { Icon = MessageBoxVM.Icon.Error }
            );
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
            data = Regex.Replace(
                data,
                @"(?<=[ \t]+)""resolutionOverride""",
                @"#""resolutionOverride"""
            );
            File.WriteAllText(GameInfo.SettingsFile, data);
            BorderlessWindow = false;
            CustomResolutionCanReset = false;
            ResolutionWidth = string.Empty;
            ResolutionHeight = string.Empty;
            sr_logger.Info(I18nRes.ResetSuccessful);
            MessageBoxVM.Show(new(I18nRes.ResetSuccessful));
        }
        catch (Exception ex)
        {
            sr_logger.Error(ex, I18nRes.CustomResolutionResetError);
            MessageBoxVM.Show(
                new(I18nRes.CustomResolutionResetError) { Icon = MessageBoxVM.Icon.Error }
            );
        }
    }

    [RelayCommand]
    private void ShowCrashReporter()
    {
        WeakReferenceMessenger.Default.Send<ShowCrashReporterMessage>(new(64));
    }
}
