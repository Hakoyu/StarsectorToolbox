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
using HKW.Libs.Log4Cs;
using HKW.Models.ControlModels;
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
        private bool infoButtonIsChecked = false;
        [ObservableProperty]
        private bool settingsButtonIsChecked = false;
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
        private ListBoxModel mainListBox = new();
        [ObservableProperty]
        private ListBoxModel expansionListBox = new();
        #endregion
        [ObservableProperty]
        private ContextMenuModel contextMenu;
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
            MainListBox.Add(new()
            {
                Icon = "A",
                Content = "AAA",
                ContextMenu = new() { new() { Name = "AMenu1", Header = "AMenu1" } }
            });
            MainListBox.Add(new()
            {
                Icon = "B",
                Content = "BBB",
                ContextMenu = new() { new() { Name = "BMenu1", Header = "CMenu1" } }
            });
            MainListBox.Add(new()
            {
                Icon = "C",
                Content = "CCC",
                ContextMenu = new() { new() { Name = "CMenu1", Header = "CMenu1" } }
            });
        }
        public MainWindowViewModel(string configData)
        {
            MainListBox.SelectedItem = ExpansionListBox.SelectedItem = SelectedPageItem;
            InitializeDirectories();
            SetConfig(configData);
            InitializeExpansionPage();
            InitializeExpansionDebugPage();
        }

        [RelayCommand]
        private void ChangeMenuExpansion()
        {
            MenuIsExpand = !MenuIsExpand;
        }
        [RelayCommand]
        private void MenuSelectionChanged(ListBoxItemModel item)
        {
            // 若切换选择,可取消原来的选中状态,以此达到多列表互斥
            if (previousSelectedPageItem?.IsSelected is true)
                previousSelectedPageItem.IsSelected = false;
            previousSelectedPageItem = item;
            ShowPage(item.Tag);
        }

        [RelayCommand]
        private void ShowPage(object? page)
        {
            NowPage = page;
            // 取消按钮页面选中状态
            InfoButtonIsChecked = false;
            SettingsButtonIsChecked = false;
            // 设置选中状态
            if (page == infoPage)
            {
                InfoButtonIsChecked = true;
                SelectedPageItem = null;
            }
            else if (page == settingsPage)
            {
                SettingsButtonIsChecked = true;
                SelectedPageItem = null;
            }
            Logger.Record($"{I18n.ShowPage}: {page?.GetType().FullName}");
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
