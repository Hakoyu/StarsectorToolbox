using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StarsectorTools.Windows.MainWindow
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool mainMenuExpand = false;
        public MainWindowViewModel()
        {

        }
        [RelayCommand]
        public void ChangeMenuExpansionStatus()
        {
            MainMenuExpand = !MainMenuExpand;
        }
    }
}
