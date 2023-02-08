using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>
    /// ST页面接口
    /// </summary>
    public interface ISTPage
    {
        /// <summary>
        /// 本地化名称
        /// </summary>
        public string NameI18n { get; }
        /// <summary>
        /// 本地化描述
        /// </summary>
        public string DescriptionI18n { get; }
        /// <summary>
        /// 需要保存
        /// </summary>
        public bool NeedSave { get; }
        /// <summary>
        /// 改变语言
        /// </summary>
        public void ChangeLanguage();
        /// <summary>
        /// 保存
        /// </summary>
        public void Save();
        /// <summary>
        /// 关闭
        /// </summary>
        public void Close();
    }
}
