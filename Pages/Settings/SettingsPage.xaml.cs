using System.Windows.Controls;

namespace StarsectorTools.Pages.Settings
{
    /// <summary>
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        internal SettingsPageViewModel ViewModel => (SettingsPageViewModel)DataContext;

        internal SettingsPage()
        {
            InitializeComponent();
            DataContext = new SettingsPageViewModel(true);
        }
    }
}