using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using HKW.TomlParse;
using Panuon.WPF.UI;
using StarsectorTools.Libs;
using StarsectorTools.Pages;
using StarsectorTools.Tools.GameSettings;
using StarsectorTools.Tools.ModManager;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows
{
    public partial class MainWindow
    {
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
                STLog.Instance.WriteLine($"{I18n.PageInitializationError}: {nameof(Settings)}", ex);
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
                STLog.Instance.WriteLine($"{I18n.PageInitializationError}: {nameof(Info)}", ex);
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
                    STLog.Instance.LogLevel = STLog.Str2STLogLevel(toml["Extras"]["LogLevel"].AsString);
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
                    if (CheckExpansionInfo(filePath) is ExpansionInfo)
                    {
                        expansionDebugPath = filePath;
                        settingsPage.SetExpansionDebugPath(filePath);
                    }
                    else
                        toml["Game"]["GamePath"] = "";
                    toml.SaveTo(ST.configFile);
                }
                else
                {
                    if (!(ST.ShowMessageBox(I18n.FirstStart, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGameDirectory()))
                    {
                        ST.ShowMessageBox(I18n.GameNotFound_SoftwareExit, MessageBoxImage.Error);
                        return false;
                    }
                    ST.CreateConfigFile();
                    TomlTable toml = TOML.Parse(ST.configFile);
                    toml["Game"]["GamePath"] = ST.gameDirectory;
                    toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                    toml.SaveTo(ST.configFile);
                }
            }
            catch (Exception ex)
            {
                ST.SetMainWindowBlurEffect();
                STLog.Instance.WriteLine($"{I18n.ConfigFileError} {I18n.Path}: {ST.configFile}", ex);
                ST.ShowMessageBox($"{I18n.ConfigFileError}\n{I18n.Path}: {ST.configFile}", MessageBoxImage.Error);
                ST.CreateConfigFile();
                ST.RemoveMainWIndowBlurEffect();
            }
            return true;
        }

        public void SetBlurEffect()
        {
            Effect = new System.Windows.Media.Effects.BlurEffect();
        }

        public void RemoveBlurEffect()
        {
            Effect = null;
        }

        public void ChangeLanguage()
        {
            STLog.Instance.WriteLine($"{I18n.DIsplayLanguageIs} {Thread.CurrentThread.CurrentUICulture.Name}");
            Label_Title.Content = I18n.StarsectorTools;
            Button_Settings.Content = I18n.Settings;
            Button_Info.Content = I18n.Info;
            RefreshMenu();
            RefreshExpansionMenu();
            STLog.Instance.WriteLine(I18n.MenuListRefreshComplete);
        }

        private void ShowPage()
        {
            if (string.IsNullOrEmpty(expansionDebugPath))
            {
                ListBox_Menu.SelectedIndex = 0;
                return;
            }
            if (CheckExpansionInfo(expansionDebugPath) is ExpansionInfo info)
            {
                AddMemu(info.Icon, info.Name, info.Id, info.Description, CreateMenu(info.ExpansionType));
                ListBox_Menu.SelectedIndex = ListBox_Menu.Items.Count - 2;
            }
        }

        private void RefreshMenu()
        {
            ClearMenu();
            AddMemu("🌐", I18n.ModManager, nameof(ModManager), I18n.ModManagerToolTip, CreateMenu(typeof(ModManager)));
            AddMemu("⚙", I18n.GameSettings, nameof(GameSettings), I18n.GameSettingsToolTip, CreateMenu(typeof(GameSettings)));

        }
        private Page? CreateMenu(Type type)
        {
            try
            {
                return (Page)type.Assembly.CreateInstance(type.FullName!)!;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.PageInitializationError}: {type.Name}", ex);
                ST.ShowMessageBox($"{I18n.PageInitializationError}:\n{type.Name}", MessageBoxImage.Error);
                return null;
            }
        }

        private void ClearMenu()
        {
            foreach (var page in menus.Values)
                ClosePage(page);
            menus.Clear();
            while (ListBox_Menu.Items.Count > 1)
                ListBox_Menu.Items.RemoveAt(0);
        }

        private void ClosePage(Page page)
        {
            // 获取page中的Close方法并执行
            // 用于关闭page中创建的线程
            if (page.GetType().GetMethod("Close") is MethodInfo info)
                _ = info.Invoke(page, null)!;
        }

        private void AddMemu(string icon, string name, string id, string toolTip, Page? page)
        {
            if (page is null)
                return;
            var item = new ListBoxItem
            {
                Content = name,
                ToolTip = toolTip,
                Tag = id,
                Style = (Style)Application.Current.Resources["ListBoxItem_Style"]
            };
            ListBoxItemHelper.SetIcon(item, new Emoji.Wpf.TextBlock() { Text = icon });
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            // 重载当前菜单
            MenuItem menuItem = new();
            menuItem.Header = I18n.RefreshPage;
            menuItem.Icon = new Emoji.Wpf.TextBlock() { Text = "🔄" };
            menuItem.Click += (s, e) =>
            {
                ClosePage(menus[id]);
                if (CreateMenu(menus[id].GetType()) is not Page newPage)
                    return;
                menus[id] = newPage;
                if (Frame_MainFrame.Content is Page oldPage && oldPage.GetType() == newPage.GetType())
                    Frame_MainFrame.Content = menus[id];
                STLog.Instance.WriteLine($"{I18n.RefreshPage}: {id}");
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBox_Menu.Items.Insert(ListBox_Menu.Items.Count - 1, item);
            menus.Add(id, page);
            STLog.Instance.WriteLine($"{I18n.AddMenu} {icon} {name}");
        }
        private void RefreshExpansionMenu()
        {
            ClearExpansionMenu();
            GetAllExpansion();
        }
        private void ClearExpansionMenu()
        {
            foreach (var lazyPage in expansionMenus.Values)
                ClosePage(lazyPage.Value);
            expansionMenus.Clear();
            allExpansionInfo.Clear();
            ListBox_ExpansionMenu.Items.Clear();
        }
        private void GetAllExpansion()
        {
            string nowDir = null!;
            try
            {
                DirectoryInfo dirs = new(expansionDirectories);
                foreach (var dir in dirs.GetDirectories())
                {
                    nowDir = dir.FullName;
                    if (CheckExpansionInfo(nowDir) is ExpansionInfo expansionInfo)
                        GetExpansionMenu(expansionInfo);
                }
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.ExpansionLoadError} {I18n.Path}: {nowDir}", ex);
                ST.ShowMessageBox($"{I18n.ExpansionLoadError} {I18n.Path}: {nowDir}", MessageBoxImage.Error);
            }
        }
        private ExpansionInfo? CheckExpansionInfo(string directory)
        {
            string tomlFile = $"{directory}\\{expansionInfoFile}";
            try
            {
                if (!File.Exists(tomlFile))
                {
                    STLog.Instance.WriteLine($"{I18n.ExpansionLoadError} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    ST.ShowMessageBox($"{I18n.ExpansionLoadError}\n{I18n.Path}: {tomlFile}", MessageBoxImage.Warning);
                    return null;
                }
                var expansionInfo = new ExpansionInfo(TOML.Parse(tomlFile));
                if (!File.Exists($"{directory}\\{expansionInfo.ExpansionFile}"))
                {
                    STLog.Instance.WriteLine($"{I18n.ExpansionFileError} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    ST.ShowMessageBox($"{I18n.ExpansionFileError}\n{I18n.Path}: {tomlFile}", MessageBoxImage.Warning);
                    return null;
                }
                expansionInfo.ExpansionType = Assembly.LoadFrom($"{directory}\\{expansionInfo.ExpansionFile}").GetType(expansionInfo.ExpansionId)!;
                if (expansionInfo.ExpansionType is null)
                {
                    STLog.Instance.WriteLine($"{I18n.ExpansionIdError} {I18n.Path}: {tomlFile}", STLogLevel.WARN);
                    ST.ShowMessageBox($"{I18n.ExpansionIdError}\n{I18n.Path}: {tomlFile}", MessageBoxImage.Warning);
                    return null;
                }
                return expansionInfo;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.ExpansionLoadError} {I18n.Path}: {tomlFile}", ex);
                ST.ShowMessageBox($"{I18n.ExpansionLoadError}\n{I18n.Path}: {tomlFile}", MessageBoxImage.Error);
                return null;
            }
        }
        private void GetExpansionMenu(ExpansionInfo expansionInfo)
        {
            allExpansionInfo.Add(expansionInfo.Id, expansionInfo);
            AddExpansionMenu(expansionInfo.Icon,
                             expansionInfo.Name,
                             expansionInfo.Id,
                             expansionInfo.Description,
                             new(() => (Page)expansionInfo.ExpansionType.Assembly.CreateInstance(expansionInfo.ExpansionType.FullName!)!));
        }
        private void AddExpansionMenu(string icon, string name, string id, string toolTip, Lazy<Page> lazyPage)
        {
            var item = new ListBoxItem
            {
                Content = name,
                ToolTip = toolTip,
                Tag = id,
                Style = (Style)Application.Current.Resources["ListBoxItem_Style"]
            };
            ListBoxItemHelper.SetIcon(item, new Emoji.Wpf.TextBlock() { Text = icon });
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            // 重载当前菜单
            MenuItem menuItem = new();
            menuItem.Header = I18n.RefreshPage;
            menuItem.Icon = new Emoji.Wpf.TextBlock() { Text = "🔄" };
            menuItem.Click += (s, e) =>
            {
                Type type = allExpansionInfo[id].ExpansionType;
                ClosePage(expansionMenus[id].Value);
                expansionMenus[id] = new(() => (Page)type.Assembly.CreateInstance(type.FullName!)!);
                if (Frame_MainFrame.Content is Page _page && _page.GetType() == type)
                    Frame_MainFrame.Content = expansionMenus[id].Value;
                STLog.Instance.WriteLine($"{I18n.RefreshPage}: {id}");
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBox_ExpansionMenu.Items.Add(item);
            expansionMenus.Add(id, lazyPage);
            STLog.Instance.WriteLine($"{I18n.AddExpansionMenu} {icon} {name}");
        }
    }
}