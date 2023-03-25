using System.Windows.Controls;
using StarsectorToolbox.ViewModels.Info;

namespace StarsectorToolbox.Views.Info;

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