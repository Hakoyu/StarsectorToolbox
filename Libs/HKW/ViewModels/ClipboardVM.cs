using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HKW.ViewModels
{
    /// <summary>
    /// 剪切板视图模型
    /// </summary>
    public class ClipboardVM
    {
        /// <summary>
        /// 初始化
        /// </summary>
        private ClipboardVM() { }
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

        public static void SetText(string text)
        {
            ViewModelEvent?.Invoke(text);
        }

        /// <summary>
        /// 委托
        /// </summary>
        public delegate void ViewModelHandler(string str);
        /// <summary>
        /// 事件
        /// </summary>
        private static event ViewModelHandler? ViewModelEvent;
    }
}
