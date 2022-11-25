using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using HKW.TomlParse;
using StarsectorTools.Lib;
using StarsectorTools.Langs.MessageBox;

namespace StarsectorTools.Windows
{
    public partial class MainWindow
    {
        void SetConfig()
        {
            try
            {
                if (Global.CreateConfigFile())
                {
                    using TomlTable toml = TOML.Parse(Global.configPath);
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    Global.SetGamePath(toml["GamePath"].AsString);
                    if (!Global.TestGamePath())
                    {
                        if (!(MessageBox.Show(this, "游戏本体路径出错\n请重新选择", MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && Global.GetGamePath()))
                        {
                            MessageBox.Show(this, "未确认游戏本体路径,软件即将退出", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                            Application.Current.Shutdown();
                        }
                        toml["GamePath"] = Global.gamePath;
                    }
                    toml.SaveTo(Global.configPath);
                }
                else
                {
                    using TomlTable toml = TOML.Parse(Global.configPath);
                    if (!(MessageBox.Show(this, "第一次启动软件\n请选择游戏本体", MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && Global.GetGamePath()))
                    {
                        MessageBox.Show(this, "未确认游戏本体路径,软件即将退出", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                        Application.Current.Shutdown();
                    }
                    toml["GamePath"] = Global.gamePath;
                    toml.SaveTo(Global.configPath);
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
            MessageBox.Show("设置文件出现错误,将恢复为默认设置", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK);
            SetConfig();
            RemoveBlurEffect();
        }
        
        void InitializeMenuList()
        {
           
        }
        private void SetBlurEffect()
        {
            Effect = new System.Windows.Media.Effects.BlurEffect();
        }
        private void RemoveBlurEffect()
        {
            Effect = null;
        }
    }
}
