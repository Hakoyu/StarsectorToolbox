using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.Models.ControlModels
{
    /// <summary>
    /// 基础控件模型
    /// </summary>
    [DebuggerDisplay("{Name} {TagDictionary.Count}")]
    public partial class ControlModelBase : ObservableObject
    {
        /// <summary>
        /// Id
        /// </summary>
        [ObservableProperty]
        private string? id;
        /// <summary>
        /// 名称
        /// </summary>
        [ObservableProperty]
        private string? name;
        /// <summary>
        /// 标签
        /// </summary>
        [ObservableProperty]
        private object? tag;
        /// <summary>
        /// 提示
        /// </summary>
        [ObservableProperty]
        private object? toolTip;
        /// <summary>
        /// 上下文菜单
        /// </summary>
        [ObservableProperty]
        private ContextMenuModel? contextMenu;
        /// <summary>
        /// 标签字典
        /// </summary>
        public Dictionary<string, object>? TagDictionary { get; set; }
    }
}
