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
using StarsectorTools.Lib;
using StarsectorTools.Tools.GameSettings;
using StarsectorTools.Tools.ModManager;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;
using Panuon.WPF.UI;

namespace StarsectorTools.Windows
{
    public partial class MainWindow
    {
        void SetConfig()
        {
            try
            {
                if (ST.CheckConfigFile())
                {
                    using TomlTable toml = TOML.Parse(ST.configPath);
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    ST.SetGamePath(toml["Game"]["GamePath"].AsString);
                    if (!ST.CheckGamePath())
                    {
                        if (!(MessageBox.Show(this, I18n.GameNotFound_SelectAgain, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGamePath()))
                        {
                            MessageBox.Show(this, I18n.GameNotFound_SoftwareExit, "", MessageBoxButton.OK, MessageBoxImage.Error);
                            toml.Close();
                            Close();
                        }
                        toml["Game"]["GamePath"] = ST.gamePath;
                    }
                    toml.SaveTo(ST.configPath);
                }
                else
                {
                    ST.CreateConfigFile();
                    using TomlTable toml = TOML.Parse(ST.configPath);
                    if (!(MessageBox.Show(this, I18n.FirstStart, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGamePath()))
                    {
                        MessageBox.Show(this, I18n.GameNotFound_SoftwareExit, "", MessageBoxButton.OK, MessageBoxImage.Error);
                        toml.Close();
                        Close();
                    }
                    toml["Game"]["GamePath"] = ST.gamePath;
                    toml["Extras"]["Lang"] = Thread.CurrentThread.CurrentUICulture.Name;
                    toml.SaveTo(ST.configPath);
                }
            }
            catch
            {
                ConfigLoadError();
            }
        }
        void ConfigLoadError()
        {
            SetBlurEffect();
            MessageBox.Show(I18n.ConfigFileError, "", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetConfig();
            RemoveBlurEffect();
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
            AddMemu("🌐", I18n.ModManager, new ModManager());
            AddMemu("⚙", I18n.GameSettings, new GameSettings());

            STLog.Instance.WriteLine(I18n.MenuListRefreshComplete);
        }
        void ClearMenu()
        {
            menuList.Clear();
            ListBox_Menu.Items.Clear();
        }
        void AddMemu(string icon, string name, Page page)
        {
            var item = new ListBoxItem();
            item.Content = name;
            item.Style = (Style)Application.Current.Resources["ListBoxItem_Style"];
            ListBoxItemHelper.SetIcon(item, new Emoji.Wpf.TextBlock() { Text = icon });
            ListBox_Menu.Items.Add(item);
            menuList.Add(name, page);
            STLog.Instance.WriteLine($"{I18n.AddMenu} {Icon} {name} {page}");
        }
    }
}
