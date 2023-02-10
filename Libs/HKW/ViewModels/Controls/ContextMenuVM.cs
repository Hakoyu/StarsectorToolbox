using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.ViewModels.Controls
{
    /// <summary>
    /// 上下文菜单模型,用于MVVM
    /// </summary>
    [DebuggerDisplay("{Name}, Count = {ItemsSource.Count}")]
    public partial class ContextMenuVM : ItemsCollectionVM<MenuItemVM>
    {
        /// <summary>
        /// 已打开
        /// </summary>
        [ObservableProperty]
        private bool isOpen = false;
        /// <summary>
        /// 已加载
        /// </summary>
        [ObservableProperty]
        private bool isLoaded = false;
        /// <summary>
        /// 初始化
        /// <para>
        /// 如果需要使用延迟加载模式 
        /// <c>Binding</c> 
        /// 需要设置为 
        /// <c>ContextMenuVM.ItemsSource</c> 
        /// 而不是 
        /// <c>ContextMenuVM</c> 
        /// </para>
        /// </summary>
        /// <param name="handler">委托</param>
        public ContextMenuVM(LoadedHandler? handler = null)
        {
            ItemsSource = new();
            if (handler is not null)
                LoadedEvent += handler;
        }

        /// <summary>
        /// 加载命令
        /// </summary>
        /// <param name="parameter">参数</param>
        [RelayCommand]
        private void Loaded(object parameter)
        {
            if (LoadedEvent is not null && IsLoaded is false)
            {
                LoadedEvent(ItemsSource);
                IsLoaded = true;
            }
        }

        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="items">参数</param>
        public delegate void LoadedHandler(ObservableCollection<MenuItemVM> items);
        /// <summary>
        /// 事件
        /// </summary>
        public event LoadedHandler? LoadedEvent;
    }
}
