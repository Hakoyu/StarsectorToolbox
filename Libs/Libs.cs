using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Media;
using Aspose.Zip;
using Aspose.Zip.Rar;
using Aspose.Zip.SevenZip;
using HKW.Management;
using Microsoft.VisualBasic.FileIO;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using StarsectorTools.Windows;
using I18n = StarsectorTools.Langs.Libs.Libs_I18n;

namespace StarsectorTools.Libs
{
    /// <summary>StarsectorTools日志等级</summary>
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
        public const string logFile = @"StarsectorTools.log";

        /// <summary>延迟启用</summary>
        private static readonly Lazy<STLog> lazy = new(new STLog());

        /// <summary>日志等级</summary>
        public STLogLevel LogLevel = STLogLevel.INFO;

        /// <summary>写入流</summary>
        private StreamWriter sw = new(logFile);

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
        private static string? GetClassNameAndMethodName()
        {
            MethodBase? method = new StackTrace().GetFrame(3)?.GetMethod();
            return $"{method?.ReflectedType?.Name}.{method?.Name}";
        }

        /// <summary>
        /// 获取所在类名
        /// </summary>
        /// <returns></returns>
        private static string? GetClassName()
        {
            return new StackTrace().GetFrame(3)?.GetMethod()?.ReflectedType?.Name;
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        public void WriteLine(string message, STLogLevel logLevel = STLogLevel.INFO)
        {
            WriteLine(message, logLevel, null!);
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
                string? name;
                if (logLevel == STLogLevel.DEBUG)
                    name = GetClassNameAndMethodName();
                else
                    name = GetClassName();
                sw.WriteLine($"[{name}] {logLevel} {ParseKey(message, keys)}");
                sw.Flush();
            }
        }

