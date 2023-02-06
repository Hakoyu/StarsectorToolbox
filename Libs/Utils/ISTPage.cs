using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
    }
}
