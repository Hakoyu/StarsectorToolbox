using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.ViewModels.Controls
{
    /// <summary>
    /// 菜单项模型,用于MVVM
    /// </summary>
    [DebuggerDisplay("{Name},Header = {Header},Count = {ItemsSource.Count}")]
    public partial class MenuItemVM : HeaderedItemsControlVM<MenuItemVM>
    {
        [ObservableProperty]
        private object? icon;
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="handler">委托</param>
        public MenuItemVM(ModelHandler? handler = null)
        {
            ModelEvent += handler;
        }
        [RelayCommand]
        private void MenuItem(object parameter)
        {
            if (ModelEvent is not null)
                ModelEvent(parameter);
        }
        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="parameter">参数</param>
        public delegate void ModelHandler(object parameter);
        /// <summary>
        /// 事件
        /// </summary>
        public event ModelHandler? ModelEvent;
    }
}