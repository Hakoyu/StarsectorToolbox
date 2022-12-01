using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Aspose.Zip;
using Aspose.Zip.SevenZip;
using Microsoft.VisualBasic.FileIO;

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
        public static string GetClassNameAndMethodName()
        {
            StackTrace stackTrace = new();
            StackFrame frame = stackTrace.GetFrame(2)!;
            MethodBase method = frame.GetMethod()!;
            return $"{method.ReflectedType!.Name!}.{method.Name!}";
        }
        public static string GetClassName()
        {
            StackTrace stackTrace = new();
            StackFrame frame = stackTrace.GetFrame(2)!;
            MethodBase method = frame.GetMethod()!;
            return method.ReflectedType!.Name!;
        }
        public void WriteLine(string message, STLogLevel logLevel = STLogLevel.INFO)
        {
            if (logLevel >= LogLevel)
            {
                string name;
                if (LogLevel == STLogLevel.DEBUG)
                    name = GetClassNameAndMethodName();
                else
                    name = GetClassName();
                sw.WriteLine($"[{name}] {logLevel} {message}");
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
        public static string gamePath { get; private set; } = null!;
        public static string gameExePath { get; private set; } = null!;
        public static string gameModsPath { get; private set; } = null!;
        public static string gameVersion { get; private set; } = null!;
        public static string gameSavePath { get; private set; } = null!;
        public static string enabledModsJsonPath { get; private set; } = null!;
        public static void SetGamePath(string path)
        {
            gameExePath = $"{path}\\starsector.exe";
            if (File.Exists(gameExePath))
            {
                gamePath = path;
                gameModsPath = $"{path}\\mods";
                gameSavePath = $"{path}\\saves";
                enabledModsJsonPath = $"{gameModsPath}\\enabled_mods.json";
                try
                {
                    gameVersion = JsonNode.Parse(File.ReadAllText($"{path}\\starsector-core\\localization_version.json"))!.AsObject()["game_version"]!.GetValue<string>();
                }
                catch (Exception ex)
                {
                    STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                }
            }
            else
            {
                gameExePath = null!;
                STLog.Instance.WriteLine($"游戏目录设置错误 位置: {path}", STLogLevel.ERROR);
                MessageBox.Show($"游戏目录设置错误\n位置: {path}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static bool CopyDirectory(string sourcePath, string destPath)
        {
            try
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
                return true;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                return false;
            }
        }
        public static bool DeleteFileToRecycleBin(string filePath)
        {
            try
            {
                if (char.IsLower(filePath.First()))
                    filePath = char.ToLower(filePath.First()) + filePath[1..];
                FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                return false;
            }
        }
        public static bool DeleteDirectoryToRecycleBin(string dirPath)
        {
            try
            {
                if (char.IsLower(dirPath.First()))
                    dirPath = char.ToLower(dirPath.First()) + dirPath[1..];
                FileSystem.DeleteDirectory(dirPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                return false;
            }
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
            SetGamePath(Path.GetDirectoryName(openFileDialog.FileName)!);
            return TestGamePath();
        }
        public static bool ZipFile(string sourceDirName, string destDirName)
        {
            var head = "";
            using StreamReader sr = new(sourceDirName);
            {
                head = $"{sr.Read()}{sr.Read()}";
            }
            try
            {
                if (head == "8297" || head == "8075")
                {
                    using (var archive = new Archive(sourceDirName, new() { Encoding = Encoding.UTF8 }))
                    {
                        archive.ExtractToDirectory(destDirName);
                    }
                }
                else if (head == "55122")
                {
                    using (var archive = new SevenZipArchive(sourceDirName))
                    {
                        archive.ExtractToDirectory(destDirName);
                    }
                }
                else
                {
                    //STLog.Instance.WriteLine(this, $"此文件不是压缩文件 位置: {sourceDirName}");
                    return false;
                }
                return true;
            }
            catch
            {
                //STLog.Instance.WriteLine(nameof(ZipFile), $"文件错误 位置:{sourceDirName}");
                Directory.Delete(destDirName);
                return false;
            }
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
