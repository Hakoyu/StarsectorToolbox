using System.Windows;
using System.Windows.Controls;

namespace HKW.WPF.Attach
{
    /// <summary>
    /// 附加上下文菜单
    /// </summary>
    public class AttackFrameworkElement
    {
        private static FrameworkElement fe = null!;

        /// <summary>
        /// 注册事件
        /// </summary>
        public static readonly DependencyProperty FrameworkElementProperty =
            DependencyProperty.RegisterAttached("Attach", typeof(FrameworkElement), typeof(AttackFrameworkElement),
            new UIPropertyMetadata(fe,
            (sender, e) =>
            {
                if (sender is not FrameworkElement element)
                    return;
                var contextMenu = e.NewValue as ContextMenu;
                if (element != null && element.DataContext == null && element.GetBindingExpression(FrameworkElement.DataContextProperty) == null)
                {
                    element.DataContext = element.DataContext;
                }
            }));

        /// <summary>
        /// 获取输入绑定
        /// </summary>
        /// <param name="element">元素</param>
        /// <returns></returns>
        public static FrameworkElement GetAttach(UIElement element) =>
            (FrameworkElement)element.GetValue(FrameworkElementProperty);

        /// <summary>
        /// 设置输入绑定
        /// </summary>
        /// <param name="element">元素</param>
        /// <param name="contextMenu">输入绑定</param>
        public static void SetAttach(UIElement element, FrameworkElement contextMenu) =>
            element.SetValue(FrameworkElementProperty, contextMenu);
    }
}