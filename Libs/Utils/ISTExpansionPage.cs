using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>
    /// 拓展页面接口
    /// </summary>
    public interface ISTExpansionPage : ISTPage
    {
        /// <summary>
        /// 本地化名称
        /// </summary>
        public string NameI18n { get; set; }
        /// <summary>
        /// 本地化描述
        /// </summary>
        public string DescriptionI18n { get; set; }
    }
}
