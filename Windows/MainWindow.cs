using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HKW.TomlParse;
using Panuon.WPF.UI;
using StarsectorTools.Pages;
using StarsectorTools.Tools.GameSettings;
using StarsectorTools.Tools.ModManager;
using StarsectorTools.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows
{
    public partial class MainWindow
    {
        /// <summary>StarsectorTools配置文件资源链接</summary>
        public static readonly Uri resourcesConfigUri = new("/Resources/Config.toml", UriKind.Relative);

        /// <summary>拓展目录</summary>
        private const string expansionDirectories = "Expansion";

        /// <summary>拓展信息文件</summary>
        private const string expansionInfoFile = "Expansion.toml";

        private const string strName = "Name";
        private const string strDescription = "Description";
        private bool menuOpen = false;
        private Dictionary<string, Page> pages = new();
        private Dictionary<string, Lazy<Page>> expansionPages = new();
        private Dictionary<string, ExpansionInfo> allExpansionsInfo = new();
        private Settings settingsPage = null!;
        private Info infoPage = null!;
        private int menuSelectedIndex = -1;
        private int exceptionMenuSelectedIndex = -1;
        private string expansionDebugPath = string.Empty;

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
            if (!Directory.Exists(ST.coreDirectory))
                Directory.CreateDirectory(ST.coreDirectory);
            if (!Directory.Exists(expansionDirectories))
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
                STLog.WriteLine($"{I18n.PageInitializationError}: {nameof(Settings)}", ex);
                ST.ShowMessageBox($"{I18n.PageInitializationError}:\n{nameof(Settings)}", MessageBoxImage.Error);
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
                STLog.WriteLine($"{I18n.PageInitializationError}: {nameof(Info)}", ex);
                ST.ShowMessageBox($"{I18n.PageInitializationError}:\n{nameof(Info)}", MessageBoxImage.Error);
            }
        }

        private bool SetConfig()
        {
            try
            {
                if (File.Exists(ST.configFile))
                {
                    TomlTable toml = TOML.Parse(ST.configFile);
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    STLog.LogLevel = STLog.Str2STLogLevel(toml["Extras"]["LogLevel"].AsString);
                    ST.SetGameData(toml["Game"]["GamePath"].AsString!);
                    if (!File.Exists(ST.gameExeFile))
                    {
                        if (!(ST.ShowMessageBox(I18n.GameNotFound_SelectAgain, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGameDirectory()))
                        {
                            ST.ShowMessageBox(I18n.GameNotFound_SoftwareExit, MessageBoxImage.Error);
                            return false;
                        }
                        toml["Game"]["GamePath"] = ST.gameDirectory;
                    }
                    string filePath = toml["Expansion"]["DebugPath"].AsString;
                    if (!string.IsNullOrEmpty(filePath) && CheckExpansionInfo(filePath, true) is ExpansionInfo)
                    {
                        expansionDebugPath = filePath;
                        settingsPage.SetExpansionDebugPath(filePath);
                    }
                    else
                        toml["Expansion"]["DebugPath"] = "";
                    toml.SaveTo(ST.configFile);
                }
                else
                {
                    if (!(ST.ShowMessageBox(I18n.FirstStart, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGameDirectory()))
                    {
                        ST.ShowMessageBox(I18n.GameNotFound_SoftwareExit, MessageBoxImage.Error);
                        return false;
                    }
                    CreateConfigFile();
                    TomlTable toml = TOML.Parse(ST.configFile);
                    toml["Game"]["GamePath"] = ST.gameDirectory;
                    toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                    toml.SaveTo(ST.configFile);
                }
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.ConfigFileError} {I18n.Path}: {ST.configFile}", ex);
                ST.ShowMessageBox($"{I18n.ConfigFileError}\n{I18n.Path}: {ST.configFile}", MessageBoxImage.Error);
                CreateConfigFile();
            }
            return true;
        }

        /// <summary>
        /// 从资源中读取默认配置文件并创建
        /// </summary>
        private void CreateConfigFile()
        {
            if (File.Exists(ST.configFile))
                File.Delete(ST.configFile);
            using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
            string str = sr.ReadToEnd();
            File.WriteAllText(ST.configFile, str);
            STLog.WriteLine($"{I18n.ConfigFileCreatedSuccess} {ST.configFile}");
        }

        public void SetBlurEffect()
        {
            Dispatcher.Invoke(() => Effect = new System.Windows.Media.Effects.BlurEffect());
        }

        public void RemoveBlurEffect()
        {
            Dispatcher.Invoke(() => Effect = null);
        }

        public void ChangeLanguage()
        {
            STLog.WriteLine($"{I18n.DIsplayLanguageIs} {Thread.CurrentThread.CurrentUICulture.Name}");
            Label_Title.Content = I18n.StarsectorTools;
            Button_Settings.Content = I18n.Settings;
            Button_Info.Content = I18n.Info;
            RefreshPages();
            RefreshExpansionPages();
            STLog.WriteLine(I18n.PageListRefreshComplete);
        }

        private void ShowPage()
        {
            if (string.IsNullOrEmpty(expansionDebugPath))
            {
                ListBox_MainMenu.SelectedIndex = 0;
            }
            else if (CheckExpansionInfo(expansionDebugPath, true) is ExpansionInfo info)
            {
                AddExpansionDebugPage(info);
                ListBox_MainMenu.SelectedIndex = ListBox_MainMenu.Items.Count - 2;
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
                STLog.WriteLine($"{I18n.PageInitializationError}: {type.Name}", ex);
                ST.ShowMessageBox($"{I18n.PageInitializationError}:\n{type.Name}", MessageBoxImage.Error);
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

        private void ClosePage(Page page)
        {
            // 获取page中的Close方法并执行
            // 用于关闭page中创建的线程
            if (page.GetType().GetMethod("Close") is MethodInfo info)
                _ = info.Invoke(page, null)!;
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
            string tomlFile = $"{directory}\\{expansionInfoFile}";
            try
            {
                if (!File.Exists(tomlFile))
                {
                    STLog.WriteLine($"{I18n.ExpansionLoadError} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    ST.ShowMessageBox($"{I18n.ExpansionLoadError}\n{I18n.Path}: {tomlFile}", MessageBoxImage.Warning);
                    return null;
                }
                var expansionInfo = new ExpansionInfo(TOML.Parse(tomlFile));
                var assemblyFile = $"{directory}\\{expansionInfo.ExpansionFile}";
                if (allExpansionsInfo.ContainsKey(expansionInfo.ExpansionId))
                {
                    STLog.WriteLine($"{I18n.ExtensionAlreadyExists} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    ST.ShowMessageBox($"{I18n.ExtensionAlreadyExists}\n{I18n.Path}: {tomlFile}", MessageBoxImage.Warning);
                    return null;
                }
                if (!File.Exists(assemblyFile))
                {
                    STLog.WriteLine($"{I18n.ExpansionFileError} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    ST.ShowMessageBox($"{I18n.ExpansionFileError}\n{I18n.Path}: {tomlFile}", MessageBoxImage.Warning);
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
                    ST.ShowMessageBox($"{I18n.ExpansionIdError}\n{I18n.Path}: {tomlFile}", MessageBoxImage.Warning);
                    return null;
                }
                return expansionInfo;
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.ExpansionLoadError} {I18n.Path}: {tomlFile}", ex);
                ST.ShowMessageBox($"{I18n.ExpansionLoadError}\n{I18n.Path}: {tomlFile}", MessageBoxImage.Error);
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
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBox_ExpansionMenu.Items.Add(item);
            expansionPages.Add(id, lazyPage);
            STLog.WriteLine($"{I18n.AddExpansionPage} {icon} {name}");
        }

        public void RefreshDebugExpansion()
        {
            if (ListBox_MainMenu.Items.Count > 3)
            {
                pages.Remove(((ListBoxItem)ListBox_MainMenu.Items[^2]).Tag.ToString()!);
                ListBox_MainMenu.Items.RemoveAt(ListBox_MainMenu.Items.Count - 2);
            }
            expansionDebugPath = settingsPage.TextBox_ExpansionDebugPath.Text;
            if (CheckExpansionInfo(expansionDebugPath, true) is ExpansionInfo info)
            {
                AddExpansionDebugPage(info);
                ListBox_MainMenu.SelectedIndex = ListBox_MainMenu.Items.Count - 2;
            }
        }

        private void AddExpansionDebugPage(ExpansionInfo expansionInfo)
        {
            string icon = expansionInfo.Icon;
            string name = expansionInfo.Name;
            string id = expansionInfo.Id;
            string toolTip = expansionInfo.Description;
            var item = CreateListBoxItemForPage(icon, name, id, toolTip);
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            // 重载当前菜单
            MenuItem menuItem = new();
            menuItem.Header = I18n.RefreshPage;
            menuItem.Icon = new Emoji.Wpf.TextBlock() { Text = "🔄" };
            menuItem.Click += (s, e) =>
            {
                RefreshDebugExpansion();
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBox_MainMenu.Items.Insert(ListBox_MainMenu.Items.Count - 1, item);
            Page page = (Page)expansionInfo.ExpansionType.Assembly.CreateInstance(expansionInfo.ExpansionType.FullName!)!;
            pages.Add(id, page);
            STLog.WriteLine($"{I18n.RefreshPage} {icon} {name}");
        }
    }
}