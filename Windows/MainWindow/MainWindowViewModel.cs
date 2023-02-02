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
using static StarsectorTools.Windows.MainWindow.MainWindow;
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
        private ListBoxItemModel nowSelectedItem = null!;

        [ObservableProperty]
        private List<ListBoxItemModel> pageItems = new();

        public MainWindowViewModel()
        {
            pageItems.Add(new(SelectPageItem) { Content = "name", ToolTip = "tooltip", Icon = "😃" });
        }

        [RelayCommand]
        private void ChangeMenuExpansionStatus()
        {
            MainMenuIsExpand = !MainMenuIsExpand;
        }

        public void SelectPageItem(ListBoxItemModel item)
        {

        }
    }
}
