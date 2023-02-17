using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Pages.GameSettings.GameSettingsPageI18nRes;
using HKW.Libs.TomlParse;
using HKW.Libs;

namespace StarsectorTools.Pages.GameSettings
{
    /// <summary>
    /// GameSettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class GameSettingsPage : Page
    {
        internal GameSettingsPageViewModel ViewModel => (GameSettingsPageViewModel)DataContext;
        /// <summary>
        /// 
        /// </summary>
        public GameSettingsPage()
        {
            InitializeComponent();
            DataContext = new GameSettingsPageViewModel(true);
        }

        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");

    }
}