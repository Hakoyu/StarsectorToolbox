using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.Model
{
    /// <summary>
    /// 列表项模型,用于MVVM
    /// </summary>
    public partial class ListBoxItemModel : ObservableObject
    {
        [ObservableProperty]
        private string? id;
        [ObservableProperty]
        private object? content;
        [ObservableProperty]
        private object? toolTip;
        [ObservableProperty]
        private object? icon;
        [ObservableProperty]
        private object? tag;
        [ObservableProperty]
        private bool isSelected = false;
        [ObservableProperty]
        private List<MenuItemModel>? menuItems;
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="handler">委托</param>
        public ListBoxItemModel(ListBoxItemHandler? handler = null)
        {
            ListBoxItemEvent += handler;
        }
        [RelayCommand]
        private void ListBoxItem()
        {
            if (ListBoxItemEvent is not null)
                ListBoxItemEvent(this);
        }

        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="item">列表项模型</param>
        public delegate void ListBoxItemHandler(ListBoxItemModel item);
        /// <summary>
        /// 事件
        /// </summary>
        public event ListBoxItemHandler? ListBoxItemEvent;
    }
}