using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.Model
{
    /// <summary>
    /// 菜单项模型,用于MVVM
    /// </summary>
    public partial class MenuItemModel : ObservableObject
    {
        [ObservableProperty]
        private object? header;
        [ObservableProperty]
        private object? toolTip;
        [ObservableProperty]
        private object? icon;
        [ObservableProperty]
        private object? tag;
        [ObservableProperty]
        private List<MenuItemModel>? menuItems;
        public MenuItemModel(MenuItemHandler? handler = null)
        {
            MenuItemEvent += handler;
        }

        [RelayCommand]
        private void MenuItem()
        {
            if (MenuItemEvent is not null)
                MenuItemEvent(this);
        }

        public delegate void MenuItemHandler(MenuItemModel item);
        public event MenuItemHandler? MenuItemEvent;
    }
}