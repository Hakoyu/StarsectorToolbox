using System;
using System.IO;
using System.Text.Json.Nodes;
using I18n = StarsectorTools.Langs.Libs.Utils_I18n;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>游戏信息</summary>
    public static class GameInfo
    {
        /// <summary>游戏目录</summary>
        public static string GameDirectory { get; private set; } = null!;

        /// <summary>游戏exe文件</summary>
        public static string ExeFile { get; private set; } = null!;

        /// <summary>游戏模组文件夹</summary>
        public static string ModsDirectory { get; private set; } = null!;

        /// <summary>游戏版本</summary>
        public static string Version { get; private set; } = null!;

        /// <summary>游戏存档文件夹</summary>
        public static string SaveDirectory { get; private set; } = null!;

        /// <summary>游戏已启用模组文件</summary>
        public static string EnabledModsJsonFile { get; private set; } = null!;

        /// <summary>游戏日志文件</summary>
        public static string LogFile { get; private set; } = null!;

        /// <summary>
        /// 设置游戏信息
        /// </summary>
        /// <param name="directoryName">游戏目录</param>
        internal static bool SetGameData(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
            {
                STLog.WriteLine(I18n.GameDirectoryIsEmpty, STLogLevel.ERROR);
                Utils.ShowMessageBox(I18n.GameDirectoryIsEmpty, STMessageBoxIcon.Error);
                return false;
            }
            ExeFile = $"{directoryName}\\starsector.exe";
            if (Utils.FileExists(ExeFile, false))
            {
                GameDirectory = directoryName;
                ModsDirectory = $"{directoryName}\\mods";
                SaveDirectory = $"{directoryName}\\saves";
                EnabledModsJsonFile = $"{ModsDirectory}\\enabled_mods.json";
                LogFile = $"{directoryName}\\starsector-core\\starsector.log";
                try
                {
                    Version = JsonNode.Parse(File.ReadAllText($"{directoryName}\\starsector-core\\localization_version.json"))!.AsObject()["game_version"]!.GetValue<string>();
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
                Utils.ShowMessageBox($"{I18n.GameDirectoryError}\n{I18n.Path}", STMessageBoxIcon.Error);
            }
            return false;
        }

        /// <summary>
        /// 获取游戏目录
        /// </summary>
        /// <returns>获取成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        internal static bool GetGameDirectory()
        {
            //新建文件选择
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                //文件选择类型
                //格式:文件描述|*.文件后缀(;*.文件后缀(适用于多个文件类型))|文件描述|*.文件后缀
                Filter = $"Exe {I18n.File}|starsector.exe"
            };
            //显示文件选择对话框,并判断文件是否选取
            if (!openFileDialog.ShowDialog().GetValueOrDefault())
                return false;
            string directory = System.IO.Path.GetDirectoryName(openFileDialog.FileName)!;
            if (SetGameData(System.IO.Path.GetDirectoryName(openFileDialog.FileName)!))
            {
                STLog.WriteLine($"{I18n.GameDirectorySetCompleted} {I18n.Path}: {directory}");
                return true;
            }
            return false;
        }
    }
}