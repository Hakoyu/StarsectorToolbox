using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StarsectorTools.Lib
{
    public enum STLogLevel
    {
        DEBUG,
        INFO,
        WARN,
        ERROR
    }
    public sealed class STLog
    {
        public const string logPath = @"StarsectorTools.log";
        private static readonly Lazy<STLog> lazy = new(new STLog());
        public STLogLevel LogLevel = STLogLevel.INFO;
        private StreamWriter sw = new(logPath);
        public static STLog Instance
        {
            get
            {
                return lazy.Value;
            }
        }
        public void WriteLine(Window window, string message, STLogLevel logLevel = STLogLevel.INFO)
        {
            if (logLevel >= LogLevel)
            {
                sw.WriteLine($"[{window.ToString().Replace("StarsectorTools.", "")}] {logLevel} {message}");
                sw.Flush();
            }
        }
        public void WriteLine(Page page, string message, STLogLevel logLevel = STLogLevel.INFO)
        {
            if (logLevel >= LogLevel)
            {
                sw.WriteLine($"[{page.ToString()!.Replace("StarsectorTools.", "")}] {logLevel} {message}");
                sw.Flush();
            }
        }
        public void Close()
        {
            sw?.Close();
        }
    }

    public static class ST
    {
        public static int totalMemory = 0;
        public const string configPath = @"Config.toml";
        public readonly static Uri resourcesConfigUri = new("/Resources/Config.toml", UriKind.Relative);
        public const string logPath = @"StarsectorTools.toml";
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
        public static void CopyDirectory(string sourcePath, string destPath)
        {
            string floderName = Path.GetFileName(sourcePath);
            DirectoryInfo di = Directory.CreateDirectory(Path.Combine(destPath, floderName));
            string[] files = Directory.GetFileSystemEntries(sourcePath);
            foreach (string file in files)
            {
                if (Directory.Exists(file))
                    CopyDirectory(file, di.FullName);
                else
                    File.Copy(file, Path.Combine(di.FullName, Path.GetFileName(file)), true);
            }
        }
        //public static string? GetFileName(string filePath)
        //{
        //    if (!File.Exists(filePath))
        //        return null!;
        //    FileInfo fileInfo = new(filePath);
        //    if (fileInfo.Name.Split(".") is string[] array)
        //        return string.Join("", array[..^1]);
        //    return null;
        //}
        //public static string? GetDirectory(string filePath)
        //{
        //    if (!File.Exists(filePath))
        //        return null!;
        //    FileInfo fileInfo = new(filePath);
        //    return fileInfo.DirectoryName;
        //}
        //public static string? GetParentDirectory(string dir)
        //{
        //    if (!Directory.Exists(dir))
        //        return null;
        //    if (Directory.GetParent(dir) is DirectoryInfo directoryInfo)
        //        return directoryInfo.FullName;
        //    return null;
        //}
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
            SetGamePath(Path.GetDirectoryName(openFileDialog.FileName)!);
            return TestGamePath();
        }
    }
    public class ModInfo
    {
        public string Id { get; private set; } = null!;
        public string Name { get; private set; } = null!;
        public string Author { get; private set; } = null!;
        public string Version { get; private set; } = null!;
        public bool Utility { get; private set; } = false;
        public string Description { get; private set; } = null!;
        public string GameVersion { get; private set; } = null!;
        public string ModPlugin { get; private set; } = null!;
        public List<ModInfo>? Dependencies { get; private set; }
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
            }
        }
    }
}
