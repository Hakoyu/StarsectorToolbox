using System.Net.Http;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.HKWViewModels;
using HKW.HKWViewModels.Dialogs;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.ST;
using I18nRes = StarsectorToolbox.Langs.Pages.Info.InfoPageI18nRes;

namespace StarsectorToolbox.ViewModels.Info;

internal partial class InfoPageViewModel : ObservableObject
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    private string _currentVersion = ST.Version;

    [ObservableProperty]
    private string checkUpdateIcon = c_yesIcon;

    [ObservableProperty]
    private ObservableI18n<I18nRes> _i18n = ObservableI18n<I18nRes>.Create(new());

    public InfoPageViewModel() { }

    public InfoPageViewModel(bool noop) { }

    [RelayCommand]
    private static void OpenGitHub()
    {
        Utils.OpenLink("https://github.com/Hakoyu/StarsectorToolbox");
    }

    [RelayCommand]
    private async Task CheckUpdate()
    {
        CheckUpdateIcon = c_runIcon;
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36"
        );
        var response = await httpClient.GetAsync(
            "https://api.github.com/repos/Hakoyu/StarsectorToolbox/releases/latest"
        );
        var releases = await response.Content.ReadAsStringAsync();
        var tagName = Regex.Match(releases, @"(?<=""name"": "")[^""]+").Value;
        //if (string.IsNullOrWhiteSpace(tagName) is false)
        //{
        //    Logger.Info($"获取成功\n{tagName}");
        //    //Utils.ShowMessageBox($"获取成功\n最新版本: {tagName}");
        //    CheckUpdateIcon = c_yesIcon;
        //}
        //else
        //{
        //    Logger.Warring($"获取失败\n{releases}");
        //    //Utils.ShowMessageBox($"获取失败\n{releases}", MessageBoxImage.Warning);
        //    CheckUpdateIcon = c_noIcon;
        //}
        MessageBoxVM.Show(new($"当前版本:{ST.Version}\n最新版本:{tagName}"));
        //var downloadUrl = Regex.Match(releases, @"(?<=""browser_download_url"": "")[^""]+").Value;
        //var fileResponse = await httpClient.GetAsync(downloadUrl);
        //fileResponse.EnsureSuccessStatusCode();
        //using (var fs = File.Create(@"C:\Users\HKW\Desktop\1.zip"))
        //{
        //    fileResponse.Content.ReadAsStream().CopyTo(fs);
        //}
    }
}
