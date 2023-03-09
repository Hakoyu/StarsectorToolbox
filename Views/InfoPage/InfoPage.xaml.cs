using System.Windows.Controls;
using StarsectorTools.ViewModels.InfoPage;

namespace StarsectorTools.Views.InfoPage
{
    /// <summary>
    /// Description.xaml 的交互逻辑
    /// </summary>
    internal partial class InfoPage : Page
    {
        internal InfoPageViewModel ViewModel => (InfoPageViewModel)DataContext;
        internal InfoPage()
        {
            InitializeComponent();
            DataContext = new InfoPageViewModel();
        }
    }
}