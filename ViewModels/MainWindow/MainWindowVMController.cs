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

        private Dictionary<string, ExtensionInfo> allExtensionsInfo = new();
        private ListBoxItemVM? selectedItem;
        private ExtensionInfo? deubgItemExtensionInfo;
        private ListBoxItemVM? deubgItem;
        private string? deubgItemPath;

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
        internal void AddMainPageItem(ListBoxItemVM vm)
        {
            DetectPageItemData(ref vm);
            ListBox_MainMenu.Add(vm);
        }

        private void DetectPageItemData(ref ListBoxItemVM vm)
        {
            if (vm?.Tag is not ISTPage page)
                return;
            vm.Id = page.GetType().FullName;
            vm.Content = page.GetNameI18n();
            vm.ToolTip = page.GetDescriptionI18n();
            vm.ContextMenu = CreateItemContextMenu();
        }

        private ContextMenuVM CreateItemContextMenu() =>
            new(
                (list) =>
                {
                    list.Add(
                        new(
                            (o) =>
                            {
                                if (o is not ListBoxItemVM vm)
                                    return;
                                RefreshPage(vm);
                            }
                        )
                        {
                            Icon = "🔄",
                            Header = I18nRes.RefreshPage,
                        }
                    );
                }
            );

        private void RefreshPage(ListBoxItemVM vm)
        {
            vm.Tag = CreatePage(vm.Tag!.GetType());
            if (vm.IsSelected)
                ShowPage(vm.Tag);
        }

        private object? CreatePage(Type type)
        {
            try
            {
                return type.Assembly.CreateInstance(type.FullName!)!;
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18nRes.PageInitializeError}: {type.FullName}", ex);
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
        #region CheckGameStartOption
        private void CheckGameStartOption()
        {
            if (clearGameLogOnStart)
                ClearGameLogFile();
        }

        private void ClearGameLogFile()
        {
            if (File.Exists(GameInfo.LogFile))
                Utils.DeleteFileToRecycleBin(GameInfo.LogFile);
            File.Create(GameInfo.LogFile).Close();
            Logger.Record(I18nRes.GameLogCleanupCompleted);
        }
        #endregion
        #region ExtensionPage
        private void InitializeExtensionPages()
        {
            DirectoryInfo dirs = new(ST.ExtensionDirectories);
            foreach (var dir in dirs.GetDirectories())
            {
                if (GetExtensionInfo(dir.FullName) is ExtensionInfo extensionInfo)
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

        private void InitializeExtensionDebugPage()
        {
            // 添加拓展调试页面
            if (deubgItemExtensionInfo is not null)
            {
                deubgItem = new()
                {
                    Icon = deubgItemExtensionInfo.Icon,
                    Tag = deubgItemExtensionInfo.ExtensionPage,
                };
                AddMainPageItem(deubgItem);
            }
        }

        internal void RefreshExtensionDebugPage(string path)
        {
            var isSelected = ListBox_MainMenu.SelectedItem == deubgItem;
            ListBox_MainMenu.Remove(deubgItem!);
            InitializeExtensionDebugPage();
            if (isSelected)
                ListBox_MainMenu.SelectedItem = deubgItem;
        }

        private ExtensionInfo? GetExtensionInfo(string path, bool loadInMemory = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Logger.Record(I18nRes.ExtensionPathIsEmpty, LogLevel.WARN);
                MessageBoxVM.Show(
                    new(I18nRes.ExtensionPathIsEmpty) { Icon = MessageBoxVM.Icon.Warning }
                );
                return null;
            }
            string tomlFile = $"{path}\\{ST.ExtensionInfoFile}";
            try
            {
                // 判断文件存在性
                if (!File.Exists(tomlFile))
                {
                    Logger.Record(
                        $"{I18nRes.ExtensionTomlFileNotFound} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExtensionTomlFileNotFound}\n{I18nRes.Path}: {tomlFile}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                    return null;
                }
                var extensionInfo = new ExtensionInfo(TOML.Parse(tomlFile));
                var assemblyFile = $"{path}\\{extensionInfo.ExtensionFile}";
                // 检测是否有相同的拓展
                if (allExtensionsInfo.ContainsKey(extensionInfo.ExtensionId))
                {
                    Logger.Record(
                        $"{I18nRes.ExtensionAlreadyExists} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExtensionAlreadyExists}\n{I18nRes.Path}: {tomlFile}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                    return null;
                }
                // 判断组件文件是否存在
                if (!File.Exists(assemblyFile))
                {
                    Logger.Record(
                        $"{I18nRes.ExtensionFileError} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExtensionFileError}\n{I18nRes.Path}: {tomlFile}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                    return null;
                }
                // 从内存或外部载入
                if (loadInMemory)
                {
                    var bytes = File.ReadAllBytes(assemblyFile);
                    var type = Assembly.Load(bytes).GetType(extensionInfo.ExtensionId)!;
                    extensionInfo.ExtensionPage = type.Assembly.CreateInstance(type.FullName!)!;
                }
                else
                {
                    var type = Assembly.LoadFrom(assemblyFile).GetType(extensionInfo.ExtensionId)!;
                    extensionInfo.ExtensionPage = type.Assembly.CreateInstance(type.FullName!)!;
                }
                // 判断是否成功创建了页面
                if (extensionInfo.ExtensionPage is null)
                {
                    Logger.Record(
                        $"{I18nRes.ExtensionIdError} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExtensionIdError}\n{I18nRes.Path}: {tomlFile}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                    return null;
                }
                // 判断页面是否实现了接口
                if (extensionInfo.ExtensionPage is not ISTPage)
                {
                    Logger.Record(
                        $"{I18nRes.ExtensionIdError} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExtensionIdError}\n{I18nRes.Path}: {tomlFile}")
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
                Logger.Record($"{I18nRes.ExtensionLoadError} {I18nRes.Path}: {tomlFile}", ex);
                MessageBoxVM.Show(
                    new($"{I18nRes.ExtensionLoadError}\n{I18nRes.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Error
                    }
                );
                return null;
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
                Logger.Record($"{I18nRes.ConfigFileError} {I18nRes.Path}: {ST.ConfigTomlFile}", ex);
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
                && GetExtensionInfo(debugPath, true) is ExtensionInfo info
            )
            {
                deubgItemExtensionInfo = info;
                deubgItemPath = debugPath;
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
            Logger.Record(
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
                Logger.Record($"{I18nRes.PageSaveError} {type.FullName}", ex);
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
                Logger.Record($"{I18nRes.PageSaveError} {type.FullName}", ex);
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
                Logger.Record($"{I18nRes.PageCloseError} {type.FullName}", ex);
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
