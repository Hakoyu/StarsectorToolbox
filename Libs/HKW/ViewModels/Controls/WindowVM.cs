using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.ViewModels.Controls
{
    /// <summary>
    /// 窗口视图模型
    /// </summary>
    public partial class WindowVM : ObservableObject
    {
        [ObservableProperty]
        private object? _dataContext;

        private partial void OnDataContextChanged(object? value)
        {
            _dataContextProperty?.SetValue(_window, value);
        }

        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private object? _tag;

        private readonly object? _window;

        private readonly Type? _windowType;

        private readonly PropertyInfo? _dataContextProperty;

        private readonly MethodInfo? _showMethod;

        private readonly MethodInfo? _showDialogMethod;

        private readonly MethodInfo? _hideMethod;

        private readonly MethodInfo? _closeMethod;

        /// <summary>
        /// 构造
        /// </summary>
        protected WindowVM()
        { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="window">窗口</param>
        public WindowVM(object window)
        {
            _window = window;
            // 通过反射获取window的数据
            _windowType = window.GetType();
            _dataContextProperty = _windowType.GetProperty(nameof(DataContext));
            _showMethod = _windowType.GetMethod(nameof(Show));
            _showDialogMethod = _windowType.GetMethod(nameof(ShowDialog));
            _hideMethod = _windowType.GetMethod(nameof(Hide));
            _closeMethod = _windowType.GetMethod(nameof(Close));
        }

        /// <summary>
        /// 显示
        /// </summary>
        public void Show()
        {
            ShowEvent?.Invoke();
            _showMethod?.Invoke(_window, null);
        }

        /// <summary>
        /// 显示对话框
        /// </summary>
        public void ShowDialog()
        {
            ShowDialogEvent?.Invoke();
            _showDialogMethod?.Invoke(_window, null);
        }

        /// <summary>
        /// 隐藏
        /// </summary>
        public void Hide()
        {
            HideEvent?.Invoke();
            _hideMethod?.Invoke(_window, null);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            CloseEvent?.Invoke();
            _closeMethod?.Invoke(_window, null);
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
}