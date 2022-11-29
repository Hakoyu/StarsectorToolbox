using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using StarsectorTools.Lib;

namespace StarsectorTools.Pages
{
    public partial class Settings
    {
        struct VmparamsData
        {
            public string data;
            public string xmsx;
        }

        VmparamsData vmparamsData = new();
        string gameKey= string.Empty;
        void GetVmparamsData()
        {
            vmparamsData.data = File.ReadAllText($"{ST.gamePath}\\vmparams");
            vmparamsData.xmsx = Regex.Match(vmparamsData.data, @"(?<=-xm[sx])[0-9]+", RegexOptions.IgnoreCase).Value;
            TextBox_MaxMemory.Text = TextBox_MinMemory.Text = vmparamsData.xmsx;
        }
        void SetVmparamsData()
        {
            File.WriteAllText($"{ST.gamePath}\\vmparams", Regex.Replace(vmparamsData.data, @"(?<=-xm[sx])(.+?)\b", $"{TextBox_MinMemory.Text}m", RegexOptions.IgnoreCase));
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
                    Password_GameKey.Password = gameKey;
                    Button_DuplicateKey.IsEnabled= true;
                }
            }
        }
    }
}
