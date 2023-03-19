namespace HKW.ViewModels.Dialogs;

/// <summary>
/// 打开文件对话框
/// </summary>
public class OpenFileDialogVM
{
    /// <summary>
    /// 描述
    /// </summary>
    public class Description : DescriptionBase
    {
        /// <summary>多选</summary>
        public bool Multiselect { get; set; } = false;
    }

    private OpenFileDialogVM()
    { }

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
    /// 显示对话框
    /// </summary>
    /// <param name="description">描述</param>
    /// <returns>选中的文件(或文件夹)</returns>
    public static string[]? Show(Description description)
    {
        if (ModelEvent is not null)
            return ModelEvent(description);
        return null;
    }

    /// <summary>
    /// 委托
    /// </summary>
    /// <param name="description">描述</param>
    /// <returns>选中的文件(或文件夹)</returns>
    public delegate string[] ModelHandler(Description description);

    /// <summary>
    /// 事件
    /// </summary>
    private static event ModelHandler? ModelEvent;
}