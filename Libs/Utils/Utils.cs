using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aspose.Zip;
using Aspose.Zip.Rar;
using Aspose.Zip.SevenZip;
using HKW.Libs.Log4Cs;
using Microsoft.VisualBasic.FileIO;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using StarsectorTools.ViewModels.MainWindow;
using I18n = StarsectorTools.Langs.Libs.UtilsI18nRes;

namespace StarsectorTools.Libs.Utils
{
    /// <summary>通用方法</summary>
    public static class Utils
    {
        /// <summary>
        /// 检测文件是否存在
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <param name="outputLog">输出日志</param>
        /// <returns>存在为<see langword="true"/>,不存在为<see langword="false"/></returns>
        public static bool FileExists(string file, bool outputLog = true)
        {
            bool isExists = File.Exists(file);
            if (!isExists && outputLog)
                Logger.Warring($"{I18n.FileNotFound} {I18n.Path}: {file}");
            return isExists;
        }

        /// <summary>
        /// 检测文件夹是否存在
        /// </summary>
        /// <param name="directory">目录路径</param>
        /// <param name="outputLog">输出日志</param>
        /// <returns>存在为<see langword="true"/>,不存在为<see langword="false"/></returns>
        public static bool DirectoryExists(string directory, bool outputLog = true)
        {
            bool exists = Directory.Exists(directory);
            if (!exists && outputLog)
                Logger.Warring($"{I18n.DirectoryNotFound} {I18n.Path}: {directory}");
            return exists;
        }

        /// <summary>
        /// 从Json文件中读取数据并格式化,去除掉注释以及不合规的逗号
        /// </summary>
        /// <param name="file">文件</param>
        /// <returns>格式化后的数据</returns>
        public static string? JsonParse2String(string file)
        {
            if (!FileExists(file))
                return null;
            string jsonData = File.ReadAllText(file);
            // 清除json中的注释
            jsonData = Regex.Replace(jsonData, @"(#|//)[\S ]*", "");
            // 清除json中不符合规定的逗号
            jsonData = Regex.Replace(
                jsonData,
                @",(?=[ \t\r\n]*[\]\}])|(?<=[\]\}]),[ \t\r\n]*\Z",
                ""
            );
            // 将异常格式 id:" 变为 "id":"
            jsonData = Regex.Replace(jsonData, @"id:""", @"""id"":""");
            return jsonData;
        }

        /// <summary>
        /// 从Json文件中读取数据并格式化,去除掉注释以及不合规的逗号
        /// </summary>
        /// <param name="file">Json文件</param>
        /// <returns>格式化后的Json对象</returns>
        public static JsonObject? JsonParse2Object(string file)
        {
            if (JsonParse2String(file) is not string jsonData)
                return null;
            JsonObject? jsonObject = null;
            try
            {
                jsonObject = JsonNode
                    .Parse(
                        jsonData,
                        documentOptions: new()
                        {
                            AllowTrailingCommas = true,
                            CommentHandling = System.Text.Json.JsonCommentHandling.Skip
                        }
                    )
                    ?.AsObject();
            }
            catch (Exception ex)
            {
                Logger.Error($"{I18n.LoadError} {file}", ex);
            }
            return jsonObject;
        }

        /// <summary>
        /// 保存json数据至文件
        /// </summary>
        /// <param name="jsonNode">json数据</param>
        /// <param name="file">文件</param>
        public static void SaveTo(this JsonNode jsonNode, string file)
        {
            File.WriteAllText(file, jsonNode.ToUTF8String());
        }

        /// <summary>
        /// 转换json数据至字符串,采用格式化及不严格的编码器(不会生成中文乱码)
        /// </summary>
        /// <param name="jsonNode">json数据</param>
        /// <returns>格式化及无乱码的字符串</returns>
        public static string ToUTF8String(this JsonNode jsonNode) =>
            jsonNode.ToJsonString(
                new()
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }
            );

        /// <summary>
        /// 复制文件夹至目标文件夹内
        /// </summary>
        /// <param name="sourceDirectory">原始路径</param>
        /// <param name="destDirectory">目标路径</param>
        /// <returns>复制成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool CopyDirectory(string sourceDirectory, string destDirectory)
        {
            try
            {
                FileSystem.CopyDirectory(
                    sourceDirectory,
                    Path.Combine(destDirectory, Path.GetFileName(sourceDirectory)),
                    UIOption.OnlyErrorDialogs
                );
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(I18n.LoadError, ex);
                return false;
            }
        }

        /// <summary>
        /// 移动文件夹至目标文件夹内
        /// </summary>
        /// <param name="sourceDirectory">原始路径</param>
        /// <param name="destDirectory">目标路径</param>
        /// <returns>移动成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool MoveDirectory(string sourceDirectory, string destDirectory)
        {
            try
            {
                FileSystem.MoveDirectory(
                    sourceDirectory,
                    Path.Combine(destDirectory, Path.GetFileName(sourceDirectory)),
                    UIOption.OnlyErrorDialogs
                );
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(I18n.LoadError, ex);
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
                FileSystem.DeleteFile(
                    file,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin
                );
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(I18n.LoadError, ex);
                return false;
            }
        }

