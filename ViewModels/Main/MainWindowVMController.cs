﻿using System.Globalization;
using System.IO;
using System.Reflection;
using HKW.HKWViewModels;
using HKW.HKWViewModels.Controls;
using HKW.HKWViewModels.Dialogs;
using HKW.TOML.Deserializer;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.Models.ST;
using I18nRes = StarsectorToolbox.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorToolbox.ViewModels.Main;

internal partial class MainWindowViewModel
{
    /// <summary>
    /// 单例化
    /// </summary>
    public static MainWindowViewModel Instance { get; private set; } = null!;

    private readonly Dictionary<string, ExtensionInfo> r_allExtensionsInfo = new();
    private ListBoxItemVM? _selectedItem;
    private ExtensionInfo? _deubgItemExtensionInfo;
    private ListBoxItemVM? _deubgItem;
    private string? _deubgItemPath;

    internal void Close()
    {
        CrashReporterWindow.ForcedClose();
        ReminderSaveAllPages();
        CloseAllPages();
    }

    private static void InitializeDirectories()
    {
        Directory.CreateDirectory(ST.CoreDirectory);
        Directory.CreateDirectory(ST.ExtensionDirectories);
    }

    #region PageItem

    internal void AddMainPageItem(ListBoxItemVM item)
    {
        DetectPageItemData(ref item);
        ListBox_MainMenu.ItemsSource.Add(item);
    }

    private void DetectPageItemData(ref ListBoxItemVM item)
    {
        if (item?.Tag is not ISTPage page)
            return;
        item.Id = page.GetType().FullName;
        item.Content = ObservableI18n.BindingValue(
            item,
            (value, target) => target.Content = value,
            () => page.GetNameI18n()
        );
        item.ToolTip = ObservableI18n.BindingValue(
            item,
            (value, target) => target.ToolTip = value,
            () => page.GetDescriptionI18n()
        );
        item.ContextMenu = CreateItemContextMenu();
    }

    private ContextMenuVM CreateItemContextMenu()
    {
        ContextMenuVM contextMenu = new();
        contextMenu.ItemsSource.Add(RefreshPageMenuItem());
        return contextMenu;
        MenuItemVM RefreshPageMenuItem()
        {
            MenuItemVM menuItem = new();
            menuItem.Icon = "🔄";
            menuItem.Header = ObservableI18n.BindingValue(
                (value) => menuItem.Header = value,
                () => I18nRes.RefreshPage
            );
            menuItem.CommandEvent += (p) =>
            {
                if (p is not ListBoxItemVM vm)
                    return;
                RefreshPage(vm);
                GC.Collect();
            };
            return menuItem;
        }
    }

    private void RefreshPage(ListBoxItemVM item)
    {
        if (item.Tag is not ISTPage page)
            return;
        page.Close();
        var type = item.Tag!.GetType();
        item.Tag = CreatePage(type);
        if (item.IsSelected)
            ShowPage(item.Tag);
        sr_logger.Info($"{I18nRes.RefreshPage}: {type.FullName}");
    }

    private static object? CreatePage(Type type)
    {
        try
        {
            return type.Assembly.CreateInstance(type.FullName!)!;
        }
        catch (Exception ex)
        {
            sr_logger.Error(ex, $"{I18nRes.PageInitializeError}: {type.FullName}");
            MessageBoxVM.Show(
                new($"{I18nRes.PageInitializeError}:\n{type.FullName}")
                {
                    Icon = MessageBoxVM.Icon.Error
                }
            );
            return null;
        }
    }

    #endregion PageItem

    #region DebugPageItem

    private void DetectDebugPageItemData(ListBoxItemVM item)
    {
        if (item?.Tag is not ISTPage page)
            return;
        item.Id = page.GetType().FullName;
        item.Content = ObservableI18n.BindingValue(
            item,
            (value, target) => target.Content = value,
            () => page.GetNameI18n()
        );
        item.ToolTip = ObservableI18n.BindingValue(
            item,
            (value, target) => target.ToolTip = value,
            () => page.GetDescriptionI18n()
        );
        item.ContextMenu = CreateExtensionDebugItemContextMenu();
    }

