using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using HKW.ViewModels.Dialog;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Libs.Utils_I18n;

namespace StarsectorTools.Libs.GameInfo
{
    /// <summary>游戏信息</summary>
    public static class GameInfo
    {
        /// <summary>游戏目录</summary>
        public static string BaseDirectory { get; private set; } = null!;
        /// <summary>Core目录</summary>
        public static string CoreDirectory { get; private set; } = null!;

        /// <summary>游戏exe文件</summary>
        public static string ExeFile { get; private set; } = null!;

        /// <summary>游戏模组文件夹</summary>
        public static string ModsDirectory { get; private set; } = null!;

        /// <summary>游戏存档文件夹</summary>
        public static string SaveDirectory { get; private set; } = null!;

        /// <summary>游戏已启用模组文件</summary>
        public static string EnabledModsJsonFile { get; private set; } = null!;

        /// <summary>游戏日志文件</summary>
        public static string LogFile { get; private set; } = null!;

        /// <summary>游戏版本</summary>
        public static string Version { get; private set; } = null!;

        /// <summary>
        /// 设置游戏信息
        /// </summary>
        /// <param name="directoryName">游戏目录</param>
        internal static bool SetGameData(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
            {
                STLog.WriteLine(I18n.GameDirectoryIsEmpty, STLogLevel.ERROR);
                Utils.Utils.ShowMessageBox(I18n.GameDirectoryIsEmpty, STMessageBoxIcon.Error);
                return false;
            }
            ExeFile = $"{directoryName}\\starsector.exe";
            if (Utils.Utils.FileExists(ExeFile, false))
            {
                BaseDirectory = directoryName;
                ModsDirectory = $"{directoryName}\\mods";
                SaveDirectory = $"{directoryName}\\saves";
                CoreDirectory = $"{directoryName}\\starsector-core";
                EnabledModsJsonFile = $"{ModsDirectory}\\enabled_mods.json";
                LogFile = $"{CoreDirectory}\\starsector.log";
                try
                {
                    Version = JsonNode.Parse(File.ReadAllText($"{CoreDirectory}\\localization_version.json"))!.AsObject()["game_version"]!.GetValue<string>();
                    return true;
                }
                catch (Exception ex)
                {
                    STLog.WriteLine($"{I18n.LoadError} {I18n.Path}: {directoryName}", ex);
                }
            }
            else
            {
                ExeFile = null!;
                STLog.WriteLine($"{I18n.GameDirectoryError} {I18n.Path}: {directoryName}", STLogLevel.ERROR);
                Utils.Utils.ShowMessageBox($"{I18n.GameDirectoryError}\n{I18n.Path}", STMessageBoxIcon.Error);
            }
            return false;
        }

        /// <summary>
        /// 获取游戏目录
        /// </summary>
        /// <returns>获取成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        internal static bool GetGameDirectory()
        {
            var fileNames = OpenFileDialogModel.Show(new()
            {
                Filter = $"Exe {I18n.File}|starsector.exe"
            })!;
            if (fileNames.Any() && fileNames.First() is string fileName)
            {
                string directory = Path.GetDirectoryName(fileName)!;
                if (SetGameData(Path.GetDirectoryName(fileName)!))
                {
                    STLog.WriteLine($"{I18n.GameDirectorySetCompleted} {I18n.Path}: {directory}");
                    return true;
                }
            }
            return false;
        }
    }
}