using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HKW.HKWViewModels;
using HKW.HKWViewModels.Controls;
using HKW.HKWViewModels.Dialogs;
using Microsoft.Win32;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.Messages;
using StarsectorToolbox.Models.ST;
using I18nRes = StarsectorToolbox.Langs.Pages.Settings.SettingsPageI18nRes;

namespace StarsectorToolbox.ViewModels.Settings;

internal partial class SettingsPageViewModel : ObservableObject
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private ObservableI18n<I18nRes> _i18n = ObservableI18n<I18nRes>.Create(new());

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearExtensionDebugPathCommand))]
    private string? _extensionDebugPath;

    [ObservableProperty]
    private ComboBoxVM _comboBox_Language =
        new(
            new ObservableCollection<ComboBoxItemVM>()
            {
                new() { Content = "English", ToolTip = "en-US" },
                new() { Content = "简体中文", ToolTip = "zh-CN" },
            }
        );

    [ObservableProperty]
    private ComboBoxVM _comboBox_Theme =
        new(() =>
        {
            var items = new ObservableCollection<ComboBoxItemVM>();
            var item = new ComboBoxItemVM();
            item.Tag = nameof(I18nRes.WindowsDefault);
            item.Content = ObservableI18n.BindingValue(
                item,
                (value, taget) => taget.Content = value,
                () => I18nRes.WindowsDefault
            );
            items.Add(item);
            item = new ComboBoxItemVM();
            item.Tag = nameof(I18nRes.Light);
            item.Content = ObservableI18n.BindingValue(
                item,
                (value, taget) => taget.Content = value,
                () => I18nRes.Light
            );
            items.Add(item);
            item = new ComboBoxItemVM();
            item.Tag = nameof(I18nRes.Dark);
            item.Content = ObservableI18n.BindingValue(
                item,
                (value, taget) => taget.Content = value,
                () => I18nRes.Dark
            );
            items.Add(item);
            return items;
        });

    public SettingsPageViewModel()
    {
        // https://github.com/CommunityToolkit/dotnet/issues/604
        // ExtensionDebugPath = WeakReferenceMessenger.Default.Send<ExtensionDebugPathRequestMessage>();
    }

    public SettingsPageViewModel(bool noop)
    {
        I18n.AddCultureChangedAction(CultureChangedAction);
        ExtensionDebugPath = WeakReferenceMessenger.Default
            .Send<ExtensionDebugPathRequestMessage>()
            .Response;
        // 设置Language初始值
        ComboBox_Language.SelectedItem = ComboBox_Language.ItemsSource.FirstOrDefault(
            i => i.ToolTip is string language && language == ObservableI18n.CurrentCulture.Name,
            ComboBox_Language.ItemsSource[0]
        );
        // 设置Theme初始值
        ComboBox_Theme.SelectedItem = ComboBox_Theme.ItemsSource.FirstOrDefault(
            i => i.Tag is string theme && theme == STSettings.Instance.Theme,
            ComboBox_Theme.ItemsSource[0]
        );
        if (ComboBox_Theme.SelectedItem.Tag is string theme && theme != STSettings.Instance.Theme)
        {
            STSettings.Instance.Theme = nameof(I18nRes.WindowsDefault);
            STSettings.Save();
        }
        // 注册事件
        ComboBox_Language.SelectionChangedEvent += ComboBox_Language_SelectionChangedEvent;
        WeakReferenceMessenger.Default.Register<ExtensionDebugPathErrorMessage>(
            this,
            ExtensionDebugPathErrorReceive
        );
    }

    private void ExtensionDebugPathErrorReceive(
        object recipient,
        ExtensionDebugPathErrorMessage message
    )
    {
        ExtensionDebugPath = string.Empty;
    }

    private void CultureChangedAction(CultureInfo cultureInfo) { }

    private void ComboBox_Language_SelectionChangedEvent(ComboBoxItemVM item)
    {
        var language = item.ToolTip!.ToString()!;
        if (ObservableI18n.CurrentCulture.Name == language)
            return;
        ObservableI18n.CurrentCulture = CultureInfo.GetCultureInfo(item.ToolTip!.ToString()!);
        STSettings.Instance.Language = ObservableI18n.CurrentCulture.Name;
        STSettings.Save();
        sr_logger.Info($"{I18nRes.LanguageSwitch}: {ObservableI18n.CurrentCulture.Name}");
    }

    //private void ComboBox_Theme_SelectionChangedEvent(ComboBoxItemVM item)
    //{
    //    if (item.Tag is not string themeName)
    //        return;
    //    STSettings.Instance.Theme = themeName;
    //    STSettings.Save();
    //    if (themeName == nameof(I18nRes.WindowsDefault))
    //        themeName = WindowsThemeIsLight() ? nameof(I18nRes.Light) : nameof(I18nRes.Dark);
    //    GlobalSettings.ChangeTheme(themeName);
    //}

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
            sr_logger.Info($"{I18nRes.SetExtensionDebugPath}: {ExtensionDebugPath}");
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
            sr_logger.Info($"{I18nRes.ClearExtensionDebugPath}: {ExtensionDebugPath}");
        }
    }

    private bool ClearButtonCanExecute() => !string.IsNullOrWhiteSpace(ExtensionDebugPath);

    [RelayCommand]
    private static void OpenLogFile()
    {
        Utils.OpenLink(ST.LogFile);
    }
}
