using System.Windows;
using StarsectorTools.Libs.Utils;

namespace StarsectorTools.Views.ModManagerPage
{
    /// <summary>
    /// ModArchiveingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ModArchiveingWindow : Window
    {
        internal ModArchiveingWindow()
        {
            Utils.SetMainWindowBlurEffect();
            InitializeComponent();
            Closed += (s, e) => Utils.RemoveMainWindowBlurEffect();
        }
    }
}