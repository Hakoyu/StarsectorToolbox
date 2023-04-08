using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HKW.Libs.Log4Cs;
using HKW.ViewModels.Dialogs;
using Microsoft.Win32;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.Models.System;
using I18nRes = StarsectorToolbox.Langs.Pages.GameSettings.GameSettingsPageI18nRes;

namespace StarsectorToolbox.ViewModels.GameSettings;

internal partial class GameSettingsPageViewModel
{
    private struct VmparamsData
    {
        public string data;
        public string xmsx;
    }

    private VmparamsData _vmparamsData = new();

    private void GetVmparamsData()
    {
        _vmparamsData.data = File.ReadAllText($"{GameInfo.BaseDirectory}\\vmparams");
        _vmparamsData.xmsx = Regex
            .Match(_vmparamsData.data, @"(?<=-xm[sx])[0-9]+[mg]", RegexOptions.IgnoreCase)
            .Value;
        GameMemory = _vmparamsData.xmsx;
    }

    private void GetGameKey()
    {
        var key = Registry.CurrentUser.OpenSubKey("Software\\JavaSoft\\Prefs\\com\\fs\\starfarer");
        if (key != null)
        {
            var serialKey = key.GetValue("serial") as string;
            if (string.IsNullOrWhiteSpace(serialKey) is false)
            {
                _realGameKey = serialKey.Replace("/", "");
                GameKey = _hideGameKey = new string('*', 23);
            }
        }
    }

    private void GetMissionsLoadouts()
    {
        string dirParh = $"{GameInfo.SaveDirectory}\\missions";
        if (Utils.DirectoryExists(dirParh) is false)
            return;
        DirectoryInfo dirs = new(dirParh);
        foreach (var dir in dirs.GetDirectories())
        {
            ComboBox_MissionsLoadouts.Add(new() { Content = dir.Name, ToolTip = dir.FullName, });
        }
    }

    private int? CheckMemorySize(int size)
    {
        if (size < 1024)
        {
            MessageBoxVM.Show(
                new($"{I18nRes.MinMemoryConnotLowThan} 1024") { Icon = MessageBoxVM.Icon.Warning }
            );
            return 1024;
        }
        else if (size > SystemInfo.TotalMemory)
        {
            MessageBoxVM.Show(
                new($"{I18nRes.MaxMemoryConnotExceed} {SystemInfo.TotalMemory}")
                {
                    Icon = MessageBoxVM.Icon.Warning
                }
            );
            return SystemInfo.TotalMemory;
        }
        return null;
    }

    private void GetCustomResolution()
    {
        try
        {
            string data = File.ReadAllText(GameInfo.SettingsFile);
            bool isUndecoratedWindow = bool.Parse(
                Regex.Match(data, @"(?<=undecoratedWindow"":)(?:false|true)").Value
            );
            if (isUndecoratedWindow)
                BorderlessWindow = true;
            string customResolutionData = Regex
                .Match(data, @"(?:#|)""resolutionOverride"":""[0-9]+x[0-9]+"",")
                .Value;
            bool isEnableCustomResolution = customResolutionData.First() != '#';
            if (isEnableCustomResolution)
            {
                CustomResolutionCanReset = true;
                var resolution = Regex
                    .Match(
                        customResolutionData,
                        @"(?<=(?:#|)""resolutionOverride"":"")[0-9]+x[0-9]+"
                    )
                    .Value.Split('x');
                ResolutionWidth = resolution.First();
                ResolutionHeight = resolution.Last();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(I18nRes.CustomResolutionGetFailed, ex);
            MessageBoxVM.Show(
                new(I18nRes.CustomResolutionGetFailed) { Icon = MessageBoxVM.Icon.Error }
            );
        }
    }
}
