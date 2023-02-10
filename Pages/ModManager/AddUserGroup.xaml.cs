using System.Windows;
using StarsectorTools.Libs.Utils;

namespace StarsectorTools.Pages.ModManager
{
    /// <summary>
    /// AddUserGroup.xaml 的交互逻辑
    /// </summary>
    public partial class AddUserGroup : Window
    {
        internal AddUserGroup()
        {
            Utils.SetMainWindowBlurEffect();
            InitializeComponent();
            Closed += (s, e) => Utils.RemoveMainWindowBlurEffect();
        }
    }
}