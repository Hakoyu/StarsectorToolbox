using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using HKW.Management;
using StarsectorTools.Libs;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using I18n = StarsectorTools.Langs.Tools.GameSettings.GameSettings_I18n;

namespace StarsectorTools.Tools.GameSettings
{
    public partial class GameSettings
    {
        void GetVmparamsData()
        {
            vmparamsData.data = File.ReadAllText($"{ST.gamePath}\\vmparams");
            vmparamsData.xmsx = Regex.Match(vmparamsData.data, @"(?<=-xm[sx])[0-9]+", RegexOptions.IgnoreCase).Value;
            TextBox_Memory.Text = TextBox_Memory.Text = vmparamsData.xmsx;
        }
        void SetVmparamsData()
        {
            File.WriteAllText($"{ST.gamePath}\\vmparams", Regex.Replace(vmparamsData.data, @"(?<=-xm[sx])(.+?)\b", $"{TextBox_Memory.Text}m", RegexOptions.IgnoreCase));
            STLog.Instance.WriteLine($"{I18n.VmparamsMemorySet}: {TextBox_Memory.Text}m");
        }
        void GetGameKey()
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
        void GetMissionsLoadouts()
        {
            string dirParh = $"{ST.gameSavePath}\\missions";
            if (!Directory.Exists(dirParh))
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
    }
}
