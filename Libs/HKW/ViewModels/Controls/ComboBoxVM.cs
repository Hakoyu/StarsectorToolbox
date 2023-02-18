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
    public partial class ComboBoxVM : SelectorVM<ComboBoxItemVM>
    {
        /// <summary>
        /// 构造
        /// </summary>
        public ComboBoxVM()
        {
            ItemsSource ??= new();
        }
    }
}
