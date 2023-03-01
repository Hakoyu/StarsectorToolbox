﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using HKW.Libs.Log4Cs;
using StarsectorTools.Libs.GameInfo;
using static HKW.Extension.SetExtension;
using I18n = StarsectorTools.Langs.Libs.UtilsI18nRes;

namespace StarsectorTools.Libs.Utils
{
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

        /// <summary>
        /// 从json数据中解析模组信息,可设置路径
        /// </summary>
        /// <param name="jsonNode">json数据</param>
        /// <param name="jsonPath">json文件路径</param>
        private ModInfo(JsonNode jsonNode, string? jsonPath = null)
        {
            if (!string.IsNullOrWhiteSpace(jsonPath)
                && Utils.FileExists(jsonPath, false)
                && Path.GetDirectoryName(jsonPath) is string directoryPath)
                ModDirectory = directoryPath;
            foreach (var kv in jsonNode.AsObject())
                SetData(kv);
        }

        /// <summary>
        /// 解析 <see langword="mod_info.json"/> 并生成模组信息
        /// </summary>
        /// <param name="jsonPath">json文件路径</param>
        /// <returns>解析成功返回 <see cref="ModInfo"/> ,失败返回 <see langword="null"/></returns>
        public static ModInfo? Parse(string jsonPath)
        {
            try
            {
                if (Utils.JsonParse(jsonPath) is not JsonNode jsonNode)
                    return null;
                return new(jsonNode, jsonPath);
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
        /// <param name="jsonPath">json文件路径</param>
        /// <returns>解析成功返回 <see cref="ModInfo"/> ,失败返回 <see langword="null"/></returns>
        public static ModInfo? Parse(JsonNode jsonNode, string? jsonPath = null)
        {
            try
            {
                return new(jsonNode, jsonPath);
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
                    if (kv.Value! is JsonValue)
                        Version = kv.Value!.GetValue<string>();
                    else
                        Version = string.Join(".", kv.Value!.AsObject().Select(kv => kv.Value!.ToString()));
                    break;

                case "utility":
                    IsUtility = bool.Parse(kv.Value!.ToString());
                    break;

                case "description":
                    Description = kv.Value!.GetValue<string>();
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
}