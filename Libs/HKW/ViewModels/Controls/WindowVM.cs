using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.ViewModels.Controls;

/// <summary>
/// 窗口视图模型
/// </summary>
public partial class WindowVM : ObservableObject
{
    [ObservableProperty]
    private object? _dataContext;

    partial void OnDataContextChanged(object? value)
    {
        r_dataContextProperty?.SetValue(r_window, value);
    }

    [ObservableProperty]
    private string? _title;

    [ObservableProperty]
    private object? _tag;

    private readonly object? r_window;

    private readonly Type? r_windowType;

    private readonly PropertyInfo? r_dataContextProperty;

    private readonly MethodInfo? r_showMethod;

    private readonly MethodInfo? r_showDialogMethod;

    private readonly MethodInfo? r_hideMethod;

    private readonly MethodInfo? r_closeMethod;

    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="window">窗口</param>
    public WindowVM(object window)
    {
        r_window = window;
        // 通过反射获取window的数据
        r_windowType = window.GetType();
        r_dataContextProperty = r_windowType.GetProperty(nameof(DataContext));
        r_showMethod = r_windowType.GetMethod(nameof(Show));
        r_showDialogMethod = r_windowType.GetMethod(nameof(ShowDialog));
        r_hideMethod = r_windowType.GetMethod(nameof(Hide));
        r_closeMethod = r_windowType.GetMethod(nameof(Close));
    }

    /// <summary>
    /// 显示
    /// </summary>
    public void Show()
    {
        ShowEvent?.Invoke();
        r_showMethod?.Invoke(r_window, null);
    }

    /// <summary>
    /// 显示对话框
    /// </summary>
    public void ShowDialog()
    {
        ShowDialogEvent?.Invoke();
        r_showDialogMethod?.Invoke(r_window, null);
    }

    /// <summary>
    /// 隐藏
    /// </summary>
    public void Hide()
    {
        HideEvent?.Invoke();
        r_hideMethod?.Invoke(r_window, null);
    }

    /// <summary>
    /// 关闭
    /// </summary>
    public void Close()
    {
        CloseEvent?.Invoke();
        r_closeMethod?.Invoke(r_window, null);
    }

    /// <summary>
    /// 委托
    /// </summary>
    public delegate void ViewModelHandler();

    /// <summary>
    /// 显示事件
    /// </summary>
    public event ViewModelHandler? ShowEvent;

    /// <summary>
    /// 显示对话框事件
    /// </summary>
    public event ViewModelHandler? ShowDialogEvent;

    /// <summary>
    /// 隐藏事件
    /// </summary>
    public event ViewModelHandler? HideEvent;

    /// <summary>
    /// 关闭事件
    /// </summary>
    public event ViewModelHandler? CloseEvent;
}