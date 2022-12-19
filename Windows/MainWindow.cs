﻿using System;
using System.Globalization;
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
                if (ST.CheckConfigFile())
                {
                    using TomlTable toml = TOML.Parse(ST.STConfigFile);
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    STLog.Instance.LogLevel = STLog.Str2STLogLevel(toml["Extras"]["LogLevel"].AsString);
                    ST.SetGameData(toml["Game"]["GamePath"].AsString!);
                    if (!ST.CheckGameDirectory())
                    {
                        if (!(MessageBox.Show(this, I18n.GameNotFound_SelectAgain, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGameDirectory()))
                        {
                            MessageBox.Show(this, I18n.GameNotFound_SoftwareExit, "", MessageBoxButton.OK, MessageBoxImage.Error);
                            toml.Close();
                            return false;
                        }
                        toml["Game"]["GamePath"] = ST.gameDirectory;
                    }
                    toml.SaveTo(ST.STConfigFile);
                }
                else
                {
                    if (!(MessageBox.Show(this, I18n.FirstStart, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGameDirectory()))
                    {
                        MessageBox.Show(this, I18n.GameNotFound_SoftwareExit, "", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    ST.CreateConfigFile();
                    using TomlTable toml = TOML.Parse(ST.STConfigFile);
                    toml["Game"]["GamePath"] = ST.gameDirectory;
                    toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                    toml.SaveTo(ST.STConfigFile);
                }
            }
            catch
            {
                SetBlurEffect();
                MessageBox.Show(I18n.ConfigFileError, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                ST.CreateConfigFile();
                RemoveBlurEffect();
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
            AddMemu("🌐", I18n.ModManager, nameof(ModManager), new ModManager());
            AddMemu("⚙", I18n.GameSettings, nameof(GameSettings), new GameSettings());

            STLog.Instance.WriteLine(I18n.MenuListRefreshComplete);
        }

        private void ClearMenu()
        {
            foreach (var lazyPage in menuList.Values)
                ClosePage(lazyPage.Value);
            menuList.Clear();
            ListBox_Menu.Items.Clear();
        }

        private void ClosePage(Page page)
        {
            // 获取page中的Close方法并执行
            // 用于关闭page中创建的线程
            if (page.GetType().GetMethod("Close") is MethodInfo info)
                _ = info.Invoke(page, null)!;
        }

        private void AddMemu(string icon, string name, string tag, Page page)
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
            menuItem.Header = I18n.Refresh;
            menuItem.Icon = new Emoji.Wpf.TextBlock() { Text = "🔄" };
            menuItem.Click += (s, e) =>
            {
                Type type = menuList[tag].Value.GetType();
                ClosePage(menuList[tag].Value);
                menuList[tag] = new((Page)type.Assembly.CreateInstance(type.FullName!)!);
                if (Frame_MainFrame.Content is Page _page && _page.GetType() == type)
                    Frame_MainFrame.Content = menuList[tag].Value;
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBox_Menu.Items.Add(item);
            menuList.Add(tag, new(page));
            STLog.Instance.WriteLine($"{I18n.AddMenu} {icon} {name} {page}");
        }
    }
}