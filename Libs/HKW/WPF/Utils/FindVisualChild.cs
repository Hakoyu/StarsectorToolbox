using System.Windows;
using System.Windows.Media;

namespace HKW.WPF
{
    /// <summary>
    /// 工具
    /// </summary>
    public class WPFUtils
    {
        /// <summary>
        /// 查找视觉树子项
        /// </summary>
        /// <typeparam name="TChild">子项类型</typeparam>
        /// <param name="obj">原始控件</param>
        /// <param name="childName"></param>
        /// <returns>成功找到返回子项,否则返回null</returns>
        public static TChild? FindVisualChild<TChild>(DependencyObject obj, string childName = "")
            where TChild : FrameworkElement
        {
            if (obj is null)
                return null;
            var count = VisualTreeHelper.GetChildrenCount(obj);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is TChild t && (string.IsNullOrWhiteSpace(childName) || t.Name == childName))
                    return t;
                if (FindVisualChild<TChild>(child) is TChild childItem)
                    return childItem;
            }
            return null;
        }
    }
}