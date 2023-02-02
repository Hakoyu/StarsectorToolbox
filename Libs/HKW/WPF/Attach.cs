using System.Windows;
using System.Windows.Input;

namespace HKW.WPF
{
    /// <summary>
    /// WPF附加属性
    /// <para>示例:
    /// <code lang="xaml">
    /// <![CDATA[
    /// <Setter Property="ex:InputBindingsAttach.InputBindings">
    ///   <Setter.Value>
    ///     <InputBindingCollection>
    ///       <!--  绑定单击事件  -->
    ///       <MouseBinding
    ///         Command="{Binding ItemClickCommand}"
    ///         CommandParameter="{Binding Tag, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type ListBoxItem}}}"
    ///         Gesture="LeftClick" />
    ///     </InputBindingCollection>
    ///    </Setter.Value>
    ///  </Setter>
    /// ]]>
    /// </code>
    /// </para>
    /// </summary>
    public static class Attach
    {
        /// <summary>
        /// 注册事件
        /// </summary>
        public static readonly DependencyProperty InputBindingsProperty =
            DependencyProperty.RegisterAttached("InputBindings", typeof(InputBindingCollection), typeof(Attach),
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
        public static InputBindingCollection GetInputBindings(UIElement element) =>
            (InputBindingCollection)element.GetValue(InputBindingsProperty);

        /// <summary>
        /// 设置输入绑定
        /// </summary>
        /// <param name="element">元素</param>
        /// <param name="inputBindings">输入绑定</param>
        public static void SetInputBindings(UIElement element, InputBindingCollection inputBindings) =>
            element.SetValue(InputBindingsProperty, inputBindings);
    }
}