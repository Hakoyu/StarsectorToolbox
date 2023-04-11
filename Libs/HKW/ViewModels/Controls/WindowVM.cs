using System;
using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
    private readonly MethodInfo? r_closingMethod;
    private readonly MethodInfo? r_closedMethod;

    private readonly EventInfo? r_closingEvent;
    private readonly EventInfo? r_closedEvent;
    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="window">窗口</param>
    public WindowVM(object window)
    {
        r_window = window;
        // 通过反射获取window的数据
        r_windowType = window.GetType();
        // Property
        r_dataContextProperty = r_windowType.GetProperty(nameof(DataContext));
        // Method
        r_showMethod = r_windowType.GetMethod(nameof(Show));
        r_showDialogMethod = r_windowType.GetMethod(nameof(ShowDialog));
        r_hideMethod = r_windowType.GetMethod(nameof(Hide));
        r_closeMethod = r_windowType.GetMethod(nameof(Close));
        // Event
        r_closingEvent = r_windowType.GetEvent(nameof(Closing));
        r_closingMethod = r_closingEvent?.GetRaiseMethod();
        r_closedEvent = r_windowType.GetEvent(nameof(Closed));
        r_closedMethod = r_closedEvent?.GetRaiseMethod();
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
    [RelayCommand]
    public void Close()
    {
        CancelEventArgs cancelEventArgs = new();
        OnClosing(cancelEventArgs);
        if (cancelEventArgs.Cancel)
            return;
        r_closeMethod?.Invoke(r_window, null);
        EventArgs eventArgs = new();
        OnClosed(eventArgs);
    }
    #region Close
    /// <summary>
    /// 关闭时
    /// </summary>
    /// <param name="e">事件参数</param>
    protected virtual void OnClosing(CancelEventArgs e) { }

    /// <summary>
    /// 关闭后
    /// </summary>
    protected virtual void OnClosed(EventArgs e) { }
    private event CancelEventHandler? ClosingEvent;
    /// <summary>
    /// 关闭时事件
    /// </summary>
    public event CancelEventHandler? Closing
    {
        add
        {
            ClosingEvent += value;
            r_closingEvent?.AddEventHandler(r_window, ClosingEvent);
        }
        remove
        {
            r_closingEvent?.RemoveEventHandler(r_window, ClosingEvent);
            ClosingEvent -= value;
        }
    }
    private event EventHandler? ClosedEvent;
    /// <summary>
    /// 关闭后事件
    /// </summary>
    public event EventHandler? Closed
    {
        add
        {
            ClosedEvent += value;
            r_closedEvent?.AddEventHandler(r_window, ClosedEvent);
        }
        remove
        {
            r_closedEvent?.RemoveEventHandler(r_window, ClosedEvent);
            ClosedEvent -= value;
        }
    }
    #endregion

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
}