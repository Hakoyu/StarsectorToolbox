using System.Windows;
using StarsectorTools.Libs.Utils;

namespace StarsectorTools.Views.ModManagerPage
{
    /// <summary>
    /// AddUserGroupWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AddUserGroupWindow : Window
    {
        internal AddUserGroupWindow()
        {
            Utils.SetMainWindowBlurEffect();
            InitializeComponent();
            Closed += (s, e) => Utils.RemoveMainWindowBlurEffect();
        }
    }
}