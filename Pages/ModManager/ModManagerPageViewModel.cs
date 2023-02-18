using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Libs.Log4Cs;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialog;
using StarsectorTools.Libs.Utils;
using I18nRes = StarsectorTools.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorTools.Pages.ModManager
{
    internal partial class ModManagerPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableI18n<I18nRes> i18n = ObservableI18n<I18nRes>.Create(new());

        [ObservableProperty]
        private bool groupMenuIsExpand = false;

        /// <summary>当前选择的列表项</summary>
        [ObservableProperty]
        private ListBoxItemVM nowSelectedGroup = null!;

        private string nowGroupName => NowSelectedGroup!.Tag!.ToString()!;

        [ObservableProperty]
        private ObservableCollection<ModShowInfo> nowShowMods;

        /// <summary>模组详情的展开状态</summary>
        [ObservableProperty]
        private bool isShowModDetails = false;

        [ObservableProperty]
        private bool showModDependencies = false;
        [ObservableProperty]
        private BitmapImage? modDetailImage;
        [ObservableProperty]
        private string? modDetailName;
        [ObservableProperty]
        private string? modDetailId;
        [ObservableProperty]
        private string? modDetailModVersion;
        [ObservableProperty]
        private string? modDetailGameVersion;
        [ObservableProperty]
        private string? modDetailPath;
        [ObservableProperty]
        private string? modDetailAuthor;
        [ObservableProperty]
        private string? modDetailDescription;
        [ObservableProperty]
        private string? modDetailDependencies;
        [ObservableProperty]
        private string? modDetailUserDescription;

        [ObservableProperty]
        private string modFilterText;

        [ObservableProperty]
        private bool showRandomEnable = false;
        [ObservableProperty]
        private bool isRemindSave = false;

        /// <summary>当前选择的模组ID</summary>
        [ObservableProperty]
        private ModShowInfo? nowSelectedMod = null;

        [ObservableProperty]
        private ListBoxVM listBox_MainMenu =
            new()
            {
                new()
                {
                    Icon = "🅰",
                    Content = I18nRes.AllMods,ToolTip = I18nRes.AllMods,
                    Tag = ModTypeGroup.All
                },
                new()
                {
                    Icon = "✅",
                    Content = I18nRes.EnabledMods,ToolTip = I18nRes.EnabledMods,
                    Tag = ModTypeGroup.Enabled
                },
                new()
                {
                    Icon = "❎",
                    Content = I18nRes.DisabledMods,ToolTip = I18nRes.DisabledMods,
                    Tag = ModTypeGroup.Disabled
                },
            };

        [ObservableProperty]
        private ListBoxVM listBox_TypeGroupMenu =
            new()
            {
                new()
                {
                    Icon = "🔝",
                    Content = I18nRes.Libraries,ToolTip = I18nRes.Libraries,
                    Tag = ModTypeGroup.Libraries
                },
                new()
                {
                    Icon = "☢",
                    Content = I18nRes.MegaMods,ToolTip = I18nRes.MegaMods,
                    Tag = ModTypeGroup.MegaMods
                },
                new()
                {
                    Icon = "🏁",
                    Content = I18nRes.FactionMods,ToolTip = I18nRes.FactionMods,
                    Tag = ModTypeGroup.FactionMods
                },
                new()
                {
                    Icon = "🆙",
                    Content = I18nRes.ContentExtensions,ToolTip = I18nRes.ContentExtensions,
                    Tag = ModTypeGroup.ContentExtensions
                },
                new()
                {
                    Icon = "🛠",
                    Content = I18nRes.UtilityMods,ToolTip = I18nRes.UtilityMods,
                    Tag = ModTypeGroup.UtilityMods
                },
                new()
                {
                    Icon = "🛄",
                    Content = I18nRes.MiscellaneousMods,ToolTip = I18nRes.MiscellaneousMods,
                    Tag = ModTypeGroup.MiscellaneousMods
                },
                new()
                {
                    Icon = "✨",
                    Content = I18nRes.BeautifyMods,ToolTip = I18nRes.BeautifyMods,
                    Tag = ModTypeGroup.BeautifyMods
                },
                new()
                {
                    Icon = "🆓",
                    Content = I18nRes.UnknownMods,ToolTip = I18nRes.UnknownMods,
                    Tag = ModTypeGroup.UnknownMods
                },
            };

        [ObservableProperty]
        private ListBoxVM listBox_UserGroupMenu =
            new()
            {
                new()
                {
                    Icon = "🌟",
                    Content = I18nRes.CollectedMods,ToolTip = I18nRes.CollectedMods,
                    Tag = ModTypeGroup.Collected
                },
            };

        [ObservableProperty]
        private ComboBoxVM comboBox_ModFilterType =
            new()
            {
                new() { Content = I18nRes.Name,ToolTip = I18nRes.Name, Tag = nameof(I18nRes.Name) },
                new() { Content = "Id",ToolTip = "Id", Tag = "Id" },
                new() { Content = I18nRes.Author,ToolTip = I18nRes.Author, Tag = nameof(I18nRes.Author) },
                new() { Content = I18nRes.UserDescription,ToolTip = I18nRes.UserDescription, Tag = nameof(I18nRes.UserDescription) },
            };

        [ObservableProperty]
        private ComboBoxVM comboBox_ExportUserGroup = new();

        public ModManagerPageViewModel()
        {
            InitializeData();
            ComboBox_ModFilterType.SelectedIndex = 0;
            ListBox_MainMenu.SelectedIndex = 0;
            I18n.AddPropertyChangedAction(I18nPropertyChangeAction);
            ListBox_MainMenu.SelectionChangedEvent += ListBox_Menu_SelectionChangedEvent;
            ListBox_TypeGroupMenu.SelectionChangedEvent += ListBox_Menu_SelectionChangedEvent;
            ListBox_UserGroupMenu.SelectionChangedEvent += ListBox_Menu_SelectionChangedEvent;
        }

        private void I18nPropertyChangeAction()
        {
            ListBox_MainMenu[0].ToolTip = I18nRes.AllMods;
            ListBox_MainMenu[1].ToolTip = I18nRes.EnabledMods;
            ListBox_MainMenu[2].ToolTip = I18nRes.DisabledMods;
            ListBox_TypeGroupMenu[0].ToolTip = I18nRes.Libraries;
            ListBox_TypeGroupMenu[1].ToolTip = I18nRes.MegaMods;
            ListBox_TypeGroupMenu[2].ToolTip = I18nRes.FactionMods;
            ListBox_TypeGroupMenu[3].ToolTip = I18nRes.ContentExtensions;
            ListBox_TypeGroupMenu[4].ToolTip = I18nRes.UtilityMods;
            ListBox_TypeGroupMenu[5].ToolTip = I18nRes.MiscellaneousMods;
            ListBox_TypeGroupMenu[6].ToolTip = I18nRes.BeautifyMods;
            ListBox_TypeGroupMenu[7].ToolTip = I18nRes.UnknownMods;
            ListBox_UserGroupMenu[0].ToolTip = I18nRes.CollectedMods;
            ComboBox_ModFilterType[0].Content = I18nRes.Name;
            ComboBox_ModFilterType[1].Content = I18nRes.Author;
            ComboBox_ModFilterType[2].Content = I18nRes.UserDescription;
        }

        private void ListBox_Menu_SelectionChangedEvent(ListBoxItemVM item)
        {
            RefreshDataGrid();
        }

        [RelayCommand]
        private void GroupMenuExpand()
        {
            GroupMenuIsExpand = !GroupMenuIsExpand;
        }
    }
}
