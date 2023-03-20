using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Libs.Log4Cs;
using HKW.ViewModels;
using StarsectorTools.Libs.Utils;
using I18nRes = StarsectorTools.Langs.Pages.Info.InfoPageI18nRes;

namespace StarsectorTools.ViewModels.Info;

internal partial class InfoPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string checkUpdateIcon = "✅";

    [ObservableProperty]
    private ObservableI18n<I18nRes> _i18n = ObservableI18n<I18nRes>.Create(new());

    public InfoPageViewModel()
    {
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        Utils.OpenLink("https://github.com/Hakoyu/StarsectorTools");
    }

    [RelayCommand]
    private async Task CheckUpdate()
    {
        CheckUpdateIcon = "💫";
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36");
            var response = await httpClient.GetAsync("https://api.github.com/repos/Hakoyu/StarsectorTools/releases/latest");
            var releases = await response.Content.ReadAsStringAsync();
            var tagName = Regex.Match(releases, @"(?<=""name"": "")[^""]+").Value;
            if (string.IsNullOrWhiteSpace(tagName) is false)
            {
                Logger.Info($"获取成功\n{tagName}");
                //Utils.ShowMessageBox($"获取成功\n最新版本: {tagName}");
                CheckUpdateIcon = "✅";
            }
            else
            {
                Logger.Warring($"获取失败\n{releases}");
                //Utils.ShowMessageBox($"获取失败\n{releases}", MessageBoxImage.Warning);
                CheckUpdateIcon = "❎";
            }
            //var downloadUrl = Regex.Match(releases, @"(?<=""browser_download_url"": "")[^""]+").Value;
            //var fileResponse = await httpClient.GetAsync(downloadUrl);
            //fileResponse.EnsureSuccessStatusCode();
            //using (var fs = File.Create(@"C:\Users\HKW\Desktop\1.zip"))
            //{
            //    fileResponse.Content.ReadAsStream().CopyTo(fs);
            //}
        }
    }
}