        /// <summary>
        /// 删除文件夹至回收站
        /// </summary>
        /// <param name="directory">文件夹</param>
        /// <returns>删除成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool DeleteDirectoryToRecycleBin(string directory)
        {
            try
            {
                FileSystem.DeleteDirectory(
                    directory,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin
                );
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(I18n.LoadError, ex);
                return false;
            }
        }

        /// <summary>
        /// 获取所有文件(包括子目录)
        /// </summary>
        /// <param name="directory">目录</param>
        /// <returns>所有文件信息</returns>
        public static List<FileInfo>? GetAllSubFiles(string directory)
        {
            if (!Directory.Exists(directory))
                return null!;
            var fileInfos = new List<FileInfo>();
            GetSubFiles(new DirectoryInfo(directory), fileInfos);
            return fileInfos;

            static void GetSubFiles(DirectoryInfo directory, List<FileInfo> fileInfos)
            {
                fileInfos.AddRange(directory.GetFiles());
                foreach (var directoryInfo in directory.GetDirectories())
                    GetSubFiles(new(directoryInfo.FullName), fileInfos);
            }
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
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true })?.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"{I18n.ProcessStartError} {link}", ex);
                return false;
            }
        }

        /// <summary>
        /// 在文件资源管理器中打开文件夹并定位到指定文件
        /// </summary>
        /// <param name="file">文件路径</param>
        /// <returns>打开成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static bool OpenDirectoryAndLocateFile(string file)
        {
            try
            {
                Process.Start("Explorer", $"/select,{file.Replace("/", "\\")}")?.Close();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"{I18n.ProcessStartError} {file}", ex);
                return false;
            }
        }

        /// <summary>
        /// <para>压缩文件夹至Zip文件并输出到目录</para>
        /// <para>若不输入压缩文件名,则以原始目录的文件夹名称来命名</para>
        /// </summary>
        /// <param name="sourceDirectory">原始目录</param>
        /// <param name="destDirectory">输出目录</param>
        /// <param name="fileName">压缩文件名</param>
        /// <returns>压缩成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static async Task<bool> ArchiveDirectoryToFile(
            string sourceDirectory,
            string destDirectory,
            string? fileName = null
        )
        {
            if (!DirectoryExists(sourceDirectory))
                return false;
            try
            {
                await Task.Run(() =>
                {
                    using var archive = ZipArchive.Create();
                    archive.AddAllFromDirectory(sourceDirectory);
                    var archiveFile = string.Empty;
                    if (string.IsNullOrWhiteSpace(fileName))
                        archiveFile = Path.Combine(
                            destDirectory,
                            Path.GetFileName(sourceDirectory)
                        );
                    else
                        archiveFile = Path.Combine(destDirectory, fileName);
                    if (!archiveFile.EndsWith(".zip"))
                        archiveFile += ".zip";
                    archive.SaveTo(archiveFile, CompressionType.Deflate);
                });
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"{I18n.ZipFileError} {I18n.Path}: {sourceDirectory}", ex);
                return false;
            }
        }

        /// <summary>
        /// <para>解压压缩文件至目录</para>
        /// <para>支持: <see langword="Zip"/> <see langword="Rar"/> <see langword="7z"/></para>
        /// </summary>
        /// <param name="sourceFile">原始文件</param>
        /// <param name="destDirectory">输出目录</param>
        /// <returns>解压成功为<see langword="true"/>,失败为<see langword="false"/></returns>
        public static async Task<bool> UnArchiveFileToDirectory(
            string sourceFile,
            string destDirectory
        )
        {
            if (!FileExists(sourceFile))
                return false;
            //读取压缩文件头,以判断压缩文件类型
            using StreamReader sr = new(sourceFile);
            string head = $"{sr.Read()}{sr.Read()}";
            sr.Close();
            if (!DirectoryExists(destDirectory, false))
                Directory.CreateDirectory(destDirectory);
            try
            {
                await Task.Run(() =>
                {
                    if (head == "8075") //Zip文件
                    {
                        using var archive = new Archive(
                            sourceFile,
                            new() { Encoding = Encoding.UTF8 }
                        );
                        archive.ExtractToDirectory(destDirectory);
                    }
                    else if (head == "8297") //Rar文件
                    {
                        using var archive = new RarArchive(sourceFile);
                        archive.ExtractToDirectory(destDirectory);
                    }
                    else if (head == "55122") //7z文件
                    {
                        using var archive = new SevenZipArchive(sourceFile);
                        archive.ExtractToDirectory(destDirectory);
                    }
                    else
                        throw new("不支持的压缩文件");
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"{I18n.ZipFileError}  {I18n.Path}: {sourceFile}", ex);
                if (DirectoryExists(destDirectory, false))
                    Directory.Delete(destDirectory);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 为主窗口设置模糊效果,用于聚焦弹窗
        /// </summary>
        public static void SetMainWindowBlurEffect(bool mainWindowIsEnabled = false) =>
            MainWindowViewModel.Instance.SetBlurEffect(mainWindowIsEnabled);

        /// <summary>
        /// 取消主窗口的模糊效果
        /// </summary>
        public static void RemoveMainWindowBlurEffect() =>
            MainWindowViewModel.Instance.RemoveBlurEffect();
    }
}
