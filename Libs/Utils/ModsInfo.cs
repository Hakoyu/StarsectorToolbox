using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HKW.Extension.SetExtension;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>所有模组信息</summary>
    public static class ModsInfo
    {
        /// <summary>
        /// <para>全部模组信息</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 模组信息</para>
        /// </summary>
        public static ReadOnlyDictionary<string, ModInfo> AllModsInfo { get; internal set; } = null!;
        /// <summary>已启用的模组ID</summary>
        public static ReadOnlySet<string> AllEnabledModsId { get; internal set; } = null!;
        /// <summary>已收藏的模组ID</summary>
        public static ReadOnlySet<string> AllCollectedModsId { get; internal set; } = null!;
        /// <summary>
        /// <para>全部用户分组</para>
        /// <para><see langword="Key"/>: 分组名称</para>
        /// <para><see langword="Value"/>: 包含的模组</para>
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlySet<string>> AllUserGroups { get; internal set; } = null!;
    }
}
