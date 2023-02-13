﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HKW.ViewModels.Controls
{
    /// <summary>
    /// 按钮视图模型
    /// </summary>
    [DebuggerDisplay("{Name},Content = {Content}")]
    public partial class ButtonVM : ContentControlVM
    {
        /// <summary>
        /// 构造
        /// </summary>
        public ButtonVM()
        {

        }
        /// <summary>
        /// 委托
        /// </summary>
        /// <param name="parameter">参数</param>
        public delegate void ViewModelHandler(object parameter);
        /// <summary>
        /// 选择改变事件
        /// </summary>
        public event ViewModelHandler? ClickEvent;
    }
}