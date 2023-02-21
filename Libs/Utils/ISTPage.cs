namespace StarsectorTools.Libs.Utils
{
    /// <summary>
    /// ST页面接口
    /// </summary>
    public interface ISTPage
    {
        /// <summary>
        /// 需要保存
        /// </summary>
        public bool NeedSave { get; }

        /// <summary>
        /// 保存
        /// </summary>
        public void Save();

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close();

        /// <summary>
        /// 本地化名称
        /// </summary>
        public string GetNameI18n();

        /// <summary>
        /// 本地化描述
        /// </summary>
        public string GetDescriptionI18n();
    }
}