using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.ViewModels.Controls;

/// <summary>
/// 按钮视图模型
/// </summary>
[DebuggerDisplay("{Name},Content = {Content}")]
public partial class ButtonVM : ContentControlVM
{
    /// <summary>
    /// 构造
    /// </summary>
    public ButtonVM()
    {
    }

    [ObservableProperty]
    private bool canExecute = true;

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private void Button(object parameter) => CommandEvent?.Invoke(parameter);

    /// <summary>
    /// 委托
    /// </summary>
    /// <param name="parameter">参数</param>
    public delegate void ViewModelHandler(object parameter);

    /// <summary>
    /// 选择改变事件
    /// </summary>
    public event ViewModelHandler? CommandEvent;
}