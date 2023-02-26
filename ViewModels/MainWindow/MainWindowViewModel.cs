﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HKW.Libs.Log4Cs;
using HKW.Libs.TomlParse;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Messages;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Resources;
using I18nRes = StarsectorTools.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorTools.ViewModels.MainWindow
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    internal partial class MainWindowViewModel : ObservableObject
    {
        #region ObservableProperty
        [ObservableProperty]
        private ObservableI18n<I18nRes> i18n = ObservableI18n<I18nRes>.Create(new());

        [ObservableProperty]
        private bool menuIsExpand = false;

        [ObservableProperty]
        private bool clearGameLogOnStart = true;

        [ObservableProperty]
        private bool infoButtonIsChecked = false;

        [ObservableProperty]
        private bool settingsButtonIsChecked = false;

        #endregion

        #region Page

        [ObservableProperty]
        private object? nowPage;

        [ObservableProperty]
        private object? infoPage;

        [ObservableProperty]
        private object? settingsPage;

        [ObservableProperty]
        private object? extensionDebugPage;

        #endregion Page

        #region ListBox

        [ObservableProperty]
        private ListBoxVM listBox_MainMenu = new();

        [ObservableProperty]
        private ListBoxVM listBox_ExtensionMenu = new();

        #endregion ListBox

        public MainWindowViewModel()
        {
            //InitializeDirectories();
        }

        public MainWindowViewModel(bool noop)
        {
            Instance = this;
            InitializeData();
            InitializeDirectories();
            // 注册日志
            Logger.Initialize(nameof(StarsectorTools), ST.LogFile);
            InitializeConfig();
            InitializeExtensionPages();
            InitializeExtensionDebugPage();
            WeakReferenceMessenger.Default.Register<ExtensionDebugPathChangeMessage>(this, ExtensionDebugPathChangeReceive);
            WeakReferenceMessenger.Default.Register<ExtensionDebugPathRequestMessage>(this, ExtensionDebugPathRequestReceive);
            I18n.AddPropertyChangedAction(I18nChangedAction);
        }

        private void I18nChangedAction()
        {
            foreach (var item in ListBox_MainMenu)
            {
                if (item is ISTPage iPage)
                {
                    item.Content = iPage.GetNameI18n();
                    item.ToolTip = iPage.GetDescriptionI18n();
                }
            }
            foreach (var item in ListBox_ExtensionMenu)
            {
                if (item is ISTPage iPage)
                {
                    item.Content = iPage.GetNameI18n();
                    item.ToolTip = iPage.GetDescriptionI18n();
                }
            }
        }

        private void InitializeData()
        {
            ListBox_MainMenu.SelectionChangedEvent += ListBox_SelectionChangedEvent;
            ListBox_ExtensionMenu.SelectionChangedEvent += ListBox_SelectionChangedEvent;
        }
        private void ListBox_SelectionChangedEvent(ListBoxItemVM item)
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

        private void ExtensionDebugPathChangeReceive(object recipient, ExtensionDebugPathChangeMessage message)
        {
            var toml = TOML.Parse(ST.ConfigTomlFile);
            toml["Extension"]["DebugPath"] = message.Value;
            toml.SaveTo(ST.ConfigTomlFile);
            if (GetExtensionInfo(message.Value, true) is ExtensionInfo info)
            {
                deubgItemExtensionInfo = info;
                deubgItemPath = message.Value;
            }
        }

        private void ExtensionDebugPathRequestReceive(object recipient, ExtensionDebugPathRequestMessage message)
        {
            message.Reply(deubgItemPath!);
        }

        #region RelayCommand

        [RelayCommand]
        private void MenuExpand(object parameter) => MenuIsExpand = !MenuIsExpand;

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
                ListBox_MainMenu.SelectedItem = ListBox_ExtensionMenu.SelectedItem = null;
            }
            else if (page == settingsPage)
            {
                SettingsButtonIsChecked = true;
                ListBox_MainMenu.SelectedItem = ListBox_ExtensionMenu.SelectedItem = null;
            }
            Logger.Record($"{I18nRes.ShowPage}: {page?.GetType().FullName}");
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
        private void RefreshExtensionMenu()
        {
            CloseExtensionPages();
            InitializeExtensionPages();
        }
        #endregion
    }
}