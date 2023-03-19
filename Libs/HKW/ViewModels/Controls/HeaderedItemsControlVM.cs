using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.ViewModels.Controls;

/// <summary>
/// 带多个项并且具有标题的控件模型
/// </summary>
[DebuggerDisplay("{Name}, Header = {Header},Count = {ItemsSource.Count}")]
public partial class HeaderedItemsControlVM<T> : ItemsCollectionVM<T>
{
    /// <summary>
    /// 标题
    /// </summary>
    [ObservableProperty]
    private object? header;
}