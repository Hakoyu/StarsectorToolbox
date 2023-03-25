using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using HKW.Libs.Log4Cs;
using HKW.Libs.TomlParse;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialogs;
using StarsectorToolbox.Libs.GameInfo;
using StarsectorToolbox.Libs.Utils;
using StarsectorToolbox.Resources;
using I18nRes = StarsectorToolbox.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorToolbox.ViewModels.Main;

internal partial class MainWindowViewModel
{
    /// <summary>
    /// 单例化
    /// </summary>
    public static MainWindowViewModel Instance { get; private set; } = null!;

    private Dictionary<string, ExtensionInfo> _allExtensionsInfo = new();
    private ListBoxItemVM? _selectedItem;
    private ExtensionInfo? _deubgItemExtensionInfo;
    private ListBoxItemVM? _deubgItem;
    private string? _deubgItemPath;

    internal void Close()
    {
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
        ListBox_MainMenu.Add(item);
    }

    private void DetectPageItemData(ref ListBoxItemVM item)
    {
        if (item?.Tag is not ISTPage page)
            return;
        item.Id = page.GetType().FullName;
        item.Content = page.GetNameI18n();
        item.ToolTip = page.GetDescriptionI18n();
        item.ContextMenu = CreateItemContextMenu();
    }

    private ContextMenuVM CreateItemContextMenu()
    {
        ContextMenuVM contextMenu = new() { RefreshPageMenuItem() };
        return contextMenu;
        MenuItemVM RefreshPageMenuItem()
        {
            MenuItemVM menuItem = new();
            menuItem.Icon = "🔄";
            menuItem.Header = I18nRes.RefreshPage;
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

    private void RefreshPage(ListBoxItemVM vm)
    {
        if (vm.Tag is not ISTPage page)
            return;
        page.Close();
        var type = vm.Tag!.GetType();
        vm.Tag = CreatePage(type);
        if (vm.IsSelected)
            ShowPage(vm.Tag);
        Logger.Info($"{I18nRes.RefreshPage}: {type.FullName}");
    }

    private static object? CreatePage(Type type)
    {
        try
        {
            return type.Assembly.CreateInstance(type.FullName!)!;
        }
        catch (Exception ex)
        {
            Logger.Error($"{I18nRes.PageInitializeError}: {type.FullName}", ex);
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

    private void DetectDebugPageItemData(ref ListBoxItemVM item)
    {
        if (item?.Tag is not ISTPage page)
            return;
        item.Id = page.GetType().FullName;
        item.Content = page.GetNameI18n();
        item.ToolTip = page.GetDescriptionI18n();
        item.ContextMenu = CreateExtensionDebugItemContextMenu();
    }

    private ContextMenuVM CreateExtensionDebugItemContextMenu()
    {
        ContextMenuVM contextMenu = new() { RefreshDebugPageMenuItem() };
        return contextMenu;
        MenuItemVM RefreshDebugPageMenuItem()
        {
            MenuItemVM menuItem = new();
            menuItem.Icon = "🔄";
            menuItem.Header = I18nRes.RefreshPage;
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
        ListBox_MainMenu.Remove(_deubgItem!);
        if (TryGetExtensionDebugItem() is not ListBoxItemVM item)
            return;
        DetectDebugPageItemData(ref item);
        _deubgItem = item;
        ListBox_MainMenu.Add(item);
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
        if (_clearGameLogOnStart)
            ClearGameLogFile();
    }

    private static void ClearGameLogFile()
    {
        if (File.Exists(GameInfo.LogFile))
            Utils.DeleteFileToRecycleBin(GameInfo.LogFile);
        File.Create(GameInfo.LogFile).Close();
        Logger.Info(I18nRes.GameLogCleanupCompleted);
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
            if (_allExtensionsInfo.TryAdd(extensionInfo.Id, extensionInfo) is false)
            {
                var originalExtensionInfo = _allExtensionsInfo[extensionInfo.Id];
                MessageBoxVM.Show(
                    new(
                        $"已存在相同的拓展\n原始文件位置: {originalExtensionInfo.ExtensionFile}\n 再次导入的文件位置{extensionInfo.FileFullName}"
                    )
                );
            }
            ListBox_ExtensionMenu.Add(
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
        ContextMenuVM contextMenu = new() { RefreshExtensionPageMenuItem() };
        return contextMenu;
        MenuItemVM RefreshExtensionPageMenuItem()
        {
            MenuItemVM menuItem = new();
            menuItem.Icon = "🔄";
            menuItem.Header = I18nRes.RefreshPage;
            menuItem.CommandEvent += (p) =>
            {
                if (p is not ListBoxItemVM vm)
                    return;
                if (vm.Tag is not ISTPage page)
                    return;
                page.Close();
                var extensionInfo = _allExtensionsInfo[vm.Id!];
                extensionInfo.ExtensionPage = vm.Tag = CreatePage(extensionInfo.ExtensionType)!;
                if (vm.IsSelected)
                    ShowPage(vm.Tag);
                Logger.Info($"{I18nRes.RefreshPage}: {extensionInfo.Id}");
                GC.Collect();
            };
            return menuItem;
        }
    }

    private ExtensionInfo? TryGetExtensionInfo(string file, bool loadInMemory = false)
    {
        if (string.IsNullOrWhiteSpace(file))
        {
            Logger.Warring(I18nRes.ExtensionPathIsEmpty);
            MessageBoxVM.Show(
                new(I18nRes.ExtensionPathIsEmpty) { Icon = MessageBoxVM.Icon.Warning }
            );
            return null;
        }
        var tomlFile = $"{file}\\{ST.ExtensionInfoFile}";
        try
        {
            var extensionInfo = new ExtensionInfo(TOML.Parse(tomlFile));
            var assemblyFile = ParseExtensionInfo(file, tomlFile, ref extensionInfo);
            // 判断组件文件是否存在
            if (File.Exists(assemblyFile) is false)
            {
                Logger.Warring($"{I18nRes.ExtensionFileError} {I18nRes.Path}: {tomlFile}");
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
                Logger.Warring(
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
            Logger.Error($"{I18nRes.ExtensionLoadError}\n{I18nRes.Path}: {tomlFile}", ex);
            MessageBoxVM.Show(
                new($"{I18nRes.ExtensionLoadError}\n{I18nRes.Path}: {tomlFile}")
                {
                    Icon = MessageBoxVM.Icon.Error
                }
            );
            return null;
        }
        string ParseExtensionInfo(string path, string tomlFile, ref ExtensionInfo extensionInfo)
        {
            // 判断文件存在性
            if (File.Exists(tomlFile) is false)
            {
                Logger.Warring($"{I18nRes.ExtensionTomlFileNotFound} {I18nRes.Path}: {tomlFile}");
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
            if (_allExtensionsInfo.ContainsKey(extensionInfo.ExtensionId))
            {
                Logger.Warring($"{I18nRes.ExtensionAlreadyExists} {I18nRes.Path}: {tomlFile}");
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
                type = assembly.GetType(extensionInfo.ExtensionId)!;
            }
            else
            {
                assembly = Assembly.LoadFile(assemblyFile);
                type = assembly.GetType(extensionInfo.ExtensionId)!;
            }
            // 判断是否成功获取了类型
            if (type is null)
            {
                var assemblyStr = string.Join("\t\n", assembly.ExportedTypes);
                Logger.Warring(
                    $"{I18nRes.ExtensionIdError} {I18nRes.Path}: {tomlFile}\n{I18nRes.ExtensionContainedClass}:\n{assemblyStr}"
                );
                MessageBoxVM.Show(
                    new(
                        $"{I18nRes.ExtensionIdError}\n{I18nRes.Path}: {tomlFile}\n{I18nRes.ExtensionContainedClass}:\n{assemblyStr}"
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
            if (Utils.FileExists(ST.ConfigTomlFile, false))
                return GetConfig();
            else
                return CreateConfig();
        }
        catch (Exception ex)
        {
            Logger.Error($"{I18nRes.ConfigFileError} {I18nRes.Path}: {ST.ConfigTomlFile}", ex);
            MessageBoxVM.Show(
                new($"{I18nRes.ConfigFileError}\n{I18nRes.Path}: {ST.ConfigTomlFile}")
                {
                    Icon = MessageBoxVM.Icon.Error,
                }
            );
            CreateConfigFile();
            return true;
        }
    }

    private bool GetConfig()
    {
        // 读取设置
        var toml = TOML.Parse(ST.ConfigTomlFile);
        // 语言
        var cultureInfo = CultureInfo.GetCultureInfo(toml["Lang"].AsString);
        ObservableI18n.Language = cultureInfo.Name;
        // 日志等级
        Logger.Options.DefaultLevel = Logger.LogLevelConverter(toml["LogLevel"].AsString);
        // 游戏目录
        if (GameInfo.SetGameData(toml["Game"]["Path"].AsString) is false)
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
            toml["Game"]["Path"] = GameInfo.BaseDirectory;
        }
        // 拓展调试目录
        string debugPath = toml["Extension"]["DebugPath"].AsString;
        if (
            !string.IsNullOrWhiteSpace(debugPath)
            && TryGetExtensionInfo(debugPath, true) is ExtensionInfo info
        )
        {
            _deubgItemExtensionInfo = info;
            _deubgItemPath = debugPath;
        }
        else
            toml["Extension"]["DebugPath"] = "";
        ClearGameLogOnStart = toml["Game"]["ClearLogOnStart"].AsBoolean;
        toml.SaveTo(ST.ConfigTomlFile);
        return true;
    }

    private static bool CreateConfig()
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
        CreateConfigFile();
        var toml = TOML.Parse(ST.ConfigTomlFile);
        toml["Game"]["Path"] = GameInfo.BaseDirectory;
        toml["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
        toml.SaveTo(ST.ConfigTomlFile);
        return true;
    }

    /// <summary>
    /// 创建配置文件
    /// </summary>
    private static void CreateConfigFile()
    {
        using StreamReader sr = ResourceDictionary.GetResourceStream(
            ResourceDictionary.Config_toml
        );
        File.WriteAllText(ST.ConfigTomlFile, sr.ReadToEnd());
        Logger.Info($"{I18nRes.ConfigFileCreationCompleted} {I18nRes.Path}: {ST.ConfigTomlFile}");
    }

    #endregion InitializeConfig

    #region WindowEffect

    /// <summary>
    /// 设置窗口效果委托
    /// </summary>
    internal Action<bool> _setWindowEffectAction = null!;

    /// <summary>
    /// 取消窗口效果委托
    /// </summary>
    internal Action _removeWindowEffectAction = null!;

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
        foreach (var item in ListBox_MainMenu)
            ReminderSavePage(item);
    }

    private void ReminderSaveExtensionPages()
    {
        foreach (var item in ListBox_ExtensionMenu)
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
            Logger.Error($"{I18nRes.PageSaveError} {type.FullName}", ex);
            MessageBoxVM.Show(
                new($"{I18nRes.PageSaveError} {type.FullName}\n{Logger.FilterException(ex)}")
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
        foreach (var item in ListBox_MainMenu)
            SavePage(item);
    }

    private void SaveExtensionPages()
    {
        foreach (var item in ListBox_ExtensionMenu)
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
            Logger.Error($"{I18nRes.PageSaveError} {type.FullName}", ex);
            MessageBoxVM.Show(
                new($"{I18nRes.PageSaveError} {type.FullName}\n{Logger.FilterException(ex)}")
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
        foreach (var page in ListBox_MainMenu)
            ClosePage(page);
    }

    private void CloseExtensionPages()
    {
        foreach (var page in ListBox_ExtensionMenu)
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
            Logger.Error($"{I18nRes.PageCloseError} {type.FullName}", ex);
            MessageBoxVM.Show(
                new($"{I18nRes.PageCloseError} {type.FullName}\n{Logger.FilterException(ex)}")
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
        if (ex.InnerException is not null)
            ex = ex.InnerException;
        if (ex.Source is nameof(StarsectorToolbox))
        {
            Logger.Error(I18nRes.GlobalException, ex, false);
            MessageBoxVM.Show(
                new($"{I18nRes.GlobalExceptionMessage}\n\n{Logger.FilterException(ex)}")
                {
                    Icon = MessageBoxVM.Icon.Error,
                }
            );
        }
        else
        {
            Logger.Error($"{I18nRes.GlobalExtensionException}: {ex.Source}", ex, false);
            MessageBoxVM.Show(
                new(
                    $"{string.Format(I18nRes.GlobalExtensionExceptionMessage, ex.Source)}\n\n{Logger.FilterException(ex)}"
                )
                {
                    Icon = MessageBoxVM.Icon.Error,
                }
            );
        }
    }
}
