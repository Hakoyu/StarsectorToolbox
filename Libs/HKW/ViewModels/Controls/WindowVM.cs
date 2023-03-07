using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HKW.ViewModels.Controls
{
    /// <summary>
    /// 窗口视图模型
    /// </summary>
    public partial class WindowVM : ObservableObject
    {
        [ObservableProperty]
        private object? _dataContext;
        partial void OnDataContextChanged(object? value)
        {
            _windowType.GetProperty(nameof(DataContext))?.SetValue(_window, value);
        }

        [ObservableProperty]
        private string? _title;

        [ObservableProperty]
        private object? _tag;

        private readonly object _window;

        private readonly Type _windowType;

        protected WindowVM() { }
        public WindowVM(object window)
        {
            _window = window;
            _windowType = window.GetType();
        }

        public void Show()
        {
            ShowEvent?.Invoke();
            _windowType.GetMethod(nameof(Show))?.Invoke(_window, null);
        }

        public void ShowDialog()
        {
            ShowDialogEvent?.Invoke();
            _windowType.GetMethod(nameof(ShowDialog))?.Invoke(_window, null);
        }

        public void Hide()
        {
            HideEvent?.Invoke();
            _windowType.GetMethod(nameof(Hide))?.Invoke(_window, null);
        }

        public void Close()
        {
            CloseEvent?.Invoke();
            _windowType.GetMethod(nameof(Close))?.Invoke(_window, null);
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
