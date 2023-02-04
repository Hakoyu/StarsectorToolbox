using System.Windows;
using StarsectorTools.Windows.MainWindow;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>StarsectorTools数据</summary>
    public static class ST
    {
        /// <summary>核心文件夹</summary>
        public const string CoreDirectory = "STCore";
        /// <summary>拓展目录</summary>
        public const string ExpansionDirectories = "STExpansion";
        /// <summary>StarsectorTools配置文件</summary>
        public const string ConfigTomlFile = $"{CoreDirectory}\\Config.toml";
        /// <summary>拓展信息文件</summary>
        public const string ExpansionInfoFile = "Expansion.toml";

        /// <summary>拓展调试目录</summary>
        internal static string ExpansionDebugPath = string.Empty;
        /// <summary>拓展调试Id</summary>
        internal static string ExpansionDebugId = string.Empty;

        /// <summary>游戏版本</summary>
        public const string Version = "0.8.0.0";
    }
}