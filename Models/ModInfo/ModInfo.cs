using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using HKW.Libs.Log4Cs;
using StarsectorToolbox.Libs;
using I18n = StarsectorToolbox.Langs.Libs.UtilsI18nRes;

namespace StarsectorToolbox.Models.ModInfo;

/// <summary>模组信息</summary>
[DebuggerDisplay("{Name},Version = {Version}")]
public class ModInfo : IModInfo
{
    /// <inheritdoc/>
    public string Id { get; private set; } = null!;

    /// <inheritdoc/>
    public string Name { get; private set; } = null!;

    /// <inheritdoc/>
    public string Author { get; private set; } = null!;

    /// <inheritdoc/>
    public string Version { get; private set; } = null!;

    /// <inheritdoc/>
    public bool IsUtility { get; private set; } = false;

    /// <inheritdoc/>
    public string Description { get; private set; } = null!;

    /// <inheritdoc/>
    public string GameVersion { get; private set; } = null!;

    /// <inheritdoc/>
    public string ModPlugin { get; private set; } = null!;

    /// <inheritdoc/>
    public string ModDirectory { get; private set; } = null!;

    /// <inheritdoc/>
    public IReadOnlySet<ModInfo>? DependenciesSet { get; private set; } = null!;

    /// <inheritdoc/>
    public bool IsSameToGameVersion => GameVersion == GameInfo.GameInfo.Version;

    /// <inheritdoc/>
    public DateTime LastUpdateTime { get; private set; }

    /// <summary>
    /// 从json数据中解析模组信息,可设置路径
    /// </summary>
    /// <param name="jsonNode">json数据</param>
    /// <param name="lastWriteTime">最后更新时间</param>
    /// <param name="jsonFile">json文件路径</param>
    private ModInfo(JsonNode jsonNode, DateTime? lastWriteTime = null, string? jsonFile = null)
    {
        LastUpdateTime = lastWriteTime ?? default;
        ModDirectory = Path.GetDirectoryName(jsonFile)!;
        foreach (var kv in jsonNode.AsObject())
            SetData(kv);
    }

    /// <summary>
    /// 解析 <see langword="mod_info.json"/> 并生成模组信息
    /// </summary>
    /// <param name="jsonFile">json文件路径</param>
    /// <returns>解析成功返回 <see cref="ModInfo"/> ,失败返回 <see langword="null"/></returns>
    public static ModInfo? Parse(string jsonFile)
    {
        try
        {
            if (Utils.JsonParse2Object(jsonFile) is not JsonNode jsonNode)
                return null;
            // 获取所有文件的修改时间,取最近的
            var lastWriteTime = Directory
                .EnumerateFiles(Path.GetDirectoryName(jsonFile)!, "*", SearchOption.AllDirectories)
                .Max(f => new FileInfo(f).LastWriteTime);
            return new(jsonNode, lastWriteTime, jsonFile);
        }
        catch (Exception ex)
        {
            Logger.Error(I18n.ModInfoError, ex);
            return null;
        }
    }

    /// <summary>
    /// 从json数据中解析模组信息,可设置路径
    /// </summary>
    /// <param name="jsonNode">json数据</param>
    /// <param name="lastWriteTime">最后更新时间</param>
    /// <param name="jsonFile">json文件路径</param>
    /// <returns>解析成功返回 <see cref="ModInfo"/> ,失败返回 <see langword="null"/></returns>
    public static ModInfo? Parse(JsonNode jsonNode, DateTime lastWriteTime, string? jsonFile = null)
    {
        try
        {
            return new(jsonNode, lastWriteTime, jsonFile);
        }
        catch (Exception ex)
        {
            Logger.Error(I18n.ModInfoError, ex);
            return null;
        }
    }

    /// <summary>设置模组信息</summary>
    /// <param name="kv">遍历至<see cref="JsonObject"/></param>
    private void SetData(KeyValuePair<string, JsonNode?> kv)
    {
        switch (kv.Key)
        {
            case "id":
                Id = kv.Value!.GetValue<string>();
                break;

            case "name":
                Name = kv.Value!.GetValue<string>();
                break;

            case "author":
                Author = kv.Value!.GetValue<string>().Trim();
                break;

            case "version":
                if (kv.Value is JsonValue)
                    Version = kv.Value!.GetValue<string>();
                else
                    Version = string.Join(
                        ".",
                        kv.Value!.AsObject().Select(kv => kv.Value!.ToString())
                    );
                break;

            case "utility":
                IsUtility = bool.Parse(kv.Value!.ToString());
                break;

            case "description":
                Description = kv.Value!.GetValue<string>().Trim();
                break;

            case "gameVersion":
                GameVersion = kv.Value!.GetValue<string>();
                break;

            case "modPlugin":
                ModPlugin = kv.Value!.GetValue<string>();
                break;

            case "dependencies":
                HashSet<ModInfo> dependenciesSet = new();
                foreach (var mod in kv.Value!.AsArray())
                    dependenciesSet.Add(new(mod!.AsObject()));
                if (dependenciesSet.Any())
                    DependenciesSet = dependenciesSet;
                break;
        }
    }

    /// <summary>
    /// 转为字符串
    /// </summary>
    /// <returns>名称与版本</returns>
    public override string ToString()
    {
        return $"{Name}: {Version}";
    }
}
