using System.Windows;
using StarsectorTools.Utils;

namespace StarsectorTools.Tools.ModManager
{
    /// <summary>
    /// AddUserGroup.xaml 的交互逻辑
    /// </summary>
    public partial class AddUserGroup : Window
    {
        public AddUserGroup()
        {
            ST.SetMainWindowBlurEffect();
            InitializeComponent();
            Closed += (s, e) => ST.RemoveMainWIndowBlurEffect();
        }
    }
}