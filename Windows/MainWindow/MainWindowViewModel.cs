using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Model;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows.MainWindow
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// 主菜单展开状态
        /// </summary>
        [ObservableProperty]
        private bool mainMenuIsExpand = false;
        [ObservableProperty]
        private string title = I18n.StarsectorTools;
        [ObservableProperty]
        private ListBoxItemModel? nowSelectedItem;
        [ObservableProperty]
        private object? nowPage;

        [ObservableProperty]
        private List<ListBoxItemModel> pageItems = new();

        public MainWindowViewModel()
        {
            //PageItems.Add(new(SelectPageItem)
            //{
            //    Content = "name",
            //    ToolTip = "tooltip",
            //    Icon = "😃"
            //});
        }

        [RelayCommand]
        private void ChangeMenuExpansionStatus()
        {
            MainMenuIsExpand = !MainMenuIsExpand;
        }

        public void AddPage(string icon, string name, string id, string toolTip, object page)
        {
            PageItems.Add(new(SelectPageItem)
            {
                Id = id,
                Icon = icon,
                Content = name,
                ToolTip = toolTip,
                Tag = page
            });
        }

        public void SelectPageItem(ListBoxItemModel item)
        {
            if (nowSelectedItem is not null)
                nowSelectedItem.IsSelected = false;
            nowSelectedItem = item;
            nowSelectedItem.IsSelected = true;
            NowPage = item.Tag;
        }
    }
}
