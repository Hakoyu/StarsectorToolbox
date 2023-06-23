using System.Collections.ObjectModel;
using HKW.TOML;
using HKW.TOML.Attributes;
using HKW.TOML.Interfaces;
using StarsectorToolbox.Libs;
using I18nRes = StarsectorToolbox.Langs.Libs.UtilsI18nRes;

namespace StarsectorToolbox.Models.ModInfo;

internal class SetConverter : ITomlConverter<IReadOnlySet<string>>
{
    public IReadOnlySet<string> Read(TomlNode node)
    {
        return node.AsTomlArray.Select(n => n.AsString).ToHashSet();
    }

    public TomlNode Write(IReadOnlySet<string> value)
    {
        var array = new TomlArray();
        foreach (var item in value)
            array.Add(item);
        return array;
    }
}

// TODO: ModTypeGroup联网更新
/// <summary>
/// 模组分组类型
/// </summary>
public static class ModTypeGroup
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 文件
    /// </summary>
    [TomlIgnore]
    public static string File => $"{ST.ST.CoreDirectory}\\ModTypeGroup.toml";

    /// <summary>
    /// 前置模组
    /// </summary>
    [TomlPropertyOrder(0)]
    [TomlConverter(typeof(SetConverter))]
    public static IReadOnlySet<string> Libraries { get; internal set; } = null!;

    /// <summary>
    /// 大型模组
    /// </summary>
    [TomlPropertyOrder(1)]
    [TomlConverter(typeof(SetConverter))]
    public static IReadOnlySet<string> MegaMods { get; internal set; } = null!;

    /// <summary>
    /// 内容模组
    /// </summary>
    [TomlPropertyOrder(2)]
    [TomlConverter(typeof(SetConverter))]
    public static IReadOnlySet<string> ContentExtensions { get; internal set; } = null!;

    /// <summary>
    /// 派系模组
    /// </summary>
    [TomlPropertyOrder(3)]
    [TomlConverter(typeof(SetConverter))]
    public static IReadOnlySet<string> FactionMods { get; internal set; } = null!;

    /// <summary>
    /// 美化模组
    /// </summary>
    [TomlPropertyOrder(4)]
    [TomlConverter(typeof(SetConverter))]
    public static IReadOnlySet<string> BeautifyMods { get; internal set; } = null!;

    /// <summary>
    /// 功能模组
    /// </summary>
    [TomlPropertyOrder(5)]
    [TomlConverter(typeof(SetConverter))]
    public static IReadOnlySet<string> UtilityMods { get; internal set; } = null!;

    /// <summary>
    /// 闲杂模组
    /// </summary>
    [TomlPropertyOrder(6)]
    [TomlConverter(typeof(SetConverter))]
    public static IReadOnlySet<string> MiscellaneousMods { get; internal set; } = null!;

    /// <summary>
    /// 所有类型分组
    /// <para><see langword="Key"/>: 模组Id</para>
    /// <para><see langword="Value"/>: 模组所在的分组</para>
    /// </summary>
    [TomlIgnore]
    public static ReadOnlyDictionary<string, IReadOnlySet<string>> AllModTypeGroup
    {
        get;
        private set;
    } = null!;

    private static readonly Dictionary<string, string> sr_groupNameFromModId = new();

    [RunOnTomlDeserialized]
    internal static void SetAllModTypeGroup()
    {
        sr_groupNameFromModId.Clear();
        AllModTypeGroup = new(
            new Dictionary<string, IReadOnlySet<string>>()
            {
                [ModTypeGroupName.Libraries] = Libraries,
                [ModTypeGroupName.MegaMods] = MegaMods,
                [ModTypeGroupName.ContentExtensions] = ContentExtensions,
                [ModTypeGroupName.FactionMods] = FactionMods,
                [ModTypeGroupName.BeautifyMods] = BeautifyMods,
                [ModTypeGroupName.UtilityMods] = UtilityMods,
                [ModTypeGroupName.MiscellaneousMods] = MiscellaneousMods,
            }
        );
        var count = 0;
        var itemRecorder = new Dictionary<string, List<string>>();
        foreach (var kv in AllModTypeGroup)
        {
            foreach (var modId in kv.Value)
            {
                if (sr_groupNameFromModId.TryAdd(modId, kv.Key) is false)
                {
                    if (itemRecorder.TryAdd(modId, new() { kv.Key }))
                        itemRecorder[modId].Add(sr_groupNameFromModId[modId]);
                    else
                        itemRecorder[modId].Add(kv.Key);
                }
                count++;
            }
        }
        if (count != sr_groupNameFromModId.Count)
        {
            sr_logger.Warn(
                $"{I18nRes.DuplicateItemsInModTypeGroup}\n{string.Join("\n", itemRecorder.Select(kv => $"{kv.Key} : {string.Join(", ", kv.Value)}"))}"
            );
        }
    }

    /// <summary>
    /// 获取模组所在的组
    /// </summary>
    /// <param name="modId">模组Id</param>
    /// <returns>模组所在的组名</returns>
    public static string GetGroupNameFromId(string modId)
    {
        return sr_groupNameFromModId.ContainsKey(modId)
            ? sr_groupNameFromModId[modId]
            : ModTypeGroupName.UnknownMods;
    }
}
