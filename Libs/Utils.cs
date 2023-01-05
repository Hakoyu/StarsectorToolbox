using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Aspose.Zip;
using Aspose.Zip.Rar;
using Aspose.Zip.SevenZip;
using HKW.TomlParse;
using Microsoft.VisualBasic.FileIO;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using StarsectorTools.Windows;
using I18n = StarsectorTools.Langs.Libs.Utils_I18n;

namespace StarsectorTools.Utils
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
    public static class STLog
    {
        /// <summary>日志目录</summary>
        public const string logFile = $"{ST.CoreDirectory}\\StarsectorTools.log";

        /// <summary>日志等级</summary>
        public static STLogLevel LogLevel = STLogLevel.INFO;

        /// <summary>写入流</summary>
        private static StreamWriter sw = new(logFile);

        /// <summary>读写锁</summary>
        private static ReaderWriterLockSlim rwLockS = new();

        /// <summary>
        /// 字符串转换成日志等级
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>日志等级</returns>
        public static STLogLevel Str2STLogLevel(string str) =>
        str switch
        {
            nameof(STLogLevel.DEBUG) => STLogLevel.DEBUG,
            nameof(STLogLevel.INFO) => STLogLevel.INFO,
            nameof(STLogLevel.WARN) => STLogLevel.WARN,
            nameof(STLogLevel.ERROR) => STLogLevel.ERROR,
            _ => STLogLevel.INFO
        };

        /// <summary>
        /// 获取所在类名和方法名
        /// </summary>
        /// <returns>类型名和方法名</returns>
        private static string? GetClassNameAndMethodName()
        {
            var method = new StackTrace().GetFrames().First(f => f.GetMethod()?.DeclaringType?.Name != nameof(STLog))?.GetMethod();
            return $"{method?.DeclaringType?.Name}.{method?.Name}";
        }

        /// <summary>
        /// 获取所在类名
        /// </summary>
        /// <returns></returns>
        private static string? GetClassName()
        {
            var frame = new StackTrace().GetFrames().First(f => f.GetMethod()?.DeclaringType?.Name != nameof(STLog));
            return frame?.GetMethod()?.DeclaringType?.Name;
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        public static void WriteLine(string message, STLogLevel logLevel = STLogLevel.INFO)
        {
            WriteLine(message, logLevel, null!);
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="args">插入的对象</param>
        public static void WriteLine(string message, STLogLevel logLevel = STLogLevel.INFO, params object[] args)
        {
            rwLockS.EnterWriteLock();
            try
            {
                if (logLevel >= LogLevel)
                {
                    string? name;
                    if (LogLevel == STLogLevel.DEBUG)
                        name = GetClassNameAndMethodName();
                    else
                        name = GetClassName();
                    sw.WriteLine($"[{name}] {logLevel} {KeyParse(message, args)}");
                    sw.Flush();
                }
            }
            finally
            {
                rwLockS.ExitWriteLock();
            }
        }

        /// <summary>
        /// 写入捕获的异常
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ex">错误</param>
        /// <param name="args">插入的对象</param>
        public static void WriteLine(string message, Exception ex, params object[] args)
        {
            rwLockS.EnterWriteLock();
            try
            {
                sw.WriteLine($"[{GetClassName()}] {STLogLevel.ERROR} {KeyParse(message, args)}");
                sw.WriteLine(ExceptionParse(ex));
                sw.Flush();
            }
            finally
            {
                rwLockS.ExitWriteLock();
            }
        }

        private static string KeyParse(string str, params object[] args)
        {
            try
            {
                return string.Format(str, args);
            }
            catch
            {
                return str;
            }
        }

        /// <summary>
        /// Exception解析 用来精简异常的堆栈输出
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns></returns>
        public static string ExceptionParse(Exception ex)
        {
            var list = ex.ToString().Split("\r\n").Where(s => !s.Contains("at System.") && !s.Contains("at MS.") && !s.Contains("End of inner exception stack trace"));
            return Regex.Replace(string.Join("\r\n", list), @$"[\S]+(?={nameof(StarsectorTools)})", "");
        }

        /// <summary>关闭</summary>
        public static void Close()
        {
            if (GetClassName() == nameof(MainWindow))
                sw?.Close();
        }
    }

    /// <summary>StarsectorTools全局工具</summary>
    public static class ST
    {
        public const string CoreDirectory = "Core";

        /// <summary>StarsectorTools配置文件</summary>
        public const string ConfigTomlFile = $"{CoreDirectory}\\Config.toml";

        /// <summary>游戏目录</summary>
        public static string GameDirectory { get; private set; } = null!;

        /// <summary>游戏exe文件</summary>
        public static string GameExeFile { get; private set; } = null!;

        /// <summary>游戏模组文件夹</summary>
        public static string GameModsDirectory { get; private set; } = null!;

        /// <summary>游戏版本</summary>
        public static string GameVersion { get; private set; } = null!;

        /// <summary>游戏存档文件夹</summary>
        public static string GameSaveDirectory { get; private set; } = null!;

        /// <summary>游戏已启用模组文件</summary>
        public static string EnabledModsJsonFile { get; private set; } = null!;

        /// <summary>游戏日志文件</summary>
        public static string GameLogFile { get; private set; } = null!;

        /// <summary>
        /// 检测文件是否存在,若不存在会自动输出日志
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>存在为<see langword="true"/>,不存在为<see langword="false"/></returns>
        public static bool FileExists(string path, bool logOutputIfNotFound = true)
        {
            bool isExists = File.Exists(path);
            if (!isExists && logOutputIfNotFound)
                STLog.WriteLine($"{I18n.FileNotFound} {I18n.Path}: {path}", STLogLevel.WARN);
            return isExists;
        }
        /// <summary>
        /// 检测文件夹是否存在,若不存在会自动输出日志
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>存在为<see langword="true"/>,不存在为<see langword="false"/></returns>
        public static bool DirectoryExists(string path, bool logOutputIfNotFound = true)
        {
            bool exists = Directory.Exists(path);
            if (!exists && logOutputIfNotFound)
                STLog.WriteLine($"{I18n.DirectoryNotFound} {I18n.Path}: {path}", STLogLevel.WARN);
            return exists;
        }

        /// <summary>
        /// 格式化Json数据,去除掉注释以及不合规的逗号
        /// </summary>
        /// <param name="jsonData">Json数据</param>
        /// <returns>格式化后的数据</returns>
        public static string JsonParse(string jsonData)
        {
            // 清除json中的注释
            jsonData = Regex.Replace(jsonData, @"(#|//)[\S ]*", "");
            // 清除json中不符合规定的逗号
            jsonData = Regex.Replace(jsonData, @",(?=[\r\n \t]*[\]\}])|(?<=[\}\]]),[ \t]*\r?\Z", "");
            return jsonData;
        }

        /// <summary>
        /// 设置游戏信息
        /// </summary>
        /// <param name="directoryName">游戏目录</param>
        public static bool SetGameData(string directoryName)
        {
            GameExeFile = $"{directoryName}\\starsector.exe";
            if (FileExists(GameExeFile, false))
            {
                GameDirectory = directoryName;
                GameModsDirectory = $"{directoryName}\\mods";
                GameSaveDirectory = $"{directoryName}\\saves";
                EnabledModsJsonFile = $"{GameModsDirectory}\\enabled_mods.json";
                GameLogFile = $"{directoryName}\\starsector-core\\starsector.log";
                try
                {
                    GameVersion = JsonNode.Parse(File.ReadAllText($"{directoryName}\\starsector-core\\localization_version.json"))!.AsObject()["game_version"]!.GetValue<string>();
                    return true;
                }
                catch (Exception ex)
                {
                    STLog.WriteLine($"{I18n.LoadError} {I18n.Path}: {directoryName}", ex);
                }
            }
            else
            {
                GameExeFile = null!;
                STLog.WriteLine($"{I18n.GameDirectoryError} {I18n.Path}: {directoryName}", STLogLevel.ERROR);
                ShowMessageBox($"{I18n.GameDirectoryError}\n{I18n.Path}", MessageBoxImage.Error);
            }
            return false;
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
                STLog.WriteLine(I18n.LoadError, ex);
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
                STLog.WriteLine(I18n.LoadError, ex);
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
                STLog.WriteLine(I18n.LoadError, ex);
                return false;
            }
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
            if (SetGameData(Path.GetDirectoryName(openFileDialog.FileName)!))
            {
                STLog.WriteLine($"{I18n.GameDirectorySetCompleted} {I18n.Path}: {newDirectory}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 使用系统默认打开方式打开链接,文件或文件夹
        /// </summary>
        /// <param name="link">链接</param>
        /// <returns>打开成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool OpenLink(string link)
        {
            try
            {
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
                return true;
            }
            catch (Exception ex)
            {
                STLog.WriteLine(I18n.LinkError, ex);
                return false;
            }
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
            if (!DirectoryExists(sourceDirectoryName))
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
                return true;
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.ZipFileError} {I18n.Path}: {sourceDirectoryName}", ex);
                return false;
            }
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
            if (!FileExists(sourceFileName))
                return false;
            //读取压缩文件头,以判断压缩文件类型
            using StreamReader sr = new(sourceFileName);
            string head = $"{sr.Read()}{sr.Read()}";
            sr.Close();
            if (!DirectoryExists(destinationDirectoryName, false))
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
                STLog.WriteLine($"{I18n.ZipFileError}  {I18n.Path}: {sourceFileName}", ex);
                if (DirectoryExists(destinationDirectoryName, false))
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
        public static MessageBoxResult ShowMessageBox(string message,
                                                      MessageBoxImage image = MessageBoxImage.Information,
                                                      bool setBlurEffect = true)
        {
            return ShowMessageBox(message, " ", image: image, setBlurEffect: setBlurEffect);
        }

        /// <summary>
        /// 弹出消息窗口
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="button">显示的按钮</param>
        /// <param name="image">显示的图标</param>
        /// <returns>按钮结果: <see cref="MessageBoxResult"/></returns>
        public static MessageBoxResult ShowMessageBox(string message,
                                                      MessageBoxButton button,
                                                      MessageBoxImage image,
                                                      bool setBlurEffect = true)
        {
            return ShowMessageBox(message, " ", button, image, setBlurEffect: setBlurEffect);
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
        public static MessageBoxResult ShowMessageBox(string message,
                                                      string caption,
                                                      MessageBoxButton button = MessageBoxButton.OK,
                                                      MessageBoxImage image = MessageBoxImage.None,
                                                      MessageBoxResult result = MessageBoxResult.None,
                                                      bool setBlurEffect = true)
        {
            if (setBlurEffect)
            {
                SetMainWindowBlurEffect();
                var outResult = MessageBox.Show(message, caption, button, image, result);
                RemoveMainWindowBlurEffect();
                return outResult;
            }
            else
            {
                return MessageBox.Show(message, caption, button, image, result);
            }
        }

        /// <summary>
        /// 为主窗口设置模糊效果,用于聚焦弹窗
        /// </summary>
        public static void SetMainWindowBlurEffect() => ((MainWindow)Application.Current.MainWindow).SetBlurEffect();

        /// <summary>
        /// 取消主窗口的模糊效果
        /// </summary>
        public static void RemoveMainWindowBlurEffect() => ((MainWindow)Application.Current.MainWindow).RemoveBlurEffect();
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