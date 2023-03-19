using System.Diagnostics;

namespace HKW.ViewModels.Controls;

/// <summary>
/// 列表模型,用于MVVM
/// </summary>
[DebuggerDisplay("{Name},Count = {ItemsSource.Count}")]
public partial class ListBoxVM : SelectorVM<ListBoxItemVM>
{
    /// <summary>
    /// 构造
    /// </summary>
    public ListBoxVM()
    {
        ItemsSource ??= new();
    }
}