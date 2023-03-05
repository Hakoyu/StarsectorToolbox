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
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Resources;
using I18nRes = StarsectorTools.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorTools.ViewModels.MainWindow
{
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

        private void InitializeDirectories()
        {
            if (!Directory.Exists(ST.CoreDirectory))
                Directory.CreateDirectory(ST.CoreDirectory);
            if (!Directory.Exists(ST.ExtensionDirectories))
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

        private object? CreatePage(Type type)
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
        #endregion

        #region DebugPageItem
        private void AddDebugMainPageItem(ListBoxItemVM item)
        {
            DetectDebugPageItemData(ref item);
            ListBox_MainMenu.Add(item);
        }

        private void DetectDebugPageItemData(ref ListBoxItemVM item)
        {
            if (item?.Tag is not ISTPage page)
                return;
            item.Id = page.GetType().FullName;
            item.Content = page.GetNameI18n();
            item.ToolTip = page.GetDescriptionI18n();
            item.ContextMenu = CreateDebugItemContextMenu();
        }

        private ContextMenuVM CreateDebugItemContextMenu()
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
                    if (
                        TryGetExtensionInfo(_deubgItemPath!, true) is not ExtensionInfo extensionInfo
                    )
                        return;
                    _deubgItemExtensionInfo = extensionInfo;
                    RefreshExtensionDebugPage();
                    GC.Collect();
                };
                return menuItem;
            }
        }
        #endregion
        #region CheckGameStartOption
        private void CheckGameStartOption()
        {
            if (_clearGameLogOnStart)
                ClearGameLogFile();
        }

        private void ClearGameLogFile()
        {
            if (File.Exists(GameInfo.LogFile))
                Utils.DeleteFileToRecycleBin(GameInfo.LogFile);
            File.Create(GameInfo.LogFile).Close();
            Logger.Info(I18nRes.GameLogCleanupCompleted);
        }
        #endregion
        #region ExtensionPage
        private void InitializeExtensionPages()
        {
            DirectoryInfo dirs = new(ST.ExtensionDirectories);
            foreach (var dir in dirs.GetDirectories())
            {
                if (TryGetExtensionInfo(dir.FullName) is ExtensionInfo extensionInfo)
                {
                    var page = extensionInfo.ExtensionPage;
                    ListBox_ExtensionMenu.Add(
                        new()
                        {
                            Id = extensionInfo.Id,
                            Icon = extensionInfo.Icon,
                            Content = extensionInfo.Name,
                            ToolTip =
                                $"Author: {extensionInfo.Author}\nDescription: {extensionInfo.Description}",
                            Tag = page
                        }
                    );
                }
            }
        }

        private void RefreshExtensionDebugPage()
        {
            ListBox_MainMenu.Remove(_deubgItem!);
            InitializeExtensionDebugPage();
            ListBox_MainMenu.SelectedItem = _deubgItem;
            NowPage = _deubgItem!.Tag!;
        }

        private void InitializeExtensionDebugPage()
        {
            // 添加拓展调试页面
            if (_deubgItemExtensionInfo is not null)
            {
                _deubgItem = new()
                {
                    Icon = _deubgItemExtensionInfo.Icon,
                    Tag = _deubgItemExtensionInfo.ExtensionPage,
                };
                AddDebugMainPageItem(_deubgItem);
            }
        }

        private ExtensionInfo? TryGetExtensionInfo(string path, bool loadInMemory = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Logger.Warring(I18nRes.ExtensionPathIsEmpty);
                MessageBoxVM.Show(
                    new(I18nRes.ExtensionPathIsEmpty) { Icon = MessageBoxVM.Icon.Warning }
                );
                return null;
            }
            var tomlFile = $"{path}\\{ST.ExtensionInfoFile}";
            try
            {
                var extensionInfo = new ExtensionInfo(TOML.Parse(tomlFile));
                var assemblyFile = ParseExtensionInfo(path, tomlFile, ref extensionInfo);
                // 判断组件文件是否存在
                if (!File.Exists(assemblyFile))
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
                extensionInfo.ExtensionPage = TryGetExtensionPage(
                    tomlFile,
                    assemblyFile,
                    ref extensionInfo,
                    loadInMemory
                )!;
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
                Logger.Error($"{I18nRes.ExtensionLoadError} {I18nRes.Path}: {tomlFile}", ex);
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
                if (!File.Exists(tomlFile))
                {
                    Logger.Warring(
                        $"{I18nRes.ExtensionTomlFileNotFound} {I18nRes.Path}: {tomlFile}"
                    );
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
            object? TryGetExtensionPage(
                string tomlFile,
                string assemblyFile,
                ref ExtensionInfo extensionInfo,
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
                    return null;
                }
                return type.Assembly.CreateInstance(type.FullName!)!;
            }
        }

        #endregion
        #region InitializeConfig
        private void InitializeConfig()
        {
            try
            {
                if (Utils.FileExists(ST.ConfigTomlFile, false))
                    GetConfig();
                else
                    CreateConfig();
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
            }
        }

        private void GetConfig()
        {
            // 读取设置
            var toml = TOML.Parse(ST.ConfigTomlFile);
            // 语言
            var cultureInfo = CultureInfo.GetCultureInfo(toml["Lang"].AsString);
            ObservableI18n.Language = cultureInfo.Name;
            // 日志等级
            Logger.Options.DefaultLevel = Logger.LogLevelConverter(toml["LogLevel"].AsString);
            // 游戏目录
            if (!GameInfo.SetGameData(toml["Game"]["Path"].AsString))
            {
                if (
                    (
                        MessageBoxVM.Show(
                            new(I18nRes.GameNotFound_SelectAgain)
                            {
                                Button = MessageBoxVM.Button.YesNo,
                                Icon = MessageBoxVM.Icon.Question,
                            }
                        ) is MessageBoxVM.Result.Yes
                        && GameInfo.GetGameDirectory()
                    )
                    is false
                )
                {
                    MessageBoxVM.Show(
                        new(I18nRes.GameNotFound_SoftwareExit) { Icon = MessageBoxVM.Icon.Error }
                    );
                    return;
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
        }

        private void CreateConfig()
        {
            if (
                !(
                    MessageBoxVM.Show(
                        new(I18nRes.FirstStart)
                        {
                            Button = MessageBoxVM.Button.YesNo,
                            Icon = MessageBoxVM.Icon.Question,
                        }
                    ) is MessageBoxVM.Result.Yes
                    && GameInfo.GetGameDirectory()
                )
            )
            {
                MessageBoxVM.Show(
                    new(I18nRes.GameNotFound_SoftwareExit) { Icon = MessageBoxVM.Icon.Error }
                );
                return;
            }
            CreateConfigFile();
            var toml = TOML.Parse(ST.ConfigTomlFile);
            toml["Game"]["Path"] = GameInfo.BaseDirectory;
            toml["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
            toml.SaveTo(ST.ConfigTomlFile);
        }

        /// <summary>
        /// 创建配置文件
        /// </summary>
        private void CreateConfigFile()
        {
            using StreamReader sr = ResourceDictionary.GetResourceStream(
                ResourceDictionary.Config_toml
            );
            File.WriteAllText(ST.ConfigTomlFile, sr.ReadToEnd());
            Logger.Info(
                $"{I18nRes.ConfigFileCreationCompleted} {I18nRes.Path}: {ST.ConfigTomlFile}"
            );
        }
        #endregion

        #region WindowEffect

        /// <summary>
        /// 设置窗口效果委托
        /// </summary>
        internal Action<bool> _setWindowEffectAction;

        /// <summary>
        /// 取消窗口效果委托
        /// </summary>
        internal Action _removeWindowEffectAction;

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

        private void ReminderSavePage(ListBoxItemVM vm)
        {
            if (vm.Tag is not ISTPage page)
                return;
            try
            {
                if (page.NeedSave)
                {
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

        private void SavePage(ListBoxItemVM vm)
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

        private void ClosePage(ListBoxItemVM vm)
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
    }
}
