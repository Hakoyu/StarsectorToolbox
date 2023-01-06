using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Tools.GameSettings.GameSettings_I18n;

namespace StarsectorTools.Tools.GameSettings
{
    public partial class GameSettings
    {
        private struct VmparamsData
        {
            public string data;
            public string xmsx;
        }

        private VmparamsData vmparamsData = new();
        private string gameKey = "";
        private string hideGameKey = "";
        private bool showKey = false;
        private int systemTotalMemory = 0;
        private string gameSettingsFile = $"{GameInfo.Directory}\\starsector-core\\data\\config\\settings.json";

        private void GetVmparamsData()
        {
            vmparamsData.data = File.ReadAllText($"{GameInfo.Directory}\\vmparams");
            vmparamsData.xmsx = Regex.Match(vmparamsData.data, @"(?<=-xm[sx])[0-9]+[mg]", RegexOptions.IgnoreCase).Value;
            TextBox_Memory.Text = TextBox_Memory.Text = vmparamsData.xmsx;
        }

        private void GetGameKey()
        {
            var key = Registry.CurrentUser.OpenSubKey("Software\\JavaSoft\\Prefs\\com\\fs\\starfarer");
            if (key != null)
            {
                var serialKey = key.GetValue("serial") as string;
                if (!string.IsNullOrWhiteSpace(serialKey))
                {
                    gameKey = serialKey.Replace("/", "");
                    hideGameKey = new string('*', gameKey.Length);
                    Label_GameKey.Content = hideGameKey;
                    Button_CopyKey.IsEnabled = true;
                    Button_ShowKey.IsEnabled = true;
                }
            }
        }

        private void GetMissionsLoadouts()
        {
            string dirParh = $"{GameInfo.SaveDirectory}\\missions";
            if (!ST.DirectoryExists(dirParh))
                return;
            DirectoryInfo dirs = new(dirParh);
            foreach (var dir in dirs.GetDirectories())
            {
                ComboBox_MissionsLoadouts.Items.Add(new ComboBoxItem()
                {
                    Content = dir.Name,
                    ToolTip = dir.FullName,
                    Style = (Style)Application.Current.Resources["ComboBoxItem_Style"],
                });
            }
        }

        private void GetCustomResolution()
        {
            try
            {
                string data = File.ReadAllText(gameSettingsFile);
                bool isUndecoratedWindow = bool.Parse(Regex.Match(data, @"(?<=undecoratedWindow"":)(?:false|true)").Value);
                if (isUndecoratedWindow)
                    CheckBox_BorderlessWindow.IsChecked = true;
                string customResolutionData = Regex.Match(data, @"(?:#|)""resolutionOverride"":""[0-9]+x[0-9]+"",").Value;
                bool isEnableCustomResolution = customResolutionData.First() != '#';
                if (isEnableCustomResolution)
                {
                    Button_CustomResolutionReset.IsEnabled = true;
                    var resolution = Regex.Match(customResolutionData, @"(?<=(?:#|)""resolutionOverride"":"")[0-9]+x[0-9]+").Value.Split('x');
                    TextBox_ResolutionWidth.Text = resolution.First();
                    TextBox_ResolutionHeight.Text = resolution.Last();
                }
            }
            catch
            {
                STLog.WriteLine(I18n.CustomResolutionGetFailed, STLogLevel.ERROR);
                ST.ShowMessageBox(I18n.CustomResolutionGetFailed, Panuon.WPF.UI.MessageBoxIcon.Error);
            }
        }

        public int? CheckMemorySize(int size)
        {
            if (size < 1024)
            {
                ST.ShowMessageBox($"{I18n.MinMemory} 1024", Panuon.WPF.UI.MessageBoxIcon.Warning);
                return 1024;
            }
            else if (size > systemTotalMemory)
            {
                ST.ShowMessageBox($"{I18n.MaxMemory} {systemTotalMemory}", Panuon.WPF.UI.MessageBoxIcon.Warning);
                return systemTotalMemory;
            }
            return null;
        }
    }
}