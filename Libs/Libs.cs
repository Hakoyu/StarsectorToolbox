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
using Aspose.Zip.Rar;
using Aspose.Zip.SevenZip;
using Microsoft.VisualBasic.FileIO;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using I18n = StarsectorTools.Langs.Libs.Libs_I18n;

namespace StarsectorTools.Libs
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
        public static STLogLevel Str2STLogLevel(string str)
        {
            return str switch
            {
                nameof(STLogLevel.DEBUG) => STLogLevel.DEBUG,
                nameof(STLogLevel.INFO) => STLogLevel.INFO,
                nameof(STLogLevel.WARN) => STLogLevel.WARN,
                nameof(STLogLevel.ERROR) => STLogLevel.ERROR,
                _ => STLogLevel.INFO
            };
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
        public void WriteLine(string message, STLogLevel logLevel = STLogLevel.INFO, params object[] keys)
        {
            if (logLevel >= LogLevel)
            {
                string name;
                if (LogLevel == STLogLevel.DEBUG)
                    name = GetClassNameAndMethodName();
                else
                    name = GetClassName();
                sw.WriteLine($"[{name}] {logLevel} {ParseKey(message, keys)}");
                sw.Flush();
            }
        }
        private string ParseKey(string str, params object[] keys)
        {
            try
            {
                return string.Format(str, keys);
            }
            catch
            {
                return str;
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
        public const string logPath = @"StarsectorTools.log";
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
                STLog.Instance.WriteLine($"{I18n.GameDirectoryError} {I18n.Path}: {path}", STLogLevel.ERROR);
                MessageBox.Show($"{I18n.GameDirectoryError}\n{I18n.Path}", "", MessageBoxButton.OK, MessageBoxImage.Error);
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
        public static bool DeleteDirToRecycleBin(string dirPath)
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
                MessageBox.Show($"{I18n.MinMemory} 1024");
                size = 1024;
            }
            else if (size > totalMemory)
            {
                MessageBox.Show(I18n.MaxMemory);
                size = totalMemory;
            }
            return size;
        }
        public static bool CheckConfigFile()
        {
            return File.Exists(configPath);
        }
        public static void CreateConfigFile()
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
            using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
            string str = sr.ReadToEnd();
            File.WriteAllText(configPath, str);
            sr.Close();
            STLog.Instance.WriteLine($"{I18n.ConfigFileCreatedSuccess} {configPath}");
        }
        public static bool CheckGamePath()
        {
            return File.Exists(gameExePath);
        }
        public static bool GetGamePath()
        {
            //新建文件选择
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                //文件选择类型
                //格式:文件描述|*.文件后缀(;*.文件后缀(适用于多个文件类型))|文件描述|*.文件后缀
                Filter = $"Exe {I18n.File}|starsector.exe"
            };
            //显示文件选择对话框,并判断文件是否选取
            if (!openFileDialog.ShowDialog().GetValueOrDefault())
                return false;
            SetGamePath(Path.GetDirectoryName(openFileDialog.FileName)!);
            return CheckGamePath();
        }
        public static bool OpenFile(string path)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo() { FileName = path, UseShellExecute = true });
                return true;
            }
            return false;
        }
        public static bool ArchiveDirToDir(string sourceDirName, string destDirName, string? archiveName = null)
        {
            if (!Directory.Exists(sourceDirName))
                return false;
            try
            {
                using (var archive = ZipArchive.Create())
                {
                    archive.AddAllFromDirectory(sourceDirName);
                    if (archiveName is null)
                        archive.SaveTo($"{destDirName}\\{Path.GetFileName(sourceDirName)}.zip", CompressionType.Deflate);
                    else
                        archive.SaveTo($"{destDirName}\\{archiveName}.zip", CompressionType.Deflate);
                }
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.ZipFileError} {I18n.Path}: {sourceDirName}", STLogLevel.WARN);
                STLog.Instance.WriteLine(ex.Message, STLogLevel.WARN);
                return false;
            }
            return true;
        }
        public static bool UnArchiveFileToDir(string sourceFileName, string destDirName)
        {
            if (!File.Exists(sourceFileName))
                return false;
            using StreamReader sr = new(sourceFileName);
            string head = $"{sr.Read()}{sr.Read()}";
            sr.Close();
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);
            try
            {
                if (head == "8075")
                {
                    using (var archive = new Archive(sourceFileName, new() { Encoding = Encoding.UTF8 }))
                    {
                        archive.ExtractToDirectory(destDirName);
                    }
                }
                else if (head == "8297")
                {
                    using (var archive = new RarArchive(sourceFileName))
                    {
                        archive.ExtractToDirectory(destDirName);
                    }
                }
                else if (head == "55122")
                {
                    using (var archive = new SevenZipArchive(sourceFileName))
                    {
                        archive.ExtractToDirectory(destDirName);
                    }
                }
                else
                    throw new Exception();
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.ZipFileError}  {I18n.Path}: {sourceFileName}", STLogLevel.WARN);
                STLog.Instance.WriteLine(ex.Message, STLogLevel.WARN);
                if (Directory.Exists(destDirName))
                    Directory.Delete(destDirName);
                return false;
            }
            return true;
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
                    Author = value.GetValue<string>().Trim();
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
