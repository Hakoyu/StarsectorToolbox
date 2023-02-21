using System;
using System.ComponentModel;

namespace HKW.ViewModels.Dialogs
{
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

    public class PendingVMHandler
    {
        public Action _showAction;
        public Action _hideAction;
        public Action _closeAction;
        public Action<string> _updateMessageAction;

        public PendingVMHandler(Action showAction, Action hideAction, Action closeAction, Action<string> updateMessageAction)
        {
            _showAction = showAction;
            _hideAction = hideAction;
            _closeAction = closeAction;
            _updateMessageAction = updateMessageAction;
        }

        public void Show()
        {
            _showAction();
        }

        public void Hide()
        {
            _hideAction();
        }

        public void Close()
        {
            _closeAction();
        }

        public void UpdateMessage(string message)
        {
            _updateMessageAction(message);
        }

        public event CancelEventHandler Cancelling;

        public event EventHandler Closed;
    }
}