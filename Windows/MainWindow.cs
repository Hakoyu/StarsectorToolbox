using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HKW.TomlParse;
using StarsectorTools.Libs;
using StarsectorTools.Tools.GameSettings;
using StarsectorTools.Tools.ModManager;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;
using Panuon.WPF.UI;
using System.Reflection;

namespace StarsectorTools.Windows
{
    public partial class MainWindow
    {
        bool SetConfig()
        {
            try
            {
                if (ST.CheckConfigFile())
                {
                    using TomlTable toml = TOML.Parse(ST.configPath);
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    STLog.Instance.LogLevel = STLog.Str2STLogLevel(toml["Extras"]["LogLevel"].AsString);
                    ST.SetGamePath(toml["Game"]["GamePath"].AsString!);
                    if (!ST.CheckGamePath())
                    {
                        if (!(MessageBox.Show(this, I18n.GameNotFound_SelectAgain, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGamePath()))
                        {
                            MessageBox.Show(this, I18n.GameNotFound_SoftwareExit, "", MessageBoxButton.OK, MessageBoxImage.Error);
                            toml.Close();
                            return false;
                        }
                        toml["Game"]["GamePath"] = ST.gamePath;
                    }
                    toml.SaveTo(ST.configPath);
                }
                else
                {
                    if (!(MessageBox.Show(this, I18n.FirstStart, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGamePath()))
                    {
                        MessageBox.Show(this, I18n.GameNotFound_SoftwareExit, "", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    ST.CreateConfigFile();
                    using TomlTable toml = TOML.Parse(ST.configPath);
                    toml["Game"]["GamePath"] = ST.gamePath;
                    toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                    toml.SaveTo(ST.configPath);
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
        private void SetBlurEffect()
        {
            Effect = new System.Windows.Media.Effects.BlurEffect();
        }
        private void RemoveBlurEffect()
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
        void ClearMenu()
        {
            foreach (Page page in menuList.Values)
                if (page.GetType().GetMethod("Close") is MethodInfo info)
                    _ = info.Invoke(page, null)!;
            menuList.Clear();
            ListBox_Menu.Items.Clear();
        }
        void AddMemu(string icon, string name, string tag, Page page)
        {
            var item = new ListBoxItem();
            item.Content = name;
            item.ToolTip = name;
            item.Tag = tag;
            item.Style = (Style)Application.Current.Resources["ListBoxItem_Style"];
            ListBoxItemHelper.SetIcon(item, new Emoji.Wpf.TextBlock() { Text = icon });
            ListBox_Menu.Items.Add(item);
            menuList.Add(tag, page);
            STLog.Instance.WriteLine($"{I18n.AddMenu} {icon} {name} {page}");
        }
    }
}
