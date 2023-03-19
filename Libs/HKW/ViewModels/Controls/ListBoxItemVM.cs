using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.ViewModels.Controls;

/// <summary>
/// 列表项视图模型
/// </summary>
[DebuggerDisplay("{Name}, Content = {Content}")]
public partial class ListBoxItemVM : ContentControlVM
{
    [ObservableProperty]
    private string? group;

    [ObservableProperty]
    private bool isSelected = false;

    /// <summary>
    /// 初始化
    /// </summary>
    public ListBoxItemVM()
    { }
}