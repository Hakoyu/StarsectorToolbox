using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StarsectorTools.Lib;

namespace StarsectorTools.Pages
{
    public partial class Page_Settings
    {
        struct VmparamsData
        {
            public string data;
            public string xmsx;
        }
        VmparamsData vmparamsData = new();
        void GetVmparamsData()
        {
            vmparamsData.data = File.ReadAllText($"{Global.gamePath}\\vmparams");
            vmparamsData.xmsx = Regex.Match(vmparamsData.data, @"(?<=-xm[sx])[0-9]+", RegexOptions.IgnoreCase).Value;
            TextBox_MaxMemory.Text = TextBox_MinMemory.Text = vmparamsData.xmsx;
        }
        void SetVmparamsData()
        {
            File.WriteAllText($"{Global.gamePath}\\vmparams", Regex.Replace(vmparamsData.data, @"(?<=-xm[sx])(.+?)\b", $"{TextBox_MinMemory.Text}m", RegexOptions.IgnoreCase));
        }
    }
}
