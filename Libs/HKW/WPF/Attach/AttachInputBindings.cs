using System.Windows;
using System.Windows.Input;

namespace HKW.WPF.Attach
{
    /// <summary>
    /// WPF附加属性
    /// <para>示例:
    /// <code lang="xaml">
    /// <![CDATA[
    /// <Setter Property="attack:AttachInputBindings.Attach">
    ///   <Setter.Value>
    ///     <InputBindingCollection>
    ///       <!--  绑定单击事件  -->
    ///       <MouseBinding
    ///         Command="{Binding ItemClickCommand}"
    ///         Gesture="LeftClick" />
    ///     </InputBindingCollection>
    ///    </Setter.Value>
    ///  </Setter>
    /// ]]>
    /// </code>
    /// </para>
    /// </summary>
    public static class AttachInputBindings
    {
        /// <summary>
        /// 注册事件
        /// </summary>
        public static readonly DependencyProperty InputBindingsProperty =
            DependencyProperty.RegisterAttached("Attach", typeof(InputBindingCollection), typeof(AttachInputBindings),
            new FrameworkPropertyMetadata(new InputBindingCollection(),
            (sender, e) =>
            {
                if (sender is not UIElement element)
                    return;
                element.InputBindings.Clear();
                element.InputBindings.AddRange((InputBindingCollection)e.NewValue);
            }));
        /// <summary>
        /// 获取输入绑定
        /// </summary>
        /// <param name="element">元素</param>
        /// <returns></returns>
        public static InputBindingCollection GetAttach(UIElement element) =>
            (InputBindingCollection)element.GetValue(InputBindingsProperty);

        /// <summary>
        /// 设置输入绑定
        /// </summary>
        /// <param name="element">元素</param>
        /// <param name="inputBindings">输入绑定</param>
        public static void SetAttach(UIElement element, InputBindingCollection inputBindings) =>
            element.SetValue(InputBindingsProperty, inputBindings);
    }
}