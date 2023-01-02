using System.Windows;
using StarsectorTools.Utils;

namespace StarsectorTools.Tools.ModManager
{
    /// <summary>
    /// ModArchiveing.xaml 的交互逻辑
    /// </summary>
    public partial class ModArchiveing : Window
    {
        public ModArchiveing()
        {
            ST.SetMainWindowBlurEffect();
            InitializeComponent();
            Closed += (s, e) => ST.RemoveMainWIndowBlurEffect();
        }
    }
}