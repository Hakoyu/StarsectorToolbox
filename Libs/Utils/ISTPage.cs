using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HKW.Extension.SetExtension;

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
        /// 本地化兼容集合
        /// </summary>
        public ReadOnlySet<string> I18nSet { get; }
        /// <summary>
        /// 改变语言
        /// </summary>
        public bool ChangeLanguage();
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
