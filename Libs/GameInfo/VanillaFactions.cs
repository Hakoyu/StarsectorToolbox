using System.Collections.Generic;
using System.Collections.ObjectModel;
using I18n = StarsectorTools.Langs.Libs.GameInfoI18nRes;

namespace StarsectorTools.Libs.GameInfo;

/// <summary>
/// 原版派系
/// </summary>
public static class VanillaFactions
{
    /// <summary>遗弃船</summary>
    public const string Derelict = "derelict";

    /// <summary>霸主</summary>
    public const string Hegemony = "hegemony";

    /// <summary>自由联盟</summary>
    public const string Independent = "independent";

    /// <summary>卢德骑士团</summary>
    public const string KnightsOfLudd = "knights_of_ludd";

    /// <summary>狮心守卫</summary>
    public const string LionsGuard = "lions_guard";

    /// <summary>卢德教会</summary>
    public const string LuddicChurch = "luddic_church";

    /// <summary>卢德左径</summary>
    public const string LuddicPath = "luddic_path";

    /// <summary>雇佣兵</summary>
    public const string Mercenary = "mercenary";

    /// <summary>中立</summary>
    public const string Neutral = "neutral";

    /// <summary>欧米伽</summary>
    public const string Omega = "omega";

    /// <summary>英仙座联盟</summary>
    public const string PerseanLeague = "persean_league";

    /// <summary>海盗</summary>
    public const string Pirates = "pirates";

    /// <summary>玩家</summary>
    public const string Player = "player";

    /// <summary>难民</summary>
    public const string Poor = "poor";

    /// <summary>余辉</summary>
    public const string Remnants = "remnants";

    /// <summary>拾荒者</summary>
    public const string Scavengers = "scavengers";

    /// <summary>辛达强权</summary>
    public const string SindrianDiktat = "sindrian_diktat";

    /// <summary>渗透者</summary>
    public const string Sleeper = "sleeper";

    /// <summary>速子科技</summary>
    public const string Tritachyon = "tritachyon";

    /// <summary>所有势力及其I18n</summary>
    public static ReadOnlyDictionary<string, string> AllVanillaFactionsI18n = new(new Dictionary<string, string>()
    {
        [Derelict] = I18n.Derelict,
        [Hegemony] = I18n.Hegemony,
        [Independent] = I18n.Independent,
        [KnightsOfLudd] = I18n.KnightsOfLudd,
        [LionsGuard] = I18n.LionsGuard,
        [LuddicChurch] = I18n.LuddicChurch,
        [LuddicPath] = I18n.LuddicPath,
        [Mercenary] = I18n.Mercenary,
        [Neutral] = I18n.Neutral,
        [Omega] = I18n.Omega,
        [PerseanLeague] = I18n.PerseanLeague,
        [Pirates] = I18n.Pirates,
        [Player] = I18n.Player,
        [Poor] = I18n.Poor,
        [Remnants] = I18n.Remnants,
        [Scavengers] = I18n.Scavengers,
        [SindrianDiktat] = I18n.SindrianDiktat,
        [Sleeper] = I18n.Sleeper,
        [Tritachyon] = I18n.Tritachyon
    });
}