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
using I18n = StarsectorTools.Langs.Pages.Settings.SettingsPageI18nRes;
using StarsectorTools.Libs.Messages;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Windows.MainWindow;

namespace StarsectorTools.Pages.Settings
{
    internal partial class SettingsI18n : INotifyPropertyChanged
    {
        public I18n I18n => new();
        public static SettingsI18n Current { get; private set; }

        public SettingsI18n()
        {
            Current = this;
        }
        private string language;
        public string Language
        {
            get { return language; }
            set
            {
                if (language == value)
                    return;
                language = value;
                var cultureInfo = new CultureInfo(value);
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
                Thread.CurrentThread.CurrentCulture = cultureInfo;
                I18n.Culture = cultureInfo;
                PropertyChanged?.Invoke(this, new(""));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
    internal partial class SettingsPageViewModel : ObservableRecipient
    {

        public static SettingsPageViewModel Instance { get; private set; }
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
            //new(){ Content = SettingsI18n.LogLevel_DEBUG,ToolTip = LogLevel.DEBUG},
            //new(){ Content = SettingsI18n.LogLevel_INFO,ToolTip = LogLevel.INFO},
            //new(){ Content = SettingsI18n.LogLevel_WARN,ToolTip = LogLevel.WARN},
        });

        public ComboBoxVM LanguageComboBox { get; set; } = new(new()
        {
            new(){ Content = "简体中文",ToolTip = "zh-CN"},
            new(){ Content = "English",ToolTip = "en-US"},
        });

        public ButtonVM OpenLogFileButton { get; set; } = new()
        {
            Content = I18n.OpenLogFile,
            ToolTip = I18n.OpenLogFile,
        };

        #region I18n
        [ObservableProperty]
        private string i18nSettings = I18n.Settings;

        [ObservableProperty]
        private string i18nOpenLogFile = I18n.OpenLogFile;

        [ObservableProperty]
        private string i18nLogLevel = I18n.LogLevel;

        [ObservableProperty]
        private string i18nLanguage = I18n.Language;

        [ObservableProperty]
        private string i18nExpansionDebugPath = I18n.ExpansionDebugPath;

        [ObservableProperty]
        private string i18nExpansionDebugPathToolTip = I18n.ExpansionDebugPathToolTip;

        [ObservableProperty]
        private string i18nSet = I18n.Set;

        [ObservableProperty]
        private string i18nClear = I18n.Clear;
        #endregion

        public SettingsPageViewModel()
        {
            //ExpansionDebugPath = WeakReferenceMessenger.Default.Send<ExpansionDebugPathRequestMessage>();
        }
        public SettingsPageViewModel(bool noop)
        {
            Instance = this;
            ExpansionDebugPath = WeakReferenceMessenger.Default.Send<ExpansionDebugPathRequestMessage>();
            //LogLevelComboBox.SelectedItem = LogLevelComboBox.First(vm => vm.ToolTip is LogLevel level && level == Logger.Options.DefaultLevel);
            LogLevelComboBox.SelectionChangedEvent += LogLevelComboBox_SelectionChangedEvent;
            LanguageComboBox.SelectionChangedEvent += LanguageComboBox_SelectionChangedEvent;
        }

        private void LanguageComboBox_SelectionChangedEvent(object parameter)
        {
            if (parameter is not ComboBoxItemVM item)
                return;
            SettingsI18n.Current.Language = item.ToolTip.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaiseProoertyChanged()
        {
            PropertyChanged?.Invoke(this, new(""));
        }

        private void LogLevelComboBox_SelectionChangedEvent(object parameter)
        {

        }

        [RelayCommand]
        private void SetExpansionDebugPath()
        {
            var filesName = OpenFileDialogVM.Show(
                new() { Title = I18n.SelectDebugFile, Filter = $"Toml {I18n.File}|Expansion.toml" }
            );
            if (filesName is null || filesName.Any() is false)
                return;
            string path = Path.GetDirectoryName(filesName.First())!;
            ExpansionDebugPath = path;
            var toml = TOML.Parse(ST.ConfigTomlFile);
            toml["Expansion"]["DebugPath"] = path;
            toml.SaveTo(ST.ConfigTomlFile);
            Logger.Record($"{I18n.SetExpansionDebugPath}: {path}");
            if (
                MessageBoxVM.Show(
                    new(I18n.EffectiveAfterReload)
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
