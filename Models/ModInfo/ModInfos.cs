using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using StarsectorToolbox.Libs;

namespace StarsectorToolbox.Models.ModInfo;

/// <summary>所有模组信息</summary>
public static class ModInfos
{
    private const string c_strEnabledMods = "enabledMods";

    /// <summary>
    /// <para>全部模组信息</para>
    /// <para><see langword="Key"/>: 模组ID</para>
    /// <para><see langword="Value"/>: 模组信息</para>
    /// </summary>
    public static ReadOnlyDictionary<string, ModInfo> AllModInfos { get; internal set; } =
        new(new Dictionary<string, ModInfo>());

    /// <summary>已启用的模组ID</summary>
    public static IReadOnlySet<string> AllEnabledModIds { get; internal set; } =
        new HashSet<string>();

    /// <summary>已收藏的模组ID</summary>
    public static IReadOnlySet<string> AllCollectedModIds { get; internal set; } =
        new HashSet<string>();

    /// <summary>
    /// <para>全部用户分组</para>
    /// <para><see langword="Key"/>: 分组名称</para>
    /// <para><see langword="Value"/>: 包含的模组</para>
    /// </summary>
    public static ReadOnlyDictionary<string, IReadOnlySet<string>> AllUserGroups
    {
        get;
        internal set;
    } = new(new Dictionary<string, IReadOnlySet<string>>());

    /// <summary>
    /// 获取当前的已启用模组的Id(从enabled_mods.json)
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<string>? GetCurrentEnabledModIds()
    {
        if (File.Exists(GameInfo.GameInfo.EnabledModsJsonFile) is false)
            return null;
        try
        {
            StringBuilder errSB = new();
            if (
                Utils.JsonParse2Object(GameInfo.GameInfo.EnabledModsJsonFile)
                is not JsonObject enabledModsJson
            )
                return null;
            if (enabledModsJson.Count is not 1 || !enabledModsJson.ContainsKey(c_strEnabledMods))
                return null;
            if (enabledModsJson[c_strEnabledMods]?.AsArray() is not JsonArray enabledModsJsonArray)
                return null;
            return enabledModsJsonArray
                .Select(i => i!.GetValue<string>())
                .Where(s => string.IsNullOrWhiteSpace(s) is false)
                .Distinct();
        }
        catch
        {
            return null;
        }
    }
}
