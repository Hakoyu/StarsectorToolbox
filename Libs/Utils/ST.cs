using System.Windows;
using StarsectorTools.Windows.MainWindow;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>StarsectorTools全局工具</summary>
    internal static class ST
    {
        internal static MainWindow MainWindow => (MainWindow)Application.Current.MainWindow;
        /// <summary>核心文件夹</summary>
        internal const string CoreDirectory = "Core";

        /// <summary>StarsectorTools配置文件</summary>
        internal const string STConfigTomlFile = $"{CoreDirectory}\\Config.toml";

        /// <summary>拓展调试目录</summary>
        internal static string ExpansionDebugPath = string.Empty;

        internal static string ExpansionDebugId = string.Empty;

        /// <summary>游戏版本</summary>
        internal const string Version = "0.8.0.0";

        /// <summary>菜单关闭</summary>
        public const string MenuCloseIcon = "📘";

        /// <summary>菜单打开</summary>
        public const string MenuOpenIcon = "📖";
    }
}