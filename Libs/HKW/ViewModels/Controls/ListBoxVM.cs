using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.ViewModels.Controls
{
    /// <summary>
    /// 列表模型,用于MVVM
    /// </summary>
    [DebuggerDisplay("{Name},Count = {ItemsSource.Count}")]
    public partial class ListBoxVM : ItemsCollectionVM<ListBoxItemVM>
    {
        [ObservableProperty]
        private int selectedIndex;
        [ObservableProperty]
        private ListBoxItemVM? selectedItem;
        /// <summary>
        /// 构造
        /// </summary>
        public ListBoxVM()
        {
            ItemsSource = new();
        }
    }
}
