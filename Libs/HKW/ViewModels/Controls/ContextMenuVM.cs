using System;
using System.Collections;
using System.Collections.Generic;
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
        /// </summary>
        /// <param name="handler">委托</param>
        public ContextMenuVM(LoadedHandler? handler = null)
        {
            ItemsSource = new List<MenuItemVM>();
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
            // 被加载后无法显示,需要再次右键,故暂不使用
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
        public delegate void LoadedHandler(IList<MenuItemVM> items);
        /// <summary>
        /// 事件
        /// </summary>
        public event LoadedHandler? LoadedEvent;
    }
}
