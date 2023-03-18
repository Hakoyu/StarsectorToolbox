using System;
using System.Collections.Generic;

namespace StarsectorTools.Models
{
    /// <summary>
    /// 模组信息接口
    /// </summary>
    public interface IModInfo
    {
        /// <summary>ID</summary>
        public string Id { get; }

        /// <summary>名称</summary>
        public string Name { get; }

        /// <summary>作者</summary>
        public string Author { get; }

        /// <summary>版本</summary>
        public string Version { get; }

        /// <summary>是功能性模组</summary>
        public bool IsUtility { get; }

        /// <summary>描述</summary>
        public string Description { get; }

        /// <summary>支持的游戏版本</summary>
        public string GameVersion { get; }

        /// <summary>支持的游戏版本与当前游戏版本相同</summary>
        public bool IsSameToGameVersion { get; }

        /// <summary>模组信息</summary>
        public string ModPlugin { get; }

        /// <summary>模组文件夹</summary>
        public string ModDirectory { get; }

        /// <summary>最后更新时间</summary>
        public DateTime LastUpdateTime { get; }

        /// <summary>前置模组</summary>
        public IReadOnlySet<ModInfo>? DependenciesSet { get; }
    }
}