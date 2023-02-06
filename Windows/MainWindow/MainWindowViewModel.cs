using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Model;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows.MainWindow
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// 主菜单展开状态
        /// </summary>
        [ObservableProperty]
        private bool menuIsExpand = false;
        [ObservableProperty]
        private bool clearGameLogOnStart = true;
        [ObservableProperty]
        private bool infoPageIsChecked = false;
        [ObservableProperty]
        private bool settingsPageIsChecked = false;

        #region Page
        [ObservableProperty]
        private object? nowPage;
        [ObservableProperty]
        private object? infoPage;
        [ObservableProperty]
        private object? settingsPage;
        [ObservableProperty]
        private object? expansionDebugPage;
        #endregion
        #region PageItem
        [ObservableProperty]
        private ListBoxItemModel? selectedPageItem;
        [ObservableProperty]
        private List<ListBoxItemModel> mainPageItems = new();
        [ObservableProperty]
        private List<ListBoxItemModel> expansionPageItems = new();
        #endregion
        #region I18n
        [ObservableProperty]
        private string titleI18n = I18n.StarsectorTools;
        [ObservableProperty]
        private string infoI18n = I18n.Info;
        [ObservableProperty]
        private string settingsI18n = I18n.Settings;
        [ObservableProperty]
        private string startGameI18n = I18n.StartGame;
        [ObservableProperty]
        private string clearGameLogOnStartI18n = I18n.ClearGameLogOnStart;
        #endregion

        public MainWindowViewModel()
        {
            //InitializeDirectories();
            MainPageItems.Add(new()
            {
                Icon = "A",
                Content = "AAA",
            });
            MainPageItems.Add(new()
            {
                Icon = "B",
                Content = "BBB",
            });
            MainPageItems.Add(new()
            {
                Icon = "C",
                Content = "CCC",
            });
            MainPageItems.Add(new()
            {
                Icon = "D",
                Content = "DDD",
            });
            MainPageItems.Add(new()
            {
                Icon = "E",
                Content = "EEE",
            });
            MainPageItems.Add(new()
            {
                Icon = "F",
                Content = "FFF",
            });

        }
        public MainWindowViewModel(string configData)
        {
            InitializeDirectories();
            SetConfig(configData);
        }

        [RelayCommand]
        private void ChangeMenuExpansionStatus()
        {
            MenuIsExpand = !MenuIsExpand;
        }

        [RelayCommand]
        private void ShowPage(object? page)
        {
            NowPage = page;
            // 取消按钮页面选中状态
            // 设置选中状态
            if (page == infoPage)
            {
                InfoPageIsChecked = true;
                SettingsPageIsChecked = false;
                SelectedPageItem = null;
            }
            else if (page == settingsPage)
            {
                SettingsPageIsChecked = true;
                InfoPageIsChecked = false;
                SelectedPageItem = null;
            }
            STLog.WriteLine($"{I18n.ShowPage}: {page?.GetType().FullName}");
        }

        [RelayCommand]
        private void StartGame()
        {
            if (Utils.FileExists(GameInfo.ExeFile))
            {
                SaveAllPages();
                CheckGameStartOption();
                using System.Diagnostics.Process process = new();
                process.StartInfo.FileName = "cmd";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardInput = true;
                if (process.Start())
                {
                    process.StandardInput.WriteLine($"cd /d {GameInfo.BaseDirectory}");
                    process.StandardInput.WriteLine($"starsector.exe");
                }
            }
        }
    }
}
