namespace StarsectorTools.Libs.Utils
{
    /// <summary>StarsectorTools数据</summary>
    public static class ST
    {
        /// <summary>核心文件夹</summary>
        public const string CoreDirectory = "STCore";

        /// <summary>拓展目录</summary>
        public const string ExtensionDirectories = "STExtension";

        /// <summary>配置文件</summary>
        public const string ConfigTomlFile = $"{CoreDirectory}\\Config.toml";

        /// <summary>日志文件</summary>
        public const string LogFile = $"{CoreDirectory}\\{nameof(StarsectorTools)}.log";

        /// <summary>拓展信息文件</summary>
        public const string ExtensionInfoFile = "Extension.toml";

        /// <summary>游戏版本</summary>
        public const string Version = "0.8.0.0";
    }
}