using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using StarsectorToolbox.Models.ST;
using StarsectorToolbox.ViewModels.GameSettings;
using I18nRes = StarsectorToolbox.Langs.Pages.GameSettings.GameSettingsPageI18nRes;

namespace StarsectorToolbox.Views.GameSettings;

/// <summary>
/// GameSettings.xaml 的交互逻辑
/// </summary>
internal partial class GameSettingsPage : Page, ISTPage
{
    internal GameSettingsPageViewModel ViewModel => (GameSettingsPageViewModel)DataContext;

    public bool NeedSave => false;

    /// <summary>
    ///
    /// </summary>
    public GameSettingsPage()
    {
        InitializeComponent();
        DataContext = new GameSettingsPageViewModel(true);
    }

    private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");

    public void Save()
    {
    }

    public void Close()
    {
    }

    public string GetNameI18n() => I18nRes.GameSettings;

    public string GetDescriptionI18n() => I18nRes.GameSettingsDescription;
}