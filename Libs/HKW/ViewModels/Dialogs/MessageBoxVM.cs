using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.ViewModels.Dialogs;

/// <summary>
/// 消息窗口模型
/// </summary>
public partial class MessageBoxVM : ObservableObject
{
    /// <summary>
    /// 描述
    /// </summary>
    public partial class Description : ObservableObject
    {
        #region CheckBox

        /// <summary>启用复选框</summary>
        [ObservableProperty]
        private bool showCheckBox = false;

        /// <summary>复选框信息</summary>
        [ObservableProperty]
        private string? checkBoxMessage;

        /// <summary>复选框被点击</summary>
        [ObservableProperty]
        private bool checkBoxIsChecked = false;

        #endregion CheckBox

        #region TextBox

        /// <summary>启用文本框</summary>
        [ObservableProperty]
        private bool showTextBox = false;

        /// <summary>文本框内容</summary>
        [ObservableProperty]
        private bool textBoxText = false;

        #endregion TextBox

        /// <summary>拥有者</summary>
        public object? Owner { get; set; }

        /// <summary>标记</summary>
        public object? Tag { get; set; }

        /// <summary>标记</summary>
        public bool ShowMainWindowBlurEffect { get; set; } = true;

        /// <summary>消息</summary>
        public string Message { get; private set; }

        /// <summary>标题</summary>
        public string? Caption { get; set; }

        /// <summary>图标</summary>
        public Icon? Icon { get; set; }

        /// <summary>按钮</summary>
        public Button? Button { get; set; }

        /// <summary>默认按钮</summary>
        public Button? DefaultButton { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="message">消息</param>
        public Description(string message)
        {
            Message = message;
        }
    }

    /// <summary>模型图标</summary>
    public enum Icon
    {
        /// <summary>无</summary>
        None,

        /// <summary>信息</summary>
        Info,

        /// <summary>警告</summary>
        Warning,

        /// <summary>错误</summary>
        Error,

        /// <summary>成功</summary>
        Success,

        /// <summary>问题</summary>
        Question
    }

    /// <summary>模型按钮</summary>
    public enum Button
    {
        /// <summary>确认</summary>
        OK,

        /// <summary>确认和取消</summary>
        OKCancel,

        /// <summary>是和否</summary>
        YesNo,

        /// <summary>是否和取消</summary>
        YesNoCancel,
    }

    /// <summary></summary>
    public enum Result
    {
        /// <summary>空</summary>
        None,

        /// <summary>确认</summary>
        OK,

        /// <summary>取消</summary>
        Cancel,

        /// <summary>是</summary>
        Yes,

        /// <summary>否</summary>
        No
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private MessageBoxVM()
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
    /// 显示消息窗口
    /// </summary>
    /// <param name="description">描述</param>
    /// <returns>结果</returns>
    public static Result? Show(Description description) =>
        ViewModelEvent?.Invoke(description);

    /// <summary>
    /// 委托
    /// </summary>
    /// <param name="description">描述</param>
    public delegate Result ViewModelHandler(Description description);

    /// <summary>
    /// 事件
    /// </summary>
    private static event ViewModelHandler? ViewModelEvent;
}