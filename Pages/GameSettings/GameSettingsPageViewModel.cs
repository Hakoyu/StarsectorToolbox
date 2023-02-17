using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Libs;
using HKW.Libs.Log4Cs;
using HKW.Libs.TomlParse;
using HKW.ViewModels;
using HKW.ViewModels.Dialog;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18nRes = StarsectorTools.Langs.Pages.GameSettings.GameSettingsPageI18nRes;

namespace StarsectorTools.Pages.GameSettings
{
    internal partial class GameSettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string gamePath = GameInfo.BaseDirectory;

        [ObservableProperty]
        private string gameVersion = GameInfo.Version;

        [ObservableProperty]
        private string gameKey = string.Empty;
        private string realGameKey = string.Empty;
        private string hideGameKey = string.Empty;

        [ObservableProperty]
        private string memory = string.Empty;

        private int systemTotalMemory = ManagementMemoryMetrics.GetMemoryMetricsNow().Total;

        [ObservableProperty]
        private ObservableI18n<I18nRes> i18n = ObservableI18n<I18nRes>.Create(new());

        public GameSettingsPageViewModel() { }

        public GameSettingsPageViewModel(bool noop)
        {
            GetGameKey();
            GetVmparamsData();
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
    }
}
