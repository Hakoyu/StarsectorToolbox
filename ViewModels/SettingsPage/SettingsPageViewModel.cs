using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HKW.Libs.Log4Cs;
using HKW.Libs.TomlParse;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialogs;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Models.Messages;
using I18nRes = StarsectorTools.Langs.Pages.Settings.SettingsPageI18nRes;

namespace StarsectorTools.ViewModels.SettingsPage
{
    internal partial class SettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableI18n<I18nRes> _i18n = ObservableI18n<I18nRes>.Create(new());

        [ObservableProperty]
        private string? _extensionDebugPath;

        [ObservableProperty]
        private ComboBoxVM _comboBox_LogLevel =
            new()
            {
                new() { Content = I18nRes.LogLevel_DEBUG, ToolTip = LogLevel.DEBUG },
                new() { Content = I18nRes.LogLevel_INFO, ToolTip = LogLevel.INFO },
                new() { Content = I18nRes.LogLevel_WARN, ToolTip = LogLevel.WARN },
            };

        [ObservableProperty]
        private ComboBoxVM _comboBox_Language =
            new()
            {
                new() { Content = "English", ToolTip = "en-US" },
                new() { Content = "简体中文", ToolTip = "zh-CN" },
            };

        public SettingsPageViewModel()
        {
            // https://github.com/CommunityToolkit/dotnet/issues/604
            // ExtensionDebugPath = WeakReferenceMessenger.Default.Send<ExtensionDebugPathRequestMessage>();
        }

        public SettingsPageViewModel(bool noop)
        {
            I18n.AddPropertyChangedAction(I18nChangedAction);
            ExtensionDebugPath =
                WeakReferenceMessenger.Default.Send<ExtensionDebugPathRequestMessage>().Response;
            // 设置LogLevel初始值
            ComboBox_LogLevel.SelectedItem = ComboBox_LogLevel.First(
                i => i.ToolTip is LogLevel level && level == Logger.Options.DefaultLevel
            );
            // 设置Language初始值
            ComboBox_Language.SelectedItem = ComboBox_Language.FirstOrDefault(
                i => i.ToolTip is string language && language == ObservableI18n.Language,
                ComboBox_Language[0]
            );
            // 注册事件
            ComboBox_LogLevel.SelectionChangedEvent += ComboBox_LogLevel_SelectionChangedEvent;
            ComboBox_Language.SelectionChangedEvent += ComboBox_Language_SelectionChangedEvent;
            WeakReferenceMessenger.Default.Register<ExtensionDebugPathErrorMessage>(this, ExtensionDebugPathErrorReceive);
        }

        private void ExtensionDebugPathErrorReceive(object recipient, ExtensionDebugPathErrorMessage message)
        {
            ExtensionDebugPath = string.Empty;
        }

        private void I18nChangedAction()
        {
            ComboBox_LogLevel[0].Content = I18nRes.LogLevel_DEBUG;
            ComboBox_LogLevel[1].Content = I18nRes.LogLevel_INFO;
            ComboBox_LogLevel[2].Content = I18nRes.LogLevel_WARN;
        }

        private void ComboBox_Language_SelectionChangedEvent(object parameter)
        {
            if (parameter is not ComboBoxItemVM item)
                return;
            var language = item.ToolTip!.ToString()!;
            if (ObservableI18n.Language == language)
                return;
            ObservableI18n.Language = item.ToolTip!.ToString()!;
            var toml = TOML.Parse(ST.ConfigTomlFile);
            toml["Lang"] = ObservableI18n.Language;
            toml.SaveTo(ST.ConfigTomlFile);
            Logger.Info($"{I18nRes.LanguageSwitch}: {ObservableI18n.Language}");
        }

        private void ComboBox_LogLevel_SelectionChangedEvent(object parameter)
        {
            if (parameter is not ComboBoxItemVM item)
                return;
            var level = item.ToolTip!.ToString()!;
            if (Logger.Options.DefaultLevel.ToString() == level)
                return;
            var toml = TOML.Parse(ST.ConfigTomlFile);
            toml["LogLevel"] = level;
            toml.SaveTo(ST.ConfigTomlFile);
            Logger.Options.DefaultLevel = Logger.LogLevelConverter(level);
            Logger.Info($"{I18nRes.LogLevelSwitch}: {Logger.Options.DefaultLevel}");
        }

        [RelayCommand]
        private void SetExtensionDebugPath()
        {
            var filesName = OpenFileDialogVM.Show(
                new()
                {
                    Title = I18nRes.SelectDebugFile,
                    Filter = $"Toml {I18nRes.File}|Extension.toml"
                }
            );
            if (filesName is null || filesName.Any() is false)
                return;
            string path = Path.GetDirectoryName(filesName.First())!;
            ExtensionDebugPath = path;
            if (
                MessageBoxVM.Show(
                    new(I18nRes.EffectiveAfterReload)
                    {
                        Button = MessageBoxVM.Button.YesNo,
                        Icon = MessageBoxVM.Icon.Question
                    }
                ) is MessageBoxVM.Result.Yes
            )
            {
                WeakReferenceMessenger.Default.Send<ExtensionDebugPathChangedMessage>(
                    new(ExtensionDebugPath)
                );
                Logger.Info($"{I18nRes.SetExtensionDebugPath}: {ExtensionDebugPath}");
            }
        }

        [RelayCommand(CanExecute = nameof(ClearButtonCanExecute))]
        private void ClearExtensionDebugPath()
        {
            ExtensionDebugPath = string.Empty;
            if (
                MessageBoxVM.Show(
                    new(I18nRes.EffectiveAfterReload)
                    {
                        Button = MessageBoxVM.Button.YesNo,
                        Icon = MessageBoxVM.Icon.Question
                    }
                ) is MessageBoxVM.Result.Yes
            )
            {
                WeakReferenceMessenger.Default.Send<ExtensionDebugPathChangedMessage>(
                    new(ExtensionDebugPath)
                );
                Logger.Info($"{I18nRes.ClearExtensionDebugPath}: {ExtensionDebugPath}");
            }
        }

        private bool ClearButtonCanExecute() => string.IsNullOrWhiteSpace(ExtensionDebugPath);

        [RelayCommand]
        private void OpenLogFile()
        {
            Utils.OpenLink(Logger.LogFile);
        }
    }
}