        /// <summary>
        /// 写入捕获的错误日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ex">错误</param>
        /// <param name="keys">嵌入实例</param>
        public void WriteLine(string message, Exception ex, params object[] keys)
        {
            sw.WriteLine($"[{GetClassName()}] {STLogLevel.ERROR} {ParseKey(message, keys)}");
            sw.WriteLine($"   at {ex.StackTrace?.Split('\\').Last()}\n{string.Join("\n", new StackTrace(2).ToString().Split("\n").Where(s => s.Contains(nameof(StarsectorTools))))}", STLogLevel.ERROR);
            sw.Flush();
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

    /// <summary>StarsectorTools全局工具</summary>
    public static class ST
    {
        /// <summary>StarsectorTools配置文件</summary>
        public const string configFile = @"Config.toml";

        /// <summary>StarsectorTools配置文件资源链接</summary>
        public static readonly Uri resourcesConfigUri = new("/Resources/Config.toml", UriKind.Relative);

        /// <summary>系统总内存</summary>
        public static int systemTotalMemory { get; private set; } = 0;

        /// <summary>游戏目录</summary>
        public static string gameDirectory { get; private set; } = null!;

        /// <summary>游戏exe文件路径</summary>
        public static string gameExeFile { get; private set; } = null!;

        /// <summary>游戏模组文件夹目录</summary>
        public static string gameModsDirectory { get; private set; } = null!;

        /// <summary>游戏版本</summary>
        public static string gameVersion { get; private set; } = null!;

        /// <summary>游戏保存文件夹目录</summary>
        public static string gameSaveDirectory { get; private set; } = null!;

        /// <summary>游戏已启用模组文件目录</summary>
        public static string enabledModsJsonFile { get; private set; } = null!;

        /// <summary>
        /// 设置游戏信息
        /// </summary>
        /// <param name="directoryName">游戏目录</param>
        public static void SetGameData(string directoryName)
        {
            gameExeFile = $"{directoryName}\\starsector.exe";
            if (File.Exists(gameExeFile))
            {
                gameDirectory = directoryName;
                gameModsDirectory = $"{directoryName}\\mods";
                gameSaveDirectory = $"{directoryName}\\saves";
                enabledModsJsonFile = $"{gameModsDirectory}\\enabled_mods.json";
                try
                {
                    gameVersion = JsonNode.Parse(File.ReadAllText($"{directoryName}\\starsector-core\\localization_version.json"))!.AsObject()["game_version"]!.GetValue<string>();
                }
                catch (Exception ex)
                {
                    STLog.Instance.WriteLine($"{I18n.LoadError} {I18n.Path}: {directoryName}", ex);
                }
            }
            else
            {
                gameExeFile = null!;
                STLog.Instance.WriteLine($"{I18n.GameDirectoryError} {I18n.Path}: {directoryName}", STLogLevel.ERROR);
                ShowMessageBox($"{I18n.GameDirectoryError}\n{I18n.Path}", MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 获取系统内存
        /// </summary>
        public static void GetSystemMemory()
        {
            systemTotalMemory = Management.GetMemoryMetricsNow().Total;
        }

        /// <summary>
        /// 复制文件夹至目标文件夹
        /// </summary>
        /// <param name="sourceDirectoryName">原始路径</param>
        /// <param name="destinationDirectoryName">目标路径</param>
        /// <returns>复制成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool CopyDirectory(string sourceDirectoryName, string destinationDirectoryName)
        {
            try
            {
                FileSystem.CopyDirectory(sourceDirectoryName, $"{destinationDirectoryName}\\{Path.GetFileName(sourceDirectoryName)}", UIOption.OnlyErrorDialogs);
                return true;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine(I18n.LoadError, ex);
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
                FileSystem.DeleteFile(file, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine(I18n.LoadError, ex);
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
                FileSystem.DeleteDirectory(directory, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine(I18n.LoadError, ex);
                return false;
            }
        }

        /// <summary>
        /// 检查内存信息
        /// </summary>
        /// <param name="size">内存大小</param>
        /// <returns>
        /// <para>如果<see langword="size"/>小于<see langword="1024"/>则会返回<see langword="1024"/></para>
        /// <para>如果<see langword="size"/>大于<see langword="systemTotalMemory"/>则会返回<see langword="systemTotalMemory"/></para>
        /// </returns>
        public static int CheckMemorySize(int size)
        {
            if (size < 1024)
            {
                ShowMessageBox($"{I18n.MinMemory} 1024", MessageBoxImage.Warning);
                size = 1024;
            }
            else if (size > systemTotalMemory)
            {
                ShowMessageBox($"{I18n.MaxMemory} {systemTotalMemory}", MessageBoxImage.Warning);
                size = systemTotalMemory;
            }
            return size;
        }

        /// <summary>
        /// 从资源中读取默认配置文件并创建
        /// </summary>
        public static void CreateConfigFile()
        {
            if (File.Exists(configFile))
                File.Delete(configFile);
            using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
            string str = sr.ReadToEnd();
            File.WriteAllText(configFile, str);
            STLog.Instance.WriteLine($"{I18n.ConfigFileCreatedSuccess} {configFile}");
        }

        /// <summary>
        /// 检测颜色是否为亮色调
        /// </summary>
        /// <param name="color">颜色</param>
        /// <returns>是为<see langword="true"/>,不是为<see langword="false"/></returns>
        public static bool IsLightColor(Color color)
        {
            return (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255 > 0.5;
        }

        /// <summary>
        /// 获取游戏目录
        /// </summary>
        /// <returns>获取成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool GetGameDirectory()
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
            string newDirectory = Path.GetDirectoryName(openFileDialog.FileName)!;
            if (File.Exists($"{newDirectory}\\starsector.exe"))
            {
                SetGameData(Path.GetDirectoryName(openFileDialog.FileName)!);
                STLog.Instance.WriteLine($"{I18n.GameDirectorySetupSuccess} {I18n.Path}: {newDirectory}");
                return true;
            }
            else
            {
                STLog.Instance.WriteLine($"{I18n.GameDirectoryError} {I18n.Path}: {newDirectory}", STLogLevel.WARN);
                return false;
            }
        }

        /// <summary>
        /// 使用系统默认打开方式打开文件或文件夹
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
        /// <param name="sourceDirectoryName">原始目录</param>
        /// <param name="destinationDirectoryName">输出目录</param>
        /// <param name="archiveName">压缩文件名</param>
        /// <returns>压缩成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool ArchiveDirToDir(string sourceDirectoryName, string destinationDirectoryName, string? archiveName = null)
        {
            if (!Directory.Exists(sourceDirectoryName))
                return false;
            try
            {
                using (var archive = ZipArchive.Create())
                {
                    archive.AddAllFromDirectory(sourceDirectoryName);
                    if (archiveName is null)
                        archive.SaveTo($"{destinationDirectoryName}\\{Path.GetFileName(sourceDirectoryName)}.zip", CompressionType.Deflate);
                    else
                        archive.SaveTo($"{destinationDirectoryName}\\{archiveName}.zip", CompressionType.Deflate);
                }
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.ZipFileError} {I18n.Path}: {sourceDirectoryName}", ex);
                return false;
            }
            return true;
        }

        /// <summary>
        /// <para>解压压缩文件至目录</para>
        /// <para>支持: <see langword="Zip"/> <see langword="Rar"/> <see langword="7z"/></para>
        /// </summary>
        /// <param name="sourceFileName">原始文件</param>
        /// <param name="destinationDirectoryName">输出目录</param>
        /// <returns>解压成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool UnArchiveFileToDir(string sourceFileName, string destinationDirectoryName)
        {
            if (!File.Exists(sourceFileName))
                return false;
            //读取压缩文件头,以判断压缩文件类型
            using StreamReader sr = new(sourceFileName);
            string head = $"{sr.Read()}{sr.Read()}";
            sr.Close();
            if (!Directory.Exists(destinationDirectoryName))
                Directory.CreateDirectory(destinationDirectoryName);
            try
            {
                if (head == "8075")//Zip文件
                {
                    using (var archive = new Archive(sourceFileName, new() { Encoding = Encoding.UTF8 }))
                    {
                        archive.ExtractToDirectory(destinationDirectoryName);
                    }
                }
                else if (head == "8297")//Rar文件
                {
                    using (var archive = new RarArchive(sourceFileName))
                    {
                        archive.ExtractToDirectory(destinationDirectoryName);
                    }
                }
                else if (head == "55122")//7z文件
                {
                    using (var archive = new SevenZipArchive(sourceFileName))
                    {
                        archive.ExtractToDirectory(destinationDirectoryName);
                    }
                }
                else
                    throw new();
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.ZipFileError}  {I18n.Path}: {sourceFileName}", ex);
                if (Directory.Exists(destinationDirectoryName))
                    Directory.Delete(destinationDirectoryName);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 弹出消息窗口
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="image">显示的图标</param>
        /// <returns>按钮结果: <see cref="MessageBoxResult"/></returns>
        public static MessageBoxResult ShowMessageBox(string message, MessageBoxImage image = MessageBoxImage.Information)
        {
            return ShowMessageBox(message, "", image: image);
        }

        /// <summary>
        /// 弹出消息窗口
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="button">显示的按钮</param>
        /// <param name="image">显示的图标</param>
        /// <returns>按钮结果: <see cref="MessageBoxResult"/></returns>
        public static MessageBoxResult ShowMessageBox(string message, MessageBoxButton button, MessageBoxImage image)
        {
            return ShowMessageBox(message, "", button, image);
        }

        /// <summary>
        /// 弹出消息窗口
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="caption">标头</param>
        /// <param name="button">显示的按钮</param>
        /// <param name="image">显示的图片</param>
        /// <param name="result">默认按钮结果</param>
        /// <param name="options">窗口设置</param>
        /// <returns>按钮结果: <see cref="MessageBoxResult"/></returns>
        public static MessageBoxResult ShowMessageBox(string message, string caption, MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None, MessageBoxResult result = MessageBoxResult.None, MessageBoxOptions options = MessageBoxOptions.ServiceNotification)
        {
            return MessageBox.Show(message, caption, button, image, result, options);
        }

        /// <summary>
        /// 为主窗口设置模糊效果,用于聚焦弹窗
        /// </summary>
        public static void SetMainWindowBlurEffect() => ((MainWindow)Application.Current.MainWindow).SetBlurEffect();

        /// <summary>
        /// 取消主窗口的模糊效果
        /// </summary>
        public static void RemoveMainWIndowBlurEffect() => ((MainWindow)Application.Current.MainWindow).RemoveBlurEffect();
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

        private void SetData(string key, JsonNode value)
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
                    if (Dependencies.Count == 0)
                        Dependencies = null;
                    break;
            }
        }
    }
}