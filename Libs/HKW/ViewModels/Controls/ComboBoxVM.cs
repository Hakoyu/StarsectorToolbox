using System;
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
    /// 组合框视图模型
    /// </summary>
    [DebuggerDisplay("{Name},Count = {ItemsSource.Count}")]
    public partial class ComboBoxVM : ItemsCollectionVM<ComboBoxItemVM>
    {
        [ObservableProperty]
        private int selectedIndex;
        [ObservableProperty]
        private ComboBoxItemVM? selectedItem;
        /// <summary>
        /// 构造
        /// </summary>
        public ComboBoxVM(ObservableCollection<ComboBoxItemVM>? collection = null)
        {
            ItemsSource = collection ?? new();
        }

        /// <summary>
        /// 选择改变
        /// </summary>
        /// <param name="parameter">参数</param>
        [RelayCommand]
        private void SelectionChanged(object parameter)
        {
            if (SelectionChangedEvent is not null)
                SelectionChangedEvent(parameter);
        }

        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="parameter">参数</param>
        public delegate void ViewModelHandler(object parameter);
        /// <summary>
        /// 选择改变事件
        /// </summary>
        public event ViewModelHandler? SelectionChangedEvent;
    }
}
