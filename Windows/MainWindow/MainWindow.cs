using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HKW.TomlParse;
using Panuon.WPF.UI;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Pages;
using StarsectorTools.Tools.GameSettings;
using StarsectorTools.Tools.ModManager;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows.MainWindow
{
    public partial class MainWindow
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;
        /// <summary>拓展目录</summary>
        private const string expansionDirectories = "Expansion";

        /// <summary>拓展信息文件</summary>
        private const string expansionInfoFile = "Expansion.toml";

        /// <summary>StarsectorTools配置文件资源链接</summary>
        private static readonly Uri resourcesConfigUri = new("/Resources/Config.toml", UriKind.Relative);

        private const string strName = "Name";
        private const string strDescription = "Description";
        private bool menuOpen = false;
        private Dictionary<string, Page> pages = new();
        private Dictionary<string, Lazy<Page>> expansionPages = new();
        private Dictionary<string, ExpansionInfo> allExpansionsInfo = new();
        private Settings settingsPage = null!;
        private Info infoPage = null!;
        private int pageSelectedIndex = -1;
        private int exceptionPageSelectedIndex = -1;
        private bool clearGameLogOnStart = false;

        /// <summary>拓展信息</summary>
        private class ExpansionInfo
        {
            /// <summary>ID</summary>
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

            public Type ExpansionType = null!;

            public ExpansionInfo(TomlTable table)
            {
                foreach (var info in table)
                    SetInfo(info.Key, info.Value.AsString);
            }

            public void SetInfo(string key, string value)
            {
                switch (key)
                {
                    case nameof(Id): Id = value; break;
                    case nameof(Name): Name = value; break;
                    case nameof(Author): Author = value; break;
                    case nameof(Icon): Icon = value; break;
                    case nameof(Version): Version = value; break;
                    case nameof(ToolsVersion): ToolsVersion = value; break;
                    case nameof(Description): Description = value; break;
                    case nameof(ExpansionId): ExpansionId = value; break;
                    case nameof(ExpansionFile): ExpansionFile = value; break;
                }
            }
        }

        private void InitializeDirectories()
        {
            if (!Utils.DirectoryExists(ST.CoreDirectory, false))
                Directory.CreateDirectory(ST.CoreDirectory);
            if (!Utils.DirectoryExists(expansionDirectories, false))
                Directory.CreateDirectory(expansionDirectories);
        }

        private void SetSettingsPage()
        {
            try
            {
                settingsPage = new();
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.PageInitializeError}: {nameof(Settings)}", ex);
                Utils.ShowMessageBox($"{I18n.PageInitializeError}:\n{nameof(Settings)}", STMessageBoxIcon.Error);
            }
        }

        private void SetInfoPage()
        {
            try
            {
                infoPage = new();
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.PageInitializeError}: {nameof(Info)}", ex);
                Utils.ShowMessageBox($"{I18n.PageInitializeError}:\n{nameof(Info)}", STMessageBoxIcon.Error);
            }
        }

        private bool SetConfig()
        {
            try
            {
                if (Utils.FileExists(ST.STConfigTomlFile, false))
                {
                    // 读取设置
                    TomlTable toml = TOML.Parse(ST.STConfigTomlFile);
                    // 语言
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    // 日志等级
                    STLog.SetLogLevel(STLog.Str2STLogLevel(toml["Extras"]["LogLevel"].AsString));
                    // 游戏目录
                    if (!GameInfo.SetGameData(toml["Game"]["Path"].AsString!))
                    {
                        if (!(Utils.ShowMessageBox(I18n.GameNotFound_SelectAgain, MessageBoxButton.YesNo, STMessageBoxIcon.Question) == MessageBoxResult.Yes && GameInfo.GetGameDirectory()))
                        {
                            Utils.ShowMessageBox(I18n.GameNotFound_SoftwareExit, STMessageBoxIcon.Error);
                            return false;
                        }
                        toml["Game"]["Path"] = GameInfo.BaseDirectory;
                    }
                    // 拓展调试目录
                    string filePath = toml["Expansion"]["DebugPath"].AsString;
                    if (!string.IsNullOrEmpty(filePath) && CheckExpansionInfo(filePath, true) is ExpansionInfo info)
                    {
                        ST.ExpansionDebugPath = filePath;
                        ST.ExpansionDebugId = info.Id;
                    }
                    else
                        toml["Expansion"]["DebugPath"] = "";
                    clearGameLogOnStart = toml["Game"]["ClearLogOnStart"].AsBoolean;
                    toml.SaveTo(ST.STConfigTomlFile);
                }
                else
                {
                    if (!(Utils.ShowMessageBox(I18n.FirstStart, MessageBoxButton.YesNo, STMessageBoxIcon.Question) == MessageBoxResult.Yes && GameInfo.GetGameDirectory()))
                    {
                        Utils.ShowMessageBox(I18n.GameNotFound_SoftwareExit, STMessageBoxIcon.Error);
                        return false;
                    }
                    CreateConfigFile();
                    TomlTable toml = TOML.Parse(ST.STConfigTomlFile);
                    toml["Game"]["Path"] = GameInfo.BaseDirectory;
                    toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                    toml.SaveTo(ST.STConfigTomlFile);
                }
            }
            catch (Exception ex)
            {
                ResetConfigFile(ex);
            }
            return true;
        }

        /// <summary>
        /// 创建配置文件
        /// </summary>
        private void CreateConfigFile()
        {
            using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
            File.WriteAllText(ST.STConfigTomlFile, sr.ReadToEnd());
            STLog.WriteLine($"{I18n.ConfigFileCreationCompleted} {I18n.Path}: {ST.STConfigTomlFile}");
        }

        /// <summary>
        /// 重置配置文件
        /// </summary>
        /// <param name="ex">异常</param>
        private void ResetConfigFile(Exception? ex = null)
        {
            if (ex is not null)
            {
                STLog.WriteLine($"{I18n.ConfigFileError} {I18n.Path}: {ST.STConfigTomlFile}", ex);
                Utils.ShowMessageBox($"{I18n.ConfigFileError}\n{I18n.Path}: {ST.STConfigTomlFile}", STMessageBoxIcon.Error);
            }
            CreateConfigFile();
        }

        internal void SetBlurEffect()
        {
            Dispatcher.Invoke(() => Effect = new System.Windows.Media.Effects.BlurEffect());
        }

        internal void RemoveBlurEffect()
        {
            Dispatcher.Invoke(() => Effect = null);
        }

        internal void ChangeLanguage()
        {
            STLog.WriteLine($"{I18n.DIsplayLanguageIs} {Thread.CurrentThread.CurrentUICulture.Name}");
            Label_Title.Content = I18n.StarsectorTools;
            Button_SettingsPage.Content = I18n.Settings;
            Button_InfoPage.Content = I18n.Info;
            RefreshPages();
            RefreshExpansionPages();
            STLog.WriteLine(I18n.PageListRefreshComplete);
        }

        private void ShowPage()
        {
            if (string.IsNullOrEmpty(ST.ExpansionDebugPath))
            {
                ListBox_MainMenu.SelectedIndex = 0;
            }
            else if (CheckExpansionInfo(ST.ExpansionDebugPath, true) is ExpansionInfo info)
            {
                ListBox_MainMenu.SelectedItem = AddExpansionDebugPage(info);
            }
        }

        private void RefreshPages()
        {
            ClearPages();
            AddPage("🌐", I18n.ModManager, nameof(ModManager), I18n.ModManagerToolTip, CreatePage(typeof(ModManager)));
            AddPage("⚙", I18n.GameSettings, nameof(GameSettings), I18n.GameSettingsToolTip, CreatePage(typeof(GameSettings)));
        }

        private Page? CreatePage(Type type)
        {
            try
            {
                return (Page)type.Assembly.CreateInstance(type.FullName!)!;
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.PageInitializeError}: {type.FullName}", ex);
                Utils.ShowMessageBox($"{I18n.PageInitializeError}:\n{type.FullName}", STMessageBoxIcon.Error);
                return null;
            }
        }

        private void ClearPages()
        {
            foreach (var page in pages.Values)
                ClosePage(page);
            pages.Clear();
            while (ListBox_MainMenu.Items.Count > 1)
                ListBox_MainMenu.Items.RemoveAt(0);
        }

        private void ClosePage(Page? page)
        {
            if (page is null)
                return;
            // 获取page中的Close方法并执行
            // 用于关闭page中创建的线程
            try
            {
                if (page.GetType().GetMethod("Close") is MethodInfo info)
                    _ = info.Invoke(page, null);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.PageCloseError} {page.GetType().FullName}", ex);
                Utils.ShowMessageBox($"{I18n.PageCloseError} {page.GetType().FullName}\n{STLog.SimplifyException(ex)}", STMessageBoxIcon.Error);
            }
        }

        private void SaveAllPages()
        {
            SavePages();
            SaveExpansionPages();
        }

        private void SavePages()
        {
            foreach (var page in pages.Values)
                SavePage(page);
        }

        private void SaveExpansionPages()
        {
            foreach (var lazyPage in expansionPages.Values)
                if (lazyPage.IsValueCreated)
                    SavePage(lazyPage.Value);
        }

        private void SavePage(Page? page)
        {
            if (page is null)
                return;
            // 获取page中的Save方法并执行
            // 用于保存page中已修改的数据
            try
            {
                if (page.GetType().GetMethod("Save") is MethodInfo info)
                    _ = info.Invoke(page, null);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.PageSaveError} {page.GetType().FullName}", ex);
                Utils.ShowMessageBox($"{I18n.PageSaveError} {page.GetType().FullName}\n{STLog.SimplifyException(ex)}", STMessageBoxIcon.Error);
            }
        }

        private ListBoxItem CreateListBoxItemForPage(string icon, string name, string id, string toolTip)
        {
            var item = new ListBoxItem
            {
                Content = name,
                ToolTip = toolTip,
                Tag = id,
                Style = (Style)Application.Current.Resources["ListBoxItem_Style"]
            };
            ListBoxItemHelper.SetIcon(item, new Emoji.Wpf.TextBlock() { Text = icon });
            return item;
        }

        private void AddPage(string icon, string name, string id, string toolTip, Page? page)
        {
            if (page is null)
                return;
            var item = CreateListBoxItemForPage(icon, name, id, toolTip);
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            // 重载当前菜单
            MenuItem menuItem = new();
            menuItem.Header = I18n.RefreshPage;
            menuItem.Icon = new Emoji.Wpf.TextBlock() { Text = "🔄" };
            menuItem.Click += (s, e) =>
            {
                ClosePage(pages[id]);
                if (CreatePage(pages[id].GetType()) is not Page newPage)
                    return;
                pages[id] = newPage;
                if (Frame_MainFrame.Content is Page oldPage && oldPage.GetType() == newPage.GetType())
                    Frame_MainFrame.Content = pages[id];
                STLog.WriteLine($"{I18n.RefreshPage}: {id}");
                GC.Collect();
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBox_MainMenu.Items.Insert(ListBox_MainMenu.Items.Count - 1, item);
            pages.Add(id, page);
            STLog.WriteLine($"{I18n.AddPage} {icon} {name}");
        }

        private void RefreshExpansionPages()
        {
            ClearExpansionPages();
            GetAllExpansions();
        }

        private void ClearExpansionPages()
        {
            foreach (var lazyPage in expansionPages.Values)
                if (lazyPage.IsValueCreated)
                    ClosePage(lazyPage.Value);
            expansionPages.Clear();
            allExpansionsInfo.Clear();
            ListBox_ExpansionMenu.Items.Clear();
        }

        private void GetAllExpansions()
        {
            DirectoryInfo dirs = new(expansionDirectories);
            foreach (var dir in dirs.GetDirectories())
                if (CheckExpansionInfo(dir.FullName) is ExpansionInfo expansionInfo)
                    GetExpansionPage(expansionInfo);
        }

        private ExpansionInfo? CheckExpansionInfo(string directory, bool loadInMemory = false)
        {
            if (string.IsNullOrEmpty(directory))
            {
                STLog.WriteLine(I18n.ExpansionPathIsEmpty, STLogLevel.WARN);
                Utils.ShowMessageBox(I18n.ExpansionPathIsEmpty, STMessageBoxIcon.Warning);
                return null;
            }
            string tomlFile = $"{directory}\\{expansionInfoFile}";
            try
            {
                if (!Utils.FileExists(tomlFile, false))
                {
                    STLog.WriteLine($"{I18n.ExpansionTomlFileNotFound} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    Utils.ShowMessageBox($"{I18n.ExpansionTomlFileNotFound}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Warning);
                    return null;
                }
                var expansionInfo = new ExpansionInfo(TOML.Parse(tomlFile));
                var assemblyFile = $"{directory}\\{expansionInfo.ExpansionFile}";
                if (allExpansionsInfo.ContainsKey(expansionInfo.ExpansionId))
                {
                    STLog.WriteLine($"{I18n.ExpansionAlreadyExists} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    Utils.ShowMessageBox($"{I18n.ExpansionAlreadyExists}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Warning);
                    return null;
                }
                if (!Utils.FileExists(assemblyFile, false))
                {
                    STLog.WriteLine($"{I18n.ExpansionFileError} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    Utils.ShowMessageBox($"{I18n.ExpansionFileError}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Warning);
                    return null;
                }
                if (loadInMemory)
                {
                    var bytes = File.ReadAllBytes(assemblyFile);
                    expansionInfo.ExpansionType = Assembly.Load(bytes).GetType(expansionInfo.ExpansionId)!;
                }
                else
                    expansionInfo.ExpansionType = Assembly.LoadFrom(assemblyFile).GetType(expansionInfo.ExpansionId)!;
                if (expansionInfo.ExpansionType is null)
                {
                    STLog.WriteLine($"{I18n.ExpansionIdError} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    Utils.ShowMessageBox($"{I18n.ExpansionIdError}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Warning);
                    return null;
                }
                return expansionInfo;
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.ExpansionLoadError} {I18n.Path}: {tomlFile}", ex);
                Utils.ShowMessageBox($"{I18n.ExpansionLoadError}\n{I18n.Path}: {tomlFile}", STMessageBoxIcon.Error);
                return null;
            }
        }

        private void GetExpansionPage(ExpansionInfo expansionInfo)
        {
            allExpansionsInfo.Add(expansionInfo.Id, expansionInfo);
            AddExpansionPage(expansionInfo);
        }

        private void AddExpansionPage(ExpansionInfo expansionInfo)
        {
            string icon = expansionInfo.Icon;
            string name = expansionInfo.Name;
            string id = expansionInfo.Id;
            string toolTip = expansionInfo.Description;
            Lazy<Page> lazyPage = new(() => (Page)expansionInfo.ExpansionType.Assembly.CreateInstance(expansionInfo.ExpansionType.FullName!)!);
            var item = CreateListBoxItemForPage(icon, name, id, toolTip);
            if (expansionInfo.Version != ST.Version)
            {
                item.Background = (Brush)Application.Current.Resources["ColorYellow2"];
                item.ToolTip = $"{I18n.IncompatibleExpansion}\n\n{item.ToolTip}";
            }
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            // 重载当前菜单
            MenuItem menuItem = new();
            menuItem.Header = I18n.RefreshPage;
            menuItem.Icon = new Emoji.Wpf.TextBlock() { Text = "🔄" };
            menuItem.Click += (s, e) =>
            {
                Type type = allExpansionsInfo[id].ExpansionType;
                ClosePage(expansionPages[id].Value);
                expansionPages[id] = new(() => (Page)type.Assembly.CreateInstance(type.FullName!)!);
                if (Frame_MainFrame.Content is Page _page && _page.GetType() == type)
                    Frame_MainFrame.Content = expansionPages[id].Value;
                STLog.WriteLine($"{I18n.RefreshPage}: {id}");
                GC.Collect();
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBox_ExpansionMenu.Items.Add(item);
            expansionPages.Add(id, lazyPage);
            STLog.WriteLine($"{I18n.AddExpansionPage} {icon} {name}");
        }

        internal void RefreshDebugExpansion()
        {
            if (!string.IsNullOrEmpty(ST.ExpansionDebugPath = settingsPage.TextBox_ExpansionDebugPath.Text))
            {
                if (CheckExpansionInfo(ST.ExpansionDebugPath, true) is ExpansionInfo info)
                {
                    ST.ExpansionDebugId = info.Id;
                    if (pages.ContainsKey(info.Id))
                        RemovePage(info.Id);
                    ListBox_MainMenu.SelectedItem = AddExpansionDebugPage(info);
                }
            }
            else if (!string.IsNullOrEmpty(ST.ExpansionDebugId))
            {
                if (ListBox_MainMenu.SelectedItem is ListBoxItem item && item.Tag.ToString() == ST.ExpansionDebugId)
                    Frame_MainFrame.Content = null;
                RemovePage(ST.ExpansionDebugId);
                ST.ExpansionDebugId = string.Empty;
            }

        }
        private void RemovePage(string pageId)
        {
            if (pages.ContainsKey(pageId))
            {
                ClosePage(pages[pageId]);
                for (int i = 0; i < ListBox_MainMenu.Items.Count; i++)
                    if (((ListBoxItem)ListBox_MainMenu.Items[i]).Tag.ToString() == pageId)
                        ListBox_MainMenu.Items.RemoveAt(i);
                pages.Remove(pageId);
            }
        }

        private ListBoxItem AddExpansionDebugPage(ExpansionInfo expansionInfo)
        {
            string icon = expansionInfo.Icon;
            string name = expansionInfo.Name;
            string id = expansionInfo.Id;
            string toolTip = expansionInfo.Description;
            var item = CreateListBoxItemForPage(icon, name, id, toolTip);
            if (expansionInfo.Version != ST.Version)
            {
                item.Background = (Brush)Application.Current.Resources["ColorYellow2"];
                item.ToolTip = $"{I18n.IncompatibleExpansion}\n\n{item.ToolTip}";
            }
            Page page = CreatePage(expansionInfo.ExpansionType)!;
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            // 重载当前菜单
            MenuItem menuItem = new();
            menuItem.Header = I18n.RefreshPage;
            menuItem.Icon = new Emoji.Wpf.TextBlock() { Text = "🔄" };
            menuItem.Click += (s, e) =>
            {
                RefreshDebugExpansion();
                GC.Collect();
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBox_MainMenu.Items.Insert(ListBox_MainMenu.Items.Count - 1, item);
            pages.Add(id, page);
            STLog.WriteLine($"{I18n.RefreshPage} {icon} {name}");
            return item;
        }
        private void ButtonPageCancelPress()
        {
            Button_SettingsPage.Tag = false;
            Button_InfoPage.Tag = false;
        }

        private void ClearGameLogFile()
        {
            if (Utils.FileExists(GameInfo.LogFile, false))
                Utils.DeleteFileToRecycleBin(GameInfo.LogFile);
            File.Create(GameInfo.LogFile).Close();
            STLog.WriteLine(I18n.GameLogCleanupCompleted);
        }
    }
}