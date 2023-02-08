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
    /// 带多个项并且具有标题的控件模型
    /// </summary>
    [DebuggerDisplay("{Name} {Header} {ItemsSource.Count}")]
    public partial class HeaderedItemsControl<T> : ItemsCollectionModel<T>
    {
        /// <summary>
        /// 标题
        /// </summary>
        [ObservableProperty]
        private object? header;
    }
}
