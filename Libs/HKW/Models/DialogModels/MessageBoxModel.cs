namespace HKW.Models.DialogModels
{
    /// <summary>
    /// 消息窗口模型
    /// </summary>
    public class MessageBoxModel
    {
        /// <summary>
        /// 描述
        /// </summary>
        public class Description
        {
            /// <summary>拥有者</summary>
            public object? Owner { get; set; }
            /// <summary>标记</summary>
            public object? Tag { get; set; }
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
        private MessageBoxModel() { }
        /// <summary>
        /// 初始化委托
        /// 单例模式,只能设置一次
        /// </summary>
        /// <param name="handler">委托</param>
        /// <returns>设置成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool InitializeHandler(ModelHandler handler)
        {
            if (ModelEvent is null)
            {
                ModelEvent += handler;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 显示消息窗口
        /// </summary>
        /// <param name="description">描述</param>
        /// <returns>结果</returns>
        public static Result? Show(Description description)
        {
            if (ModelEvent is not null)
                return ModelEvent(description);
            return null;
        }
        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="description">描述</param>
        public delegate Result ModelHandler(Description description);
        /// <summary>
        /// 事件
        /// </summary>
        private static event ModelHandler? ModelEvent;
    }
}
