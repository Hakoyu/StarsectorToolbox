using System.Windows;
using StarsectorTools.Libs.Utils;

namespace StarsectorTools.Tools.ModManager
{
    /// <summary>
    /// ModArchiveing.xaml 的交互逻辑
    /// </summary>
    public partial class ModArchiveing : Window
    {
        public ModArchiveing()
        {
            Utils.SetMainWindowBlurEffect();
            InitializeComponent();
            Closed += (s, e) => Utils.RemoveMainWindowBlurEffect();
        }
    }
}