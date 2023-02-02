using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.Model
{
    /// <summary>
    /// 页面模型
    /// </summary>
    public partial class PageModel : ObservableObject
    {
        [ObservableProperty]
        private object? page;
        [ObservableProperty]
        private string? pageName;
        public PageModel()
        {

        }
    }
}
