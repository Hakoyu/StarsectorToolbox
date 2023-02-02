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

        public delegate void ListBoxItemHandler(ListBoxItemModel item);
        public event ListBoxItemHandler? ListBoxItemEvent;
    }
}