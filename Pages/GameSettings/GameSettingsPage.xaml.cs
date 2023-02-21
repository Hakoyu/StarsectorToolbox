using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace StarsectorTools.Pages.GameSettings
{
    /// <summary>
    /// GameSettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class GameSettingsPage : Page
    {
        internal GameSettingsPageViewModel ViewModel => (GameSettingsPageViewModel)DataContext;

        /// <summary>
        ///
        /// </summary>
        public GameSettingsPage()
        {
            InitializeComponent();
            DataContext = new GameSettingsPageViewModel(true);
        }

        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
    }
}