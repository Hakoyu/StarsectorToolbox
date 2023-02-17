using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using StarsectorTools.Libs.GameInfo;

namespace StarsectorTools.Pages.GameSettings
{
    internal partial class GameSettingsPageViewModel
    {
        private struct VmparamsData
        {
            public string data;
            public string xmsx;
        }
        private VmparamsData vmparamsData = new();

        private void GetVmparamsData()
        {
            vmparamsData.data = File.ReadAllText($"{GameInfo.BaseDirectory}\\vmparams");
            vmparamsData.xmsx = Regex.Match(vmparamsData.data, @"(?<=-xm[sx])[0-9]+[mg]", RegexOptions.IgnoreCase).Value;
            Memory = vmparamsData.xmsx;
        }
        private void GetGameKey()
        {
            var key = Registry.CurrentUser.OpenSubKey("Software\\JavaSoft\\Prefs\\com\\fs\\starfarer");
            if (key != null)
            {
                var serialKey = key.GetValue("serial") as string;
                if (!string.IsNullOrWhiteSpace(serialKey))
                {
                    realGameKey = serialKey.Replace("/", "");
                    GameKey = hideGameKey = new string('*', 23);
                }
            }
        }
    }
}
