using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.Model
{
    /// <summary>
    /// 列表项模型,用于MVVM
    /// </summary>
    [DebuggerDisplay("{Id} {Content}")]
    public partial class ListBoxItemModel : ObservableObject
    {
        [ObservableProperty]
        private string? name;
        [ObservableProperty]
        private string? id;
        [ObservableProperty]
        private string? group;
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
        public ListBoxItemModel(ModelHandler? handler = null)
        {
            ModelEvent += handler;
        }
        [RelayCommand]
        private void ListBoxItem()
        {
            if (ModelEvent is not null)
                ModelEvent(this);
        }
        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="model">列表项模型</param>
        public delegate void ModelHandler(ListBoxItemModel model);
        /// <summary>
        /// 事件
        /// </summary>
        public event ModelHandler? ModelEvent;
    }
}