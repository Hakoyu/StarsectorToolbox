using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.ViewModels.Controls
{
    /// <summary>
    /// 选择器视图模型
    /// </summary>
    /// <typeparam name="TItem">项目类型</typeparam>
    public partial class SelectorVM<TItem> : ItemsCollectionVM<TItem>
    {
        /// <summary>
        /// 选中项的索引
        /// </summary>
        [ObservableProperty]
        private int selectedIndex;
        /// <summary>
        /// 选中项的值
        /// </summary>
        [ObservableProperty]
        private TItem? selectedItem;

        /// <summary>
        /// 选中项改变命令
        /// </summary>
        /// <param name="item">选中项</param>
        [RelayCommand]
        private void SelectionChanged(TItem item) => SelectionChangedEvent?.Invoke(item);

        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="item">参数</param>
        public delegate void ViewModelHandler(TItem item);
        /// <summary>
        /// 选中项选择改变事件
        /// </summary>
        public event ViewModelHandler? SelectionChangedEvent;
    }
}
