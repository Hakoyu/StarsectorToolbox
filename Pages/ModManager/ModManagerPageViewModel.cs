using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
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

        [ObservableProperty]
        private ListBoxVM listBox_MainMenu =
            new()
            {
                new()
                {
                    Icon = "🅰",
                    Content = I18nRes.AllMods,
                    Tag = ModTypeGroup.All
                },
                new()
                {
                    Icon = "✅",
                    Content = I18nRes.EnabledMods,
                    Tag = ModTypeGroup.Enabled
                },
                new()
                {
                    Icon = "❎",
                    Content = I18nRes.DisabledMods,
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
                    Content = I18nRes.Libraries,
                    Tag = ModTypeGroup.Libraries
                },
                new()
                {
                    Icon = "☢",
                    Content = I18nRes.MegaMods,
                    Tag = ModTypeGroup.MegaMods
                },
                new()
                {
                    Icon = "🏁",
                    Content = I18nRes.FactionMods,
                    Tag = ModTypeGroup.FactionMods
                },
                new()
                {
                    Icon = "🆙",
                    Content = I18nRes.ContentExtensions,
                    Tag = ModTypeGroup.ContentExtensions
                },
                new()
                {
                    Icon = "🛠",
                    Content = I18nRes.UtilityMods,
                    Tag = ModTypeGroup.UtilityMods
                },
                new()
                {
                    Icon = "🛄",
                    Content = I18nRes.MiscellaneousMods,
                    Tag = ModTypeGroup.MiscellaneousMods
                },
                new()
                {
                    Icon = "✨",
                    Content = I18nRes.BeautifyMods,
                    Tag = ModTypeGroup.BeautifyMods
                },
                new()
                {
                    Icon = "🆓",
                    Content = I18nRes.UnknownMods,
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
                    Content = I18nRes.CollectedMods,
                    Tag = ModTypeGroup.Collected
                },
            };

        public ModManagerPageViewModel()
        {
            I18n.AddChangedAction(I18nAction);
            ListBox_MainMenu.SelectionChangedEvent += ListBox_Menu_SelectionChangedEvent;
            ListBox_TypeGroupMenu.SelectionChangedEvent += ListBox_Menu_SelectionChangedEvent;
            ListBox_UserGroupMenu.SelectionChangedEvent += ListBox_Menu_SelectionChangedEvent;
        }

        private void I18nAction()
        {
            ListBox_MainMenu[0].Content = I18nRes.AllMods;
            ListBox_MainMenu[1].Content = I18nRes.EnabledMods;
            ListBox_MainMenu[2].Content = I18nRes.DisabledMods;
            ListBox_TypeGroupMenu[0].Content = I18nRes.Libraries;
            ListBox_TypeGroupMenu[1].Content = I18nRes.MegaMods;
            ListBox_TypeGroupMenu[2].Content = I18nRes.FactionMods;
            ListBox_TypeGroupMenu[3].Content = I18nRes.ContentExtensions;
            ListBox_TypeGroupMenu[4].Content = I18nRes.UtilityMods;
            ListBox_TypeGroupMenu[5].Content = I18nRes.MiscellaneousMods;
            ListBox_TypeGroupMenu[6].Content = I18nRes.BeautifyMods;
            ListBox_TypeGroupMenu[7].Content = I18nRes.UnknownMods;
            ListBox_UserGroupMenu[0].Content = I18nRes.CollectedMods;
        }

        private void ListBox_Menu_SelectionChangedEvent(ListBoxItemVM item)
        {
        }

        [RelayCommand]
        private void GroupMenuExpand()
        {
            GroupMenuIsExpand = !GroupMenuIsExpand;
        }
    }
}
