using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.Models.ControlModels
{
    /// <summary>
    /// 列表项模型,用于MVVM
    /// </summary>
    [DebuggerDisplay("{Name} {Content}")]
    public partial class ListBoxItemModel : ContentControlModel
    {
        [ObservableProperty]
        private string? group;
        [ObservableProperty]
        private object? icon;
        [ObservableProperty]
        private bool isSelected = false;
        /// <summary>
        /// 初始化
        /// </summary>
        public ListBoxItemModel() { }
    }
}