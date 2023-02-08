using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.Models.ControlModels
{
    /// <summary>
    /// 上下文菜单模型,用于MVVM
    /// </summary>
    [DebuggerDisplay("{Name}, Count = {ItemsSource.Count}")]
    public partial class ContextMenuModel : ItemsCollectionModel<MenuItemModel>
    {
        /// <summary>
        /// 初始化
        /// </summary>
        public ContextMenuModel()
        {
            ItemsSource = new List<MenuItemModel>();
        }
    }
}
