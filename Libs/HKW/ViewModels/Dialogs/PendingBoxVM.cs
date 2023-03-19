using System;

namespace HKW.ViewModels.Dialogs;

/// <summary>
/// 等待消息弹窗
/// </summary>
public class PendingBoxVM
{
    private PendingBoxVM()
    { }

    /// <summary>
    /// 初始化委托
    /// 单例模式,只能设置一次
    /// </summary>
    /// <param name="handler">委托</param>
    /// <returns>设置成功为<see langword="true"/>,失败为<see langword="false"/></returns>
    public static bool InitializeHandler(ViewModelHandler handler)
    {
        if (ViewModelEvent is null)
        {
            ViewModelEvent += handler;
            return true;
        }
        return false;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static PendingVMHandler Show(string message) =>
        ViewModelEvent!.Invoke(message, string.Empty, false);

    /// <summary>
    ///
    /// </summary>
    /// <param name="message"></param>
    /// <param name="canCancel"></param>
    /// <returns></returns>
    public static PendingVMHandler Show(string message, bool canCancel) =>
        ViewModelEvent!.Invoke(message, string.Empty, canCancel);

    /// <summary>
    ///
    /// </summary>
    /// <param name="message"></param>
    /// <param name="caption"></param>
    /// <returns></returns>
    public static PendingVMHandler Show(string message, string caption) =>
        ViewModelEvent!.Invoke(message, caption, false);

    /// <summary>
    ///
    /// </summary>
    /// <param name="message"></param>
    /// <param name="caption"></param>
    /// <param name="canCancel"></param>
    /// <returns></returns>
    public static PendingVMHandler Show(string message, string caption, bool canCancel) =>
        ViewModelEvent!.Invoke(message, caption, canCancel);

    /// <summary>
    /// 委托
    /// </summary>
    public delegate PendingVMHandler ViewModelHandler(string message, string caption, bool canCancel);

    /// <summary>
    /// 事件
    /// </summary>
    private static event ViewModelHandler? ViewModelEvent;
}

/// <summary>
/// 等待消息弹窗处理器
/// </summary>
public class PendingVMHandler : IDisposable
{
    private Action _showAction;
    private Action _hideAction;
    private Action _closeAction;
    private Action<string> _updateMessageAction;

    internal PendingVMHandler(Action showAction, Action hideAction, Action closeAction, Action<string> updateMessageAction)
    {
        _showAction = showAction;
        _hideAction = hideAction;
        _closeAction = closeAction;
        _updateMessageAction = updateMessageAction;
    }

    /// <summary>
    /// 显示窗口
    /// </summary>
    public void Show()
    {
        _showAction();
    }

    /// <summary>
    /// 隐藏窗口
    /// </summary>
    public void Hide()
    {
        _hideAction();
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    public void Close()
    {
        _closeAction();
    }

    /// <summary>
    /// 更新消息
    /// </summary>
    /// <param name="message">消息</param>
    public void UpdateMessage(string message)
    {
        _updateMessageAction(message);
    }

    /// <summary>
    /// 回收
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    ///// <summary>
    ///// 取消事件
    ///// </summary>
    //public event CancelEventHandler? Cancelling;

    ///// <summary>
    ///// 关闭事件
    ///// </summary>
    //public event EventHandler? Closed;
}