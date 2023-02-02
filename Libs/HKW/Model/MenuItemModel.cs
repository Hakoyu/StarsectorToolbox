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
        private string? id;
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
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="handler">委托</param>
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
        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="item">菜单项模型</param>
        public delegate void MenuItemHandler(MenuItemModel item);
        /// <summary>
        /// 事件
        /// </summary>
        public event MenuItemHandler? MenuItemEvent;
    }
}