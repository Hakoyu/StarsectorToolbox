﻿using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HKW.Libs.Log4Cs;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialogs;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.Messages;
using StarsectorToolbox.Models.ST;
using I18nRes = StarsectorToolbox.Langs.Pages.Settings.SettingsPageI18nRes;

namespace StarsectorToolbox.ViewModels.Settings;

internal partial class SettingsPageViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableI18n<I18nRes> _i18n = ObservableI18n<I18nRes>.Create(new());

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearExtensionDebugPathCommand))]
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
            i => i.ToolTip is LogLevel level && level == Logger.DefaultOptions.Level
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
        STSettings.Instance.Language = ObservableI18n.Language;
        STSettings.Save();
        Logger.Info($"{I18nRes.LanguageSwitch}: {ObservableI18n.Language}");
    }

    private void ComboBox_LogLevel_SelectionChangedEvent(object parameter)
    {
        if (parameter is not ComboBoxItemVM item)
            return;
        var level = item.ToolTip!.ToString()!;
        if (Logger.DefaultOptions.Level.ToString() == level)
            return;
        STSettings.Instance.LogLevel = level;
        STSettings.Save();
        Logger.DefaultOptions.Level = Logger.LogLevelConverter(level);
        Logger.Info($"{I18nRes.LogLevelSwitch}: {Logger.DefaultOptions.Level}");
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

    private bool ClearButtonCanExecute() => !string.IsNullOrWhiteSpace(ExtensionDebugPath);

    [RelayCommand]
    private static void OpenLogFile()
    {
        Utils.OpenLink(Logger.LogFile);
    }
}