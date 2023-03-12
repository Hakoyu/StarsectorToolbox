using System;
using HKW.Libs.TomlParse;

namespace StarsectorTools.ViewModels.MainWindow
{
    /// <summary>拓展信息</summary>
    internal class ExtensionInfo
    {
        /// <summary>Id</summary>
        public string Id { get; private set; } = null!;

        /// <summary>名称</summary>
        public string Name { get; private set; } = null!;

        /// <summary>作者</summary>
        public string Author { get; private set; } = null!;

        /// <summary>图标</summary>
        public string Icon { get; private set; } = null!;

        /// <summary>版本</summary>
        public string Version { get; private set; } = null!;

        /// <summary>支持的工具箱版本</summary>
        public string ToolsVersion { get; private set; } = null!;

        /// <summary>描述</summary>
        public string Description { get; private set; } = null!;

        /// <summary>拓展Id</summary>
        public string ExtensionId { get; private set; } = null!;

        /// <summary>拓展文件</summary>
        public string ExtensionFile { get; private set; } = null!;

        /// <summary>拓展类型</summary>
        public Type ExtensionType { get; set; } = null!;

        /// <summary>拓展页面</summary>
        public object ExtensionPage { get; set; } = null!;

        public ExtensionInfo(TomlTable table)
        {
            foreach (var info in table)
                SetInfo(info.Key, info.Value.AsString);
        }

        public void SetInfo(string key, string value)
        {
            switch (key)
            {
                case nameof(Id):
                    Id = value;
                    break;

                case nameof(Name):
                    Name = value;
                    break;

                case nameof(Author):
                    Author = value;
                    break;

                case nameof(Icon):
                    Icon = value;
                    break;

                case nameof(Version):
                    Version = value;
                    break;

                case nameof(ToolsVersion):
                    ToolsVersion = value;
                    break;

                case nameof(Description):
                    Description = value;
                    break;

                case nameof(ExtensionId):
                    ExtensionId = value;
                    break;

                case nameof(ExtensionFile):
                    ExtensionFile = value;
                    break;
            }
        }
    }
}