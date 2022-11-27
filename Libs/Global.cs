using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace StarsectorTools.Lib
{
    public static class Global
    {
        public static int totalMemory = 0;
        public const string configPath = @"Config.toml";
        public readonly static Uri resourcesConfigUri = new("/Resources/Config.toml", UriKind.Relative);
        public static string gamePath = null!;
        public static string gameExePath = null!;
        public static string gameModsPath = null!;
        public static string gameVersion = null!;
        public static string enabledModsJsonPath = null!;
        public static void SetGamePath(string path)
        {
            gamePath = path;
            gameExePath = $"{path}\\starsector.exe";
            gameModsPath = $"{path}\\mods";
            enabledModsJsonPath = $"{gameModsPath}\\enabled_mods.json";
            gameVersion = JsonNode.Parse(File.ReadAllText($"{path}\\starsector-core\\localization_version.json"))!.AsObject()["game_version"]!.GetValue<string>();
        }
        public static string? GetFileName(string filePath)
        {
            if (!File.Exists(filePath))
                return null!;
            FileInfo fileInfo = new(filePath);
            if (fileInfo.Name.Split(".") is string[] array)
                return string.Join("", array[..^1]);
            return null;
        }
        public static string? GetDirectory(string filePath)
        {
            if (!File.Exists(filePath))
                return null!;
            FileInfo fileInfo = new(filePath);
            return fileInfo.DirectoryName;
        }
        public static string? GetParentDirectory(string dir)
        {
            if (!Directory.Exists(dir))
                return null;
            if (Directory.GetParent(dir) is DirectoryInfo directoryInfo)
                return directoryInfo.FullName;
            return null;
        }
        public static int MemorySizeParse(int size)
        {
            if (size < 1024)
            {
                MessageBox.Show("最小内存为 1024");
                size = 1024;
            }
            else if (size > totalMemory)
            {
                MessageBox.Show("最大不能超过本机物理内存");
                size = totalMemory;
            }
            return size;
        }
        public static int IntervalTimeParse(int size)
        {
            if (size.ToString().Length != 2)
            {
                size = 10;
                MessageBox.Show("The value range is 10~99");
            }
            return size;
        }
        /// <summary>
        /// 判断配置文件是否存在
        /// </summary>
        /// <returns>存在返回true,不存在则新建配置文件并返回false</returns>
        public static bool CreateConfigFile()
        {
            if (File.Exists(configPath))
                return true;
            using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
            string config = sr.ReadToEnd();
            File.WriteAllText(configPath, config);
            return false;
        }
        public static bool TestGamePath()
        {
            return File.Exists($"{gamePath}\\starsector.exe");
        }
        public static bool GetGamePath()
        {
            //新建文件选择
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                //文件选择类型
                //格式:文件描述|*.文件后缀(;*.文件后缀(适用于多个文件类型))|文件描述|*.文件后缀
                Filter = "Exe File|*.exe"
            };
            //显示文件选择对话框,并判断文件是否选取
            if (!openFileDialog.ShowDialog().GetValueOrDefault())
                return false;
            SetGamePath(GetDirectory(openFileDialog.FileName)!);
            return TestGamePath();
        }
    }
    public class ModInfo
    {
        public string? Id;
        public string? Name;
        public string? Author;
        public string? Version;
        public bool? Utility;
        public string? Description;
        public string? GameVersion;
        public string? ModPlugin;
        public List<ModInfo>? Dependencies;
        public Dictionary<string, string>? Other;
        public string Path = null!;
        public void SetData(KeyValuePair<string, JsonNode?> kv) => SetData(kv.Key, kv.Value!);
        public void SetData(string key, JsonNode value)
        {
            switch (key)
            {
                case "id":
                    Id = value.GetValue<string>();
                    break;
                case "name":
                    Name = value.GetValue<string>();
                    break;
                case "author":
                    Author = value.GetValue<string>();
                    break;
                case "version":
                    if (value is JsonValue)
                        Version = value.GetValue<string>();
                    else
                        Version = string.Join(".", value.AsObject().Select(kv => kv.Value!.ToString()));
                    break;
                case "utility":
                    Utility = bool.Parse(value.ToString());
                    break;
                case "description":
                    Description = value.GetValue<string>();
                    break;
                case "gameVersion":
                    GameVersion = value.GetValue<string>();
                    break;
                case "modPlugin":
                    ModPlugin = value.GetValue<string>();
                    break;
                case "dependencies":
                    Dependencies ??= new();
                    foreach (var mod in value.AsArray())
                    {
                        ModInfo modInfo = new();
                        foreach (var info in mod!.AsObject())
                            modInfo.SetData(info);
                        Dependencies.Add(modInfo);
                    }
                    break;
                default:
                    Other ??= new();
                    if (!Other.TryAdd(key, value.ToString()))
                        Other[key] += value.ToString();
                    break;
            }
        }
    }
}
