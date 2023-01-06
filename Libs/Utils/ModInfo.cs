using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using I18n = StarsectorTools.Langs.Libs.Utils_I18n;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>模组信息</summary>
    public class ModInfo
    {
        /// <summary>ID</summary>
        public string Id { get; private set; } = null!;

        /// <summary>名称</summary>
        public string Name { get; private set; } = null!;

        /// <summary>作者</summary>
        public string Author { get; private set; } = null!;

        /// <summary>版本</summary>
        public string Version { get; private set; } = null!;

        /// <summary>是否为功能性模组</summary>
        public bool IsUtility { get; private set; } = false;

        /// <summary>描述</summary>
        public string Description { get; private set; } = null!;

        /// <summary>支持的游戏版本</summary>
        public string GameVersion { get; private set; } = null!;

        /// <summary>模组信息</summary>
        public string ModPlugin { get; private set; } = null!;

        /// <summary>前置</summary>
        public List<ModInfo>? Dependencies { get; private set; }

        /// <summary>本地路径</summary>
        public string Path = null!;

        public ModInfo(JsonObject jsonObject)
        {
            try
            {
                foreach (var kv in jsonObject)
                    SetData(kv);
            }
            catch (Exception ex)
            {
                STLog.WriteLine(I18n.ModInfoError, ex);
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
                    Dependencies ??= new();
                    foreach (var mod in kv.Value!.AsArray())
                        Dependencies.Add(new(mod!.AsObject()));
                    if (Dependencies.Count == 0)
                        Dependencies = null;
                    break;
            }
        }
    }
}