    private ContextMenuVM CreateExtensionDebugItemContextMenu()
    {
        ContextMenuVM contextMenu = new();
        contextMenu.ItemsSource.Add(RefreshDebugPageMenuItem());
        return contextMenu;
        MenuItemVM RefreshDebugPageMenuItem()
        {
            MenuItemVM menuItem = new();
            menuItem.Icon = "🔄";
            menuItem.Header = ObservableI18n.BindingValue(
                (value) => menuItem.Header = value,
                () => I18nRes.RefreshPage
            );
            menuItem.CommandEvent += (p) =>
            {
                if (p is not ListBoxItemVM vm)
                    return;
                if (vm.Tag is not ISTPage page)
                    return;
                page.Close();
                if (TryGetExtensionInfo(_deubgItemPath!, true) is not ExtensionInfo extensionInfo)
                    return;
                _deubgItemExtensionInfo = extensionInfo;
                RefreshExtensionDebugPage();
                GC.Collect();
            };
            return menuItem;
        }
    }

    private void RefreshExtensionDebugPage()
    {
        ListBox_MainMenu.ItemsSource.Remove(_deubgItem!);
        if (TryGetExtensionDebugItem() is not ListBoxItemVM item)
            return;
        DetectDebugPageItemData(item);
        _deubgItem = item;
        ListBox_MainMenu.ItemsSource.Add(item);
        ListBox_MainMenu.SelectedItem = _deubgItem;
        NowPage = _deubgItem?.Tag;
    }

    private ListBoxItemVM? TryGetExtensionDebugItem()
    {
        // 添加拓展调试页面
        if (_deubgItemExtensionInfo is null)
            return null;
        return new()
        {
            Icon = _deubgItemExtensionInfo.Icon,
            Tag = _deubgItemExtensionInfo.ExtensionPage,
        };
    }
    #endregion

    #region CheckGameStartOption

    private void CheckGameStartOption()
    {
        if (ClearGameLogOnStart)
            Utils.ClearGameLog();
    }

    #endregion CheckGameStartOption

    #region ExtensionPage

    private void InitializeExtensionPages()
    {
        DirectoryInfo dirs = new(ST.ExtensionDirectories);
        foreach (var dir in dirs.GetDirectories())
        {
            if (TryGetExtensionInfo(dir.FullName) is not ExtensionInfo extensionInfo)
                continue;
            if (r_allExtensionsInfo.TryAdd(extensionInfo.Id, extensionInfo) is false)
            {
                var originalExtensionInfo = r_allExtensionsInfo[extensionInfo.Id];
                MessageBoxVM.Show(
                    new(
                        $"已存在相同的拓展\n原始文件位置: {originalExtensionInfo.ExtensionFile}\n 再次导入的文件位置{extensionInfo.FileFullName}"
                    )
                );
            }
            ListBox_ExtensionMenu.ItemsSource.Add(
                new()
                {
                    Id = extensionInfo.Id,
                    Icon = extensionInfo.Icon,
                    Content = extensionInfo.Name,
                    ToolTip =
                        $"Author: {extensionInfo.Author}\nDescription: {extensionInfo.Description}",
                    Tag = extensionInfo.ExtensionPage,
                    ContextMenu = CreateExtensionItemContextMenu()
                }
            );
            ;
        }
    }

    private ContextMenuVM CreateExtensionItemContextMenu()
    {
        ContextMenuVM contextMenu = new();
        contextMenu.ItemsSource.Add(RefreshExtensionPageMenuItem());
        return contextMenu;
        MenuItemVM RefreshExtensionPageMenuItem()
        {
            MenuItemVM menuItem = new();
            menuItem.Icon = "🔄";
            menuItem.Header = ObservableI18n.BindingValue(
                (value) => menuItem.Header = value,
                () => I18nRes.RefreshPage
            );
            menuItem.CommandEvent += (p) =>
            {
                if (p is not ListBoxItemVM vm)
                    return;
                if (vm.Tag is not ISTPage page)
                    return;
                page.Close();
                var extensionInfo = r_allExtensionsInfo[vm.Id!];
                extensionInfo.ExtensionPage = vm.Tag = CreatePage(extensionInfo.ExtensionType)!;
                if (vm.IsSelected)
                    ShowPage(vm.Tag);
                sr_logger.Info($"{I18nRes.RefreshPage}: {extensionInfo.Id}");
                GC.Collect();
            };
            return menuItem;
        }
    }

