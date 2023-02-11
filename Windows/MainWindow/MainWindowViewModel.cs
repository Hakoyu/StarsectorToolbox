using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Libs.Log4Cs;
using HKW.ViewModels.Controls;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows.MainWindow
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
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

        #endregion Page

        #region PageItem

        [ObservableProperty]
        private ListBoxVM mainListBox = new();

        [ObservableProperty]
        private ListBoxVM expansionListBox = new();

        #endregion PageItem

        #region I18n

        [ObservableProperty]
        private string i18nStarsectorTools = I18n.StarsectorTools;

        [ObservableProperty]
        private string i18nInfo = I18n.Info;

        [ObservableProperty]
        private string i18nSettings = I18n.Settings;

        [ObservableProperty]
        private string i18nStartGame = I18n.StartGame;

        [ObservableProperty]
        private string i18nClearGameLogOnStart = I18n.ClearGameLogOnStart;

        [ObservableProperty]
        private string i18nRefreshExpansionList = I18n.RefreshExpansionList;

        [ObservableProperty]
        private string i18nExpansion = I18n.Expansion;

        #endregion I18n

        public MainWindowViewModel()
        {
            //InitializeDirectories();
        }

        public MainWindowViewModel(string configData)
        {
            Instance = this;
            InitializeDirectories();
            SetConfig(configData);
            InitializeExpansionPages();
            InitializeExpansionDebugPage();
        }

        [RelayCommand]
        private void ChangeMenuExpansion()
        {
            MenuIsExpand = !MenuIsExpand;
        }

        [RelayCommand]
        private void SelectionChanged(ListBoxItemVM item)
        {
            // 在对ListBoxVM.SelectedItem赋值触发此命令时,item并非ListBoxVM.SelectedItem,原因未知
            if (item is null || selectedItem == item)
            {
                selectedItem = null;
                return;
            }
            // 若切换选择,可取消原来的选中状态,以此达到多列表互斥
            if (selectedItem?.IsSelected is true)
                selectedItem.IsSelected = false;
            selectedItem = item;
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
                MainListBox.SelectedItem = ExpansionListBox.SelectedItem = null;
            }
            else if (page == settingsPage)
            {
                SettingsButtonIsChecked = true;
                MainListBox.SelectedItem = ExpansionListBox.SelectedItem = null;
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

        [RelayCommand]
        private void RefreshExpansionMenu()
        {
            CloseExpansionPages();
            InitializeExpansionPages();
        }
    }
}