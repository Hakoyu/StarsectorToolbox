﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using HKW.Libs.Log4Cs;
using HKW.Libs.TomlParse;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialog;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18nRes = StarsectorTools.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorTools.Windows.MainWindow
{
    internal partial class MainWindowViewModel
    {
        /// <summary>
        /// 单例化
        /// </summary>
        public static MainWindowViewModel Instance { get; private set; } = null!;

        private Dictionary<string, ExpansionInfo> allExpansionsInfo = new();
        private ListBoxItemVM? selectedItem;
        private ExpansionInfo? deubgItemExpansionInfo;
        private ListBoxItemVM? deubgItem;
        private string? deubgItemPath;

        /// <summary>拓展信息</summary>
        private class ExpansionInfo
        {
            /// <summary>Id</summary>
            public string Id { get; private set; } = null!;

            /// <summary>名称</summary>
            public string Name { get; private set; } = null!;

            /// <summary>作者</summary>
            public string Author { get; private set; } = null!;

            /// <summary>图标</summary>
            public string Icon { get; private set; } = null!;

            /// <summary>版本</summary>
            public string Version { get; private set; } = null!;

            /// <summary>支持的工具箱版本</summary>
            public string ToolsVersion { get; private set; } = null!;

            /// <summary>描述</summary>
            public string Description { get; private set; } = null!;

            /// <summary>拓展Id</summary>
            public string ExpansionId { get; private set; } = null!;

            /// <summary>拓展文件</summary>
            public string ExpansionFile { get; private set; } = null!;

            /// <summary>拓展页面</summary>
            public object ExpansionPage { get; set; } = null!;

            public ExpansionInfo(TomlTable table)
            {
                foreach (var info in table)
                    SetInfo(info.Key, info.Value.AsString);
            }

            public void SetInfo(string key, string value)
            {
                switch (key)
                {
                    case nameof(Id):
                        Id = value;
                        break;

                    case nameof(Name):
                        Name = value;
                        break;

                    case nameof(Author):
                        Author = value;
                        break;

                    case nameof(Icon):
                        Icon = value;
                        break;

                    case nameof(Version):
                        Version = value;
                        break;

                    case nameof(ToolsVersion):
                        ToolsVersion = value;
                        break;

                    case nameof(Description):
                        Description = value;
                        break;

                    case nameof(ExpansionId):
                        ExpansionId = value;
                        break;

                    case nameof(ExpansionFile):
                        ExpansionFile = value;
                        break;
                }
            }
        }

        internal void Close()
        {
            CloseAllPages();
            ReminderSaveAllPages();
        }

        internal void AddMainPageItem(ListBoxItemVM vm)
        {
            DetectPageItemData(ref vm);
            MainListBox.Add(vm);
        }

        private void DetectPageItemData(ref ListBoxItemVM vm)
        {
            if (vm?.Tag is not ISTPage page)
                return;
            vm.Id = page.GetType().FullName;
            vm.Content = page.NameI18n;
            vm.ToolTip = page.DescriptionI18n;
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

        private void InitializeDirectories()
        {
            if (!Directory.Exists(ST.CoreDirectory))
                Directory.CreateDirectory(ST.CoreDirectory);
            if (!Directory.Exists(ST.ExpansionDirectories))
                Directory.CreateDirectory(ST.ExpansionDirectories);
        }

        private bool SetConfig(string originalConfigData)
        {
            try
            {
                if (Utils.FileExists(ST.ConfigTomlFile, false))
                {
                    // 读取设置
                    var toml = TOML.Parse(ST.ConfigTomlFile);
                    // 语言
                    var cultureInfo = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    if (Thread.CurrentThread.CurrentUICulture.Name != cultureInfo.Name)
                    {
                        Thread.CurrentThread.CurrentUICulture = cultureInfo;
                        ChangeLanguage();
                    }
                    // 日志等级
                    Logger.Options.DefaultLevel = Logger.LogLevelConverter(
                        toml["Extras"]["LogLevel"].AsString
                    );
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
                            ) is false
                        )
                        {
                            MessageBoxVM.Show(
                                new(I18nRes.GameNotFound_SoftwareExit)
                                {
                                    Icon = MessageBoxVM.Icon.Error
                                }
                            );
                            return false;
                        }
                        toml["Game"]["Path"] = GameInfo.BaseDirectory;
                    }
                    // 拓展调试目录
                    string debugPath = toml["Expansion"]["DebugPath"].AsString;
                    if (
                        !string.IsNullOrEmpty(debugPath)
                        && GetExpansionInfo(debugPath, true) is ExpansionInfo info
                    )
                    {
                        deubgItemExpansionInfo = info;
                        deubgItemPath = debugPath;
                    }
                    else
                        toml["Expansion"]["DebugPath"] = "";
                    ClearGameLogOnStart = toml["Game"]["ClearLogOnStart"].AsBoolean;
                    toml.SaveTo(ST.ConfigTomlFile);
                }
                else
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
                        return false;
                    }
                    CreateConfigFile(originalConfigData);
                    var toml = TOML.Parse(ST.ConfigTomlFile);
                    toml["Game"]["Path"] = GameInfo.BaseDirectory;
                    toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                    toml.SaveTo(ST.ConfigTomlFile);
                }
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
                CreateConfigFile(originalConfigData);
            }
            return true;
        }

        /// <summary>
        /// 创建配置文件
        /// </summary>
        private void CreateConfigFile(string configData)
        {
            File.WriteAllText(ST.ConfigTomlFile, configData);
            Logger.Record($"{I18nRes.ConfigFileCreationCompleted} {I18nRes.Path}: {ST.ConfigTomlFile}");
        }

        private ExpansionInfo? GetExpansionInfo(string path, bool loadInMemory = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                Logger.Record(I18nRes.ExpansionPathIsEmpty, LogLevel.WARN);
                MessageBoxVM.Show(
                    new(I18nRes.ExpansionPathIsEmpty) { Icon = MessageBoxVM.Icon.Warning }
                );
                return null;
            }
            string tomlFile = $"{path}\\{ST.ExpansionInfoFile}";
            try
            {
                // 判断文件存在性
                if (!File.Exists(tomlFile))
                {
                    Logger.Record(
                        $"{I18nRes.ExpansionTomlFileNotFound} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExpansionTomlFileNotFound}\n{I18nRes.Path}: {tomlFile}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                    return null;
                }
                var expansionInfo = new ExpansionInfo(TOML.Parse(tomlFile));
                var assemblyFile = $"{path}\\{expansionInfo.ExpansionFile}";
                // 检测是否有相同的拓展
                if (allExpansionsInfo.ContainsKey(expansionInfo.ExpansionId))
                {
                    Logger.Record(
                        $"{I18nRes.ExpansionAlreadyExists} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExpansionAlreadyExists}\n{I18nRes.Path}: {tomlFile}")
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
                        $"{I18nRes.ExpansionFileError} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExpansionFileError}\n{I18nRes.Path}: {tomlFile}")
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
                    var type = Assembly.Load(bytes).GetType(expansionInfo.ExpansionId)!;
                    expansionInfo.ExpansionPage = type.Assembly.CreateInstance(type.FullName!)!;
                }
                else
                {
                    var type = Assembly.LoadFrom(assemblyFile).GetType(expansionInfo.ExpansionId)!;
                    expansionInfo.ExpansionPage = type.Assembly.CreateInstance(type.FullName!)!;
                }
                // 判断是否成功创建了页面
                if (expansionInfo.ExpansionPage is null)
                {
                    Logger.Record(
                        $"{I18nRes.ExpansionIdError} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExpansionIdError}\n{I18nRes.Path}: {tomlFile}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                    return null;
                }
                // 判断页面是否实现了接口
                if (expansionInfo.ExpansionPage is not ISTPage)
                {
                    Logger.Record(
                        $"{I18nRes.ExpansionIdError} {I18nRes.Path}: {tomlFile}",
                        LogLevel.WARN
                    );
                    MessageBoxVM.Show(
                        new($"{I18nRes.ExpansionIdError}\n{I18nRes.Path}: {tomlFile}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                    return null;
                }
                return expansionInfo;
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18nRes.ExpansionLoadError} {I18nRes.Path}: {tomlFile}", ex);
                MessageBoxVM.Show(
                    new($"{I18nRes.ExpansionLoadError}\n{I18nRes.Path}: {tomlFile}")
                    {
                        Icon = MessageBoxVM.Icon.Error
                    }
                );
                return null;
            }
        }

        internal void ChangeLanguage(bool changePages = false)
        {
            Logger.Record($"{I18nRes.DisplayLanguageIs} {Thread.CurrentThread.CurrentUICulture.Name}");
            //if (changePages)
            //    ChangeLanguageToAllPages();
            var toml = TOML.Parse(ST.ConfigTomlFile);
            toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
            toml.SaveTo(ST.ConfigTomlFile);
        }

        private void InitializeExpansionPages()
        {
            DirectoryInfo dirs = new(ST.ExpansionDirectories);
            foreach (var dir in dirs.GetDirectories())
            {
                if (GetExpansionInfo(dir.FullName) is ExpansionInfo expansionInfo)
                {
                    var page = expansionInfo.ExpansionPage;
                    ExpansionListBox.Add(
                        new()
                        {
                            Id = expansionInfo.Id,
                            Icon = expansionInfo.Icon,
                            Content = expansionInfo.Name,
                            ToolTip =
                                $"Author: {expansionInfo.Author}\nDescription: {expansionInfo.Description}",
                            Tag = page
                        }
                    );
                }
            }
        }

        private void InitializeExpansionDebugPage()
        {
            // 添加拓展调试页面
            if (deubgItemExpansionInfo is not null)
            {
                deubgItem = new()
                {
                    Icon = deubgItemExpansionInfo.Icon,
                    Tag = deubgItemExpansionInfo.ExpansionPage,
                };
                AddMainPageItem(deubgItem);
            }
        }

        internal void RefreshExpansionDebugPage(string path)
        {
            var isSelected = MainListBox.SelectedItem == deubgItem;
            MainListBox.Remove(deubgItem);
            InitializeExpansionDebugPage();
            if (isSelected)
                MainListBox.SelectedItem = deubgItem;
        }
        #region WindowEffect

        internal void RegisterChangeWindowEffectEvent(
            ChangeWindowEffectHandler setHandler,
            ChangeWindowEffectHandler removeHandler
        )
        {
            SetWindowEffectEvent += setHandler;
            RemoveWindowEffectEvent += removeHandler;
        }

        internal void SetBlurEffect()
        {
            if (SetWindowEffectEvent is not null)
                SetWindowEffectEvent();
        }

        internal void RemoveBlurEffect()
        {
            if (RemoveWindowEffectEvent is not null)
                RemoveWindowEffectEvent();
        }
        #endregion

        #region ChangeLanguageToPage
        private void ChangeLanguageToAllPages()
        {
            ChangeLanguageToMainPages();
            ChangeLanguageToExpansionPages();
        }

        private void ChangeLanguageToMainPages()
        {
            foreach (var item in MainListBox)
                ChangeLanguageToPage(item);
        }

        private void ChangeLanguageToExpansionPages()
        {
            foreach (var item in ExpansionListBox)
                ChangeLanguageToPage(item);
        }

        private void ChangeLanguageToPage(ListBoxItemVM vm)
        {
            if (vm.Tag is not ISTPage page)
                return;
            try
            {
                if (page.I18nSet.Contains(Thread.CurrentThread.CurrentCulture.Name))
                    if (page.ChangeLanguage() is false)
                        RefreshPage(vm);
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
        #endregion

        #region ReminderSavePage

        private void ReminderSaveAllPages()
        {
            ReminderSaveMainPages();
            TrySaveExpansionPages();
        }

        private void ReminderSaveMainPages()
        {
            foreach (var item in MainListBox)
                ReminderSavePage(item);
        }

        private void TrySaveExpansionPages()
        {
            foreach (var item in ExpansionListBox)
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

        #endregion CheckPageSave

        #region SavePage

        private void SaveAllPages()
        {
            SaveMainPages();
            SaveExpansionPages();
        }

        private void SaveMainPages()
        {
            foreach (var item in MainListBox)
                SavePage(item);
        }

        private void SaveExpansionPages()
        {
            foreach (var item in ExpansionListBox)
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
            CloseExpansionPages();
        }

        private void CloseMainPages()
        {
            foreach (var page in MainListBox)
                ClosePage(page);
        }

        private void CloseExpansionPages()
        {
            foreach (var page in ExpansionListBox)
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

        /// <summary>
        /// 改变窗口效果委托
        /// </summary>
        internal delegate void ChangeWindowEffectHandler();

        /// <summary>
        /// 设置窗口效果事件
        /// </summary>
        internal event ChangeWindowEffectHandler? SetWindowEffectEvent;

        /// <summary>
        /// 取消窗口效果事件
        /// </summary>
        internal event ChangeWindowEffectHandler? RemoveWindowEffectEvent;
    }
}