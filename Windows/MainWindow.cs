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
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows
{
    public partial class MainWindow
    {
        void SetConfig()
        {
            try
            {
                if (ST.CreateConfigFile())
                {
                    using TomlTable toml = TOML.Parse(ST.configPath);
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(toml["Extras"]["Lang"].AsString);
                    ST.SetGamePath(toml["GamePath"].AsString);
                    if (!ST.TestGamePath())
                    {
                        if (!(MessageBox.Show(this, I18n.GameNotFound_SelectAgain, MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGamePath()))
                        {
                            MessageBox.Show(this, I18n.GameNotFound_SoftwareExit, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                            toml.Close();
                            Close();
                        }
                        toml["GamePath"] = ST.gamePath;
                    }
                    toml.SaveTo(ST.configPath);
                }
                else
                {
                    using TomlTable toml = TOML.Parse(ST.configPath);
                    if (!(MessageBox.Show(this, $"{I18n.FirstStart}(starsector.exe)", MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && ST.GetGamePath()))
                    {
                        MessageBox.Show(this, I18n.GameNotFound_SoftwareExit, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                        toml.Close();
                        Close();
                    }
                    toml["GamePath"] = ST.gamePath;
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
            MessageBox.Show(I18n.SettingFileError, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK);
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
    }
}