    private ExtensionInfo? TryGetExtensionInfo(string file, bool loadInMemory = false)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            sr_logger.Warn(I18nRes.ExtensionPathIsEmpty);
            MessageBoxVM.Show(
                new(I18nRes.ExtensionPathIsEmpty) { Icon = MessageBoxVM.Icon.Warning }
            );
            return null;
        }
        var tomlFile = $"{file}\\{ST.ExtensionInfoFile}";
        try
        {
            var extensionInfo = TomlDeserializer.DeserializeFromFile<ExtensionInfo>(tomlFile);
            var assemblyFile = ParseExtensionInfo(file, tomlFile, extensionInfo);
            // 判断组件文件是否存在
            if (File.Exists(assemblyFile) is false)
            {
                sr_logger.Warn($"{I18nRes.ExtensionFileError} {I18nRes.Path}: {tomlFile}");
                MessageBoxVM.Show(
                    new($"{I18nRes.ExtensionFileError}\n{I18nRes.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
                return null;
            }
            if (
                TryGetExtensionPage(tomlFile, assemblyFile, extensionInfo, loadInMemory)
                is not
                (object page, Type type)
            )
                return null;
            extensionInfo.FileFullName = file;
            extensionInfo.ExtensionPage = page;
            extensionInfo.ExtensionType = type;
            // 判断页面是否实现了接口
            if (extensionInfo.ExtensionPage is not ISTPage)
            {
                sr_logger.Warn(
                    $"{I18nRes.ExtensionPageError}: {I18nRes.NotImplementedISTPage}\n{I18nRes.Path}: {tomlFile}"
                );
                MessageBoxVM.Show(
                    new(
                        $"{I18nRes.ExtensionPageError}: {I18nRes.NotImplementedISTPage}\n{I18nRes.Path}: {tomlFile}"
                    )
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
                return null;
            }
            return extensionInfo;
        }
        catch (Exception ex)
        {
            sr_logger.Error(ex, $"{I18nRes.ExtensionLoadError}\n{I18nRes.Path}: {tomlFile}");
            MessageBoxVM.Show(
                new($"{I18nRes.ExtensionLoadError}\n{I18nRes.Path}: {tomlFile}")
                {
                    Icon = MessageBoxVM.Icon.Error
                }
            );
            return null;
        }
        string ParseExtensionInfo(string path, string tomlFile, ExtensionInfo extensionInfo)
        {
            // 判断文件存在性
            if (File.Exists(tomlFile) is false)
            {
                sr_logger.Warn($"{I18nRes.ExtensionTomlFileNotFound} {I18nRes.Path}: {tomlFile}");
                MessageBoxVM.Show(
                    new($"{I18nRes.ExtensionTomlFileNotFound}\n{I18nRes.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
                return string.Empty;
            }
            var assemblyFile = $"{path}\\{extensionInfo.ExtensionFile}";
            // 检测是否有相同的拓展
            if (r_allExtensionsInfo.ContainsKey(extensionInfo.ExtensionPublic))
            {
                sr_logger.Warn($"{I18nRes.ExtensionAlreadyExists} {I18nRes.Path}: {tomlFile}");
                MessageBoxVM.Show(
                    new($"{I18nRes.ExtensionAlreadyExists}\n{I18nRes.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
                return string.Empty;
            }
            return assemblyFile;
        }
        (object?, Type?) TryGetExtensionPage(
            string tomlFile,
            string assemblyFile,
            ExtensionInfo extensionInfo,
            bool loadInMemory
        )
        {
            Type type;
            Assembly assembly;
            // 从内存或外部载入
            if (loadInMemory)
            {
                var bytes = File.ReadAllBytes(assemblyFile);
                assembly = Assembly.Load(bytes, bytes);
                type = assembly.GetType(extensionInfo.ExtensionPublic)!;
            }
            else
            {
                assembly = Assembly.LoadFile(assemblyFile);
                type = assembly.GetType(extensionInfo.ExtensionPublic)!;
            }
            // 判断是否成功获取了类型
            if (type is null)
            {
                var assemblyStr = string.Join("\t\n", assembly.ExportedTypes);
                sr_logger.Warn(
                    $"{I18nRes.ExtensionPublicError} {I18nRes.Path}: {tomlFile}\n{I18nRes.ExtensionContainedClass}:\n{assemblyStr}"
                );
                MessageBoxVM.Show(
                    new(
                        $"{I18nRes.ExtensionPublicError}\n{I18nRes.Path}: {tomlFile}\n{I18nRes.ExtensionContainedClass}:\n{assemblyStr}"
                    )
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
                return (null, null);
            }
            return (CreatePage(type), type);
        }
    }

    #endregion ExtensionPage

    #region InitializeConfig

    private bool InitializeConfig()
    {
        try
        {
            if (File.Exists(ST.SettingsTomlFile))
            {
                STSettings.Initialize(ST.SettingsTomlFile);
                return GetConfig();
            }
            else
            {
                return FirstCreateConfig(ST.SettingsTomlFile);
            }
        }
        catch (Exception ex)
        {
            sr_logger.Error(ex, $"{I18nRes.ConfigFileError} {I18nRes.Path}: {ST.SettingsTomlFile}");
            MessageBoxVM.Show(
                new($"{I18nRes.ConfigFileError}\n{I18nRes.Path}: {ST.SettingsTomlFile}")
                {
                    Icon = MessageBoxVM.Icon.Error,
                }
            );
            return FirstCreateConfig(ST.SettingsTomlFile);
        }
    }

    private bool GetConfig()
    {
        // 设置语言
        ObservableI18n.CurrentCulture = CultureInfo.GetCultureInfo(STSettings.Instance.Language);
        // 设置游戏目录
        if (GameInfo.SetGameData(STSettings.Instance.Game.Path) is false)
        {
            if (
                MessageBoxVM.Show(
                    new(I18nRes.GameNotFound_SelectAgain)
                    {
                        Button = MessageBoxVM.Button.YesNo,
                        Icon = MessageBoxVM.Icon.Question,
                    }
                ) is MessageBoxVM.Result.No
                || GameInfo.GetGameDirectory() is false
            )
            {
                MessageBoxVM.Show(
                    new(I18nRes.GameNotFound_SoftwareExit) { Icon = MessageBoxVM.Icon.Error }
                );
                return false;
            }
            STSettings.Instance.Game.Path = GameInfo.BaseDirectory;
        }
        // 设置拓展调试目录
        string debugPath = STSettings.Instance.Extension.DebugPath;
        if (
            !string.IsNullOrWhiteSpace(debugPath)
            && TryGetExtensionInfo(debugPath, true) is ExtensionInfo info
        )
        {
            _deubgItemExtensionInfo = info;
            _deubgItemPath = debugPath;
        }
        else
            STSettings.Instance.Extension.DebugPath = string.Empty;

        ClearGameLogOnStart = STSettings.Instance.Game.ClearLogOnStart;
        STSettings.Save();
        return true;
    }

    private static bool FirstCreateConfig(string tomlFile)
    {
        if (
            MessageBoxVM.Show(
                new(I18nRes.FirstStart)
                {
                    Button = MessageBoxVM.Button.YesNo,
                    Icon = MessageBoxVM.Icon.Question,
                }
            ) is MessageBoxVM.Result.No
            || GameInfo.GetGameDirectory() is false
        )
        {
            MessageBoxVM.Show(
                new(I18nRes.GameNotFound_SoftwareExit) { Icon = MessageBoxVM.Icon.Error }
            );
            return false;
        }
        File.Create(tomlFile).Close();
        STSettings.Reset(tomlFile);
        STSettings.Instance.Game.Path = GameInfo.BaseDirectory;
        STSettings.Save();
        return true;
    }

    #endregion InitializeConfig

    #region WindowEffect

    /// <summary>
    /// 设置窗口效果委托
    /// </summary>
    private Action<bool> _setWindowEffectAction = null!;

    /// <summary>
    /// 取消窗口效果委托
    /// </summary>
    private Action _removeWindowEffectAction = null!;

    internal void RegisterChangeWindowEffectEvent(
        Action<bool> setWindowEffectAction,
        Action removeWindowEffectAction
    )
    {
        _setWindowEffectAction = setWindowEffectAction;
        _removeWindowEffectAction = removeWindowEffectAction;
    }

    internal void SetBlurEffect(bool isEnabled)
    {
        _setWindowEffectAction(isEnabled);
    }

    internal void RemoveBlurEffect()
    {
        _removeWindowEffectAction();
    }

    #endregion WindowEffect

    #region ReminderSavePage

    private void ReminderSaveAllPages()
    {
        using var handler = PendingBoxVM.Show(I18nRes.Saving);
        ReminderSaveMainPages();
        ReminderSaveExtensionPages();
    }

    private void ReminderSaveMainPages()
    {
        foreach (var item in ListBox_MainMenu.ItemsSource)
            ReminderSavePage(item);
    }

    private void ReminderSaveExtensionPages()
    {
        foreach (var item in ListBox_ExtensionMenu.ItemsSource)
            ReminderSavePage(item);
    }

    private static void ReminderSavePage(ListBoxItemVM vm)
    {
        if (vm.Tag is not ISTPage page)
            return;
        try
        {
            if (page.NeedSave is false)
                return;
            if (
                MessageBoxVM.Show(
                    new($"{I18nRes.Page}: {vm.Content} {I18nRes.PageCheckSave}")
                    {
                        Icon = MessageBoxVM.Icon.Question,
                        Button = MessageBoxVM.Button.YesNo
                    }
                ) is MessageBoxVM.Result.Yes
            )
            {
                SavePage(vm);
            }
        }
        catch (Exception ex)
        {
            var type = page.GetType();
            sr_logger.Error(ex, $"{I18nRes.PageSaveError} {type.FullName}");
            MessageBoxVM.Show(
                new($"{I18nRes.PageSaveError} {type.FullName}\n{ex}")
                {
                    Icon = MessageBoxVM.Icon.Error
                }
            );
        }
    }

    #endregion ReminderSavePage

    #region SavePage

    private void SaveAllPages()
    {
        SaveMainPages();
        SaveExtensionPages();
    }

    private void SaveMainPages()
    {
        foreach (var item in ListBox_MainMenu.ItemsSource)
            SavePage(item);
    }

    private void SaveExtensionPages()
    {
        foreach (var item in ListBox_ExtensionMenu.ItemsSource)
            SavePage(item);
    }

    private static void SavePage(ListBoxItemVM vm)
    {
        if (vm.Tag is not ISTPage page)
            return;
        try
        {
            page.Save();
        }
        catch (Exception ex)
        {
            var type = page.GetType();
            sr_logger.Error(ex, $"{I18nRes.PageSaveError} {type.FullName}");
            MessageBoxVM.Show(
                new($"{I18nRes.PageSaveError} {type.FullName}\n{ex}")
                {
                    Icon = MessageBoxVM.Icon.Error
                }
            );
        }
    }

    #endregion SavePage

    #region ClosePage

    private void CloseAllPages()
    {
        CloseMainPages();
        CloseExtensionPages();
    }

    private void CloseMainPages()
    {
        foreach (var page in ListBox_MainMenu.ItemsSource)
            ClosePage(page);
    }

    private void CloseExtensionPages()
    {
        foreach (var page in ListBox_ExtensionMenu.ItemsSource)
            ClosePage(page);
    }

    private static void ClosePage(ListBoxItemVM vm)
    {
        if (vm.Tag is not ISTPage page)
            return;
        try
        {
            page.Close();
        }
        catch (Exception ex)
        {
            var type = page.GetType();
            sr_logger.Error(ex, $"{I18nRes.PageCloseError} {type.FullName}", ex);
            MessageBoxVM.Show(
                new($"{I18nRes.PageCloseError} {type.FullName}\n{ex}")
                {
                    Icon = MessageBoxVM.Icon.Error
                }
            );
        }
    }

    #endregion ClosePage

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        var sourceException = ex;
        // 获取源异常
        while (sourceException.InnerException is not null)
            sourceException = sourceException.InnerException;
        if (sourceException.Source is nameof(StarsectorToolbox))
        {
            sr_logger.Error(ex, I18nRes.GlobalException);
            MessageBoxVM.Show(
                new($"{I18nRes.GlobalExceptionMessage}\n\n{ex}") { Icon = MessageBoxVM.Icon.Error, }
            );
        }
        else
        {
            sr_logger.Error(ex, $"{I18nRes.GlobalExtensionException}: {ex.Source}");
            MessageBoxVM.Show(
                new($"{string.Format(I18nRes.GlobalExtensionExceptionMessage, ex.Source)}\n\n{ex}")
                {
                    Icon = MessageBoxVM.Icon.Error,
                }
            );
        }
    }
}
