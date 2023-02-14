using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using HKW.Libs.Log4Cs;
using HKW.Libs.TomlParse;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialog;
using I18nRes = StarsectorTools.Langs.Pages.Settings.SettingsPageI18nRes;
using StarsectorTools.Libs.Messages;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Windows.MainWindow;
using HKW.ViewModels;

namespace StarsectorTools.Pages.Settings
{
    internal partial class SettingsPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableI18n<I18nRes> i18n = ObservableI18n<I18nRes>.Create(new());
        //[ObservableProperty]
        //private string language;
        //partial void OnLanguageChanged(string value)
        //{
        //    var cultureInfo = new CultureInfo(value);
        //    Thread.CurrentThread.CurrentUICulture = cultureInfo;
        //    Thread.CurrentThread.CurrentCulture = cultureInfo;
        //    SettingsI18n.Current.Language = value;
        //}

        [ObservableProperty]
        private string? expansionDebugPath;

        public ComboBoxVM LogLevelComboBox { get; set; } = new(new()
        {
            new(){ Content = I18nRes.LogLevel_DEBUG,ToolTip = LogLevel.DEBUG},
            new(){ Content = I18nRes.LogLevel_INFO,ToolTip = LogLevel.INFO},
            new(){ Content = I18nRes.LogLevel_WARN,ToolTip = LogLevel.WARN},
        });

        public ComboBoxVM LanguageComboBox { get; set; } = new(new()
        {
            new(){ Content = "简体中文",ToolTip = "zh-CN"},
            new(){ Content = "English",ToolTip = "en-US"},
        });

        public ButtonVM OpenLogFileButton { get; set; } = new()
        {
            Content = I18nRes.OpenLogFile,
            ToolTip = I18nRes.OpenLogFile,
        };

        public SettingsPageViewModel()
        {
            //ExpansionDebugPath = WeakReferenceMessenger.Default.Send<ExpansionDebugPathRequestMessage>();
        }
        public SettingsPageViewModel(bool noop)
        {
            ExpansionDebugPath = WeakReferenceMessenger.Default.Send<ExpansionDebugPathRequestMessage>();
            //LogLevelComboBox.SelectedItem = LogLevelComboBox.First(vm => vm.ToolTip is LogLevel level && level == Logger.Options.DefaultLevel);
            LogLevelComboBox.SelectionChangedEvent += LogLevelComboBox_SelectionChangedEvent;
            LanguageComboBox.SelectionChangedEvent += LanguageComboBox_SelectionChangedEvent;
        }

        private void LanguageComboBox_SelectionChangedEvent(object parameter)
        {
            if (parameter is not ComboBoxItemVM item)
                return;
            ObservableI18n.Language = item.ToolTip!.ToString()!;
        }

        private void LogLevelComboBox_SelectionChangedEvent(object parameter)
        {

        }

        [RelayCommand]
        private void SetExpansionDebugPath()
        {
            var filesName = OpenFileDialogVM.Show(
                new() { Title = I18nRes.SelectDebugFile, Filter = $"Toml {I18nRes.File}|Expansion.toml" }
            );
            if (filesName is null || filesName.Any() is false)
                return;
            string path = Path.GetDirectoryName(filesName.First())!;
            ExpansionDebugPath = path;
            var toml = TOML.Parse(ST.ConfigTomlFile);
            toml["Expansion"]["DebugPath"] = path;
            toml.SaveTo(ST.ConfigTomlFile);
            Logger.Record($"{I18nRes.SetExpansionDebugPath}: {path}");
            if (
                MessageBoxVM.Show(
                    new(I18nRes.EffectiveAfterReload)
                    {
                        Button = MessageBoxVM.Button.YesNo,
                        Icon = MessageBoxVM.Icon.Question
                    }
                ) is MessageBoxVM.Result.Yes
            )
                WeakReferenceMessenger.Default.Send<ExpansionDebugPathChangeMessage>(new(path));
        }
    }
}
