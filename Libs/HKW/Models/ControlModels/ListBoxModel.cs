using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.Models.ControlModels
{
    /// <summary>
    /// 列表模型,用于MVVM
    /// </summary>
    [DebuggerDisplay("{Name} {ItemsSource.Count}")]
    public partial class ListBoxModel : ItemsCollectionModel<ListBoxItemModel>
    {
        [ObservableProperty]
        private int selectedIndex;
        [ObservableProperty]
        private ListBoxItemModel? selectedItem;
        /// <summary>
        /// 构造
        /// </summary>
        public ListBoxModel()
        {
            ItemsSource = new List<ListBoxItemModel>();
        }
    }
}
