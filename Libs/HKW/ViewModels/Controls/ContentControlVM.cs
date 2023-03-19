using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.ViewModels.Controls;

/// <summary>
/// 可包含任意类型内容的控件模型
/// </summary>
[DebuggerDisplay("{Name}, Content = {Content}")]
public partial class ContentControlVM : ControlVMBase
{
    [ObservableProperty]
    private object? icon;

    [ObservableProperty]
    private object? content;
}