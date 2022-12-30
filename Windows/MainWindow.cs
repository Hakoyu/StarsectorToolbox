﻿using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using HKW.TomlParse;
using Panuon.WPF.UI;
using StarsectorTools.Libs;
using StarsectorTools.Tools.GameSettings;
using StarsectorTools.Tools.ModManager;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows
{
    public partial class MainWindow
    {
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
                        toml.SaveTo(ST.configFile);
                    }
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
            Label_Title.Content = I18n.StarsectorTools;
            Button_Settings.Content = I18n.Settings;
            Button_Info.Content = I18n.Info;
            RefreshMenuList();
        }

        public void RefreshMenuList()
        {
            ClearMenu();
            AddMemu("🌐", I18n.ModManager, nameof(ModManager), new(() => new ModManager()));
            AddMemu("⚙", I18n.GameSettings, nameof(GameSettings), new(() => new GameSettings()));

            STLog.Instance.WriteLine(I18n.MenuListRefreshComplete);
        }

        private void ClearMenu()
        {
            foreach (var lazyPage in menuList.Values)
                ClosePage(lazyPage.Value);
            menuList.Clear();
            while (ListBox_Menu.Items.Count > 2)
                ListBox_Menu.Items.RemoveAt(0);
        }

        private void ClosePage(Page page)
        {
            // 获取page中的Close方法并执行
            // 用于关闭page中创建的线程
            if (page.GetType().GetMethod("Close") is MethodInfo info)
                _ = info.Invoke(page, null)!;
        }

        private void AddMemu(string icon, string name, string tag, Lazy<Page> lazyPage)
        {
            var item = new ListBoxItem();
            item.Content = name;
            item.ToolTip = name;
            item.Tag = tag;
            item.Style = (Style)Application.Current.Resources["ListBoxItem_Style"];
            ListBoxItemHelper.SetIcon(item, new Emoji.Wpf.TextBlock() { Text = icon });
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            // 重载当前菜单
            MenuItem menuItem = new();
            menuItem.Header = I18n.RefreshPage;
            menuItem.Icon = new Emoji.Wpf.TextBlock() { Text = "🔄" };
            menuItem.Click += (s, e) =>
            {
                Type type = menuList[tag].Value.GetType();
                ClosePage(menuList[tag].Value);
                menuList[tag] = new(() => (Page)type.Assembly.CreateInstance(type.FullName!)!);
                if (Frame_MainFrame.Content is Page _page && _page.GetType() == type)
                    Frame_MainFrame.Content = menuList[tag].Value;
                STLog.Instance.WriteLine($"{I18n.RefreshPage}: {tag}");
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBox_Menu.Items.Insert(ListBox_Menu.Items.Count - 2, item);
            menuList.Add(tag, lazyPage);
            STLog.Instance.WriteLine($"{I18n.AddMenu} {icon} {name}");
        }
    }
}