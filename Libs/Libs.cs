using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
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
    /// <summary>日志等级</summary>
    public enum STLogLevel
    {
        /// <summary>调试</summary>
        DEBUG,
        /// <summary>提示</summary>
        INFO,
        /// <summary>警告</summary>
        WARN,
        /// <summary>错误</summary>
        ERROR
    }
    /// <summary>StarsectorTools日志</summary>
    public sealed class STLog
    {
        /// <summary>日志目录</summary>
        public const string logPath = @"StarsectorTools.log";
        /// <summary>延迟启用</summary>
        private static readonly Lazy<STLog> lazy = new(new STLog());
        /// <summary>日志等级</summary>
        public STLogLevel LogLevel = STLogLevel.INFO;
        /// <summary>写入流</summary>
        private StreamWriter sw = new(logPath);
        /// <summary>单例</summary>
        public static STLog Instance
        {
            get => lazy.Value;
        }
        /// <summary>
        /// 字符串转换成日志等级
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>日志等级</returns>
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
        /// <summary>
        /// 获取所在类名和方法名
        /// </summary>
        /// <returns>类型名和方法名</returns>
        private static string GetClassNameAndMethodName()
        {
            StackTrace stackTrace = new();
            StackFrame frame = stackTrace.GetFrame(2)!;
            MethodBase method = frame.GetMethod()!;
            return $"{method.ReflectedType!.Name!}.{method.Name!}";
        }
        /// <summary>
        /// 获取所在类名
        /// </summary>
        /// <returns></returns>
        private static string GetClassName()
        {
            StackTrace stackTrace = new();
            StackFrame frame = stackTrace.GetFrame(2)!;
            MethodBase method = frame.GetMethod()!;
            return method.ReflectedType!.Name!;
        }
        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
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
        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="keys">嵌入实例</param>
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
        /// <summary>关闭</summary>
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
        /// <summary>
        /// 复制文件夹
        /// </summary>
        /// <param name="sourcePath">原始路径</param>
        /// <param name="destinationPath">目标路径</param>
        /// <returns>复制成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool CopyDirectory(string sourcePath, string destinationPath)
        {
            try
            {
                FileSystem.CopyDirectory(sourcePath, destinationPath, UIOption.OnlyErrorDialogs);
                return true;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                return false;
            }
        }
        /// <summary>
        /// 删除文件至回收站
        /// </summary>
        /// <param name="file"></param>
        /// <returns>删除成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool DeleteFileToRecycleBin(string file)
        {
            try
            {
                if (char.IsLower(file.First()))
                    file = char.ToLower(file.First()) + file[1..];
                FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                return false;
            }
        }
        /// <summary>
        /// 删除文件夹至回收站
        /// </summary>
        /// <param name="directory">文件夹</param>
        /// <returns>删除成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool DeleteDirToRecycleBin(string directory)
        {
            try
            {
                if (char.IsLower(directory.First()))
                    directory = char.ToLower(directory.First()) + directory[1..];
                FileSystem.DeleteDirectory(directory, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
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
        public static bool IsLightColor(Color color)
        {
            return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255 > 0.5;
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
        /// <summary>
        /// 使用系统默认打开方式打开文件
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>打开成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool OpenFile(string path)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo() { FileName = path, UseShellExecute = true });
                return true;
            }
            return false;
        }
        /// <summary>
        /// <para>压缩文件夹至Zip文件并输出到目录</para>
        /// <para>若不输入压缩文件名,则以原始目录的文件夹名称来命名</para>
        /// </summary>
        /// <param name="sourceDirName">原始目录</param>
        /// <param name="destDirName">输出目录</param>
        /// <param name="archiveName">压缩文件名</param>
        /// <returns>压缩成功为<see langword="true"/>,失败为<see langword="false"/></returns>
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
        /// <summary>
        /// <para>解压压缩文件至目录</para>
        /// <para>支持: <see langword="Zip"/> <see langword="Rar"/> <see langword="7z"/></para>
        /// </summary>
        /// <param name="sourceFileName">原始文件</param>
        /// <param name="destDirName">输出目录</param>
        /// <returns>解压成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool UnArchiveFileToDir(string sourceFileName, string destDirName)
        {
            if (!File.Exists(sourceFileName))
                return false;
            //读取压缩文件头,以判断压缩文件类型
            using StreamReader sr = new(sourceFileName);
            string head = $"{sr.Read()}{sr.Read()}";
            sr.Close();
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);
            try
            {
                if (head == "8075")//Zip文件
                {
                    using (var archive = new Archive(sourceFileName, new() { Encoding = Encoding.UTF8 }))
                    {
                        archive.ExtractToDirectory(destDirName);
                    }
                }
                else if (head == "8297")//Rar文件
                {
                    using (var archive = new RarArchive(sourceFileName))
                    {
                        archive.ExtractToDirectory(destDirName);
                    }
                }
                else if (head == "55122")//7z文件
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
                STLog.Instance.WriteLine($"{I18n.ZipFileError}  {I18n.Path}: {sourceFileName}", STLogLevel.ERROR);
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                if (Directory.Exists(destDirName))
                    Directory.Delete(destDirName);
                return false;
            }
            return true;
        }
    }
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
        /// <summary>设置模组信息</summary>
        /// <param name="kv">遍历至<see cref="JsonObject"/></param>
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
                    IsUtility = bool.Parse(value.ToString());
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
