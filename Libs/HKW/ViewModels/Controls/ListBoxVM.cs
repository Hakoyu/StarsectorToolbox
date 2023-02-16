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
    /// 列表模型,用于MVVM
    /// </summary>
    [DebuggerDisplay("{Name},Count = {ItemsSource.Count}")]
    public partial class ListBoxVM : SelectorVM<ListBoxItemVM>
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="collection">初始集合</param>
        public ListBoxVM(ObservableCollection<ListBoxItemVM>? collection = null)
        {
            ItemsSource = collection ?? new();
        }
    }
}
