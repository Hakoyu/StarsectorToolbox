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
    /// 带多个项并且具有标题的控件模型
    /// </summary>
    [DebuggerDisplay("{Name}, Header = {Header},Count = {ItemsSource.Count}")]
    public partial class HeaderedItemsControl<T> : ItemsCollectionModel<T>
    {
        /// <summary>
        /// 标题
        /// </summary>
        [ObservableProperty]
        private object? header;
    }
}
