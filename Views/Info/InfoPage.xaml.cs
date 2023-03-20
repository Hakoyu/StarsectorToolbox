using System.Windows.Controls;
using StarsectorTools.ViewModels.Info;

namespace StarsectorTools.Views.Info;

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