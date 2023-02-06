using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using StarsectorTools.Libs.Utils;

namespace StarsectorTools.Pages
{
    /// <summary>
    /// Description.xaml 的交互逻辑
    /// </summary>
    public partial class InfoPage : Page
    {
        internal InfoPage()
        {
            InitializeComponent();
        }

        private void Hyperlink_GitHub_Click(object sender, RoutedEventArgs e)
        {
            Utils.OpenLink("https://github.com/Hakoyu/StarsectorTools");
        }

        private async void Button_CheckUpdate_ClickAsync(object sender, RoutedEventArgs e)
        {
            TextBlock_CheckUpdateIcon.Text = "💫";
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36");
                var response = await httpClient.GetAsync("https://api.github.com/repos/Hakoyu/StarsectorTools/releases/latest");
                var releases = await response.Content.ReadAsStringAsync();
                var tagName = Regex.Match(releases, @"(?<=""name"": "")[^""]+").Value;
                if (!string.IsNullOrEmpty(tagName))
                {
                    STLog.WriteLine($"获取成功\n{tagName}");
                    //Utils.ShowMessageBox($"获取成功\n最新版本: {tagName}");
                    TextBlock_CheckUpdateIcon.Text = "✅";
                }
                else
                {
                    STLog.WriteLine($"获取失败\n{releases}", STLogLevel.WARN);
                    //Utils.ShowMessageBox($"获取失败\n{releases}", MessageBoxImage.Warning);
                    TextBlock_CheckUpdateIcon.Text = "❎";
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
}