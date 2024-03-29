﻿using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Aspose.Zip;
using Aspose.Zip.Rar;
using Aspose.Zip.SevenZip;
using Microsoft.VisualBasic.FileIO;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.ViewModels.Main;
using I18nRes = StarsectorToolbox.Langs.Libs.UtilsI18nRes;

namespace StarsectorToolbox.Libs;

/// <summary>通用方法</summary>
public static class Utils
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();
    private const string c_ZipFileHeadCode = "8075";
    private const string c_RarFileHeadCode = "8297";
    private const string c_7ZFileHeadCode = "55122";
    private static readonly Regex sr_jsonCommentsRegex =
        new(@"(?<!:""[^""]*)#.*", RegexOptions.Compiled);
    private static readonly Regex sr_jsonCommasRegex =
        new(@",(?=[ \t\r\n]*[\]\}])|(?<=[\]\}]),[ \t\r\n]*\Z", RegexOptions.Compiled);
    private static readonly Regex sr_jsonQuotesRegex =
        new(@"(""\w*""[ \t]*:[ \t]*)'(\w*)'", RegexOptions.Compiled);
    private static readonly Regex sr_jsonPropertyRegex = new(@"id:""", RegexOptions.Compiled);

    /// <summary>
    /// 检测文件是否存在
    /// </summary>
    /// <param name="file">文件路径</param>
    /// <param name="outputLog">输出日志</param>
    /// <returns>存在为<see langword="true"/>,不存在为<see langword="false"/></returns>
    public static bool FileExists(string file, bool outputLog = true)
    {
        bool isExists = File.Exists(file);
        if (isExists is false && outputLog)
            sr_logger.Warn($"{I18nRes.FileNotFound} {I18nRes.Path}: {file}");
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
        if (exists is false && outputLog)
            sr_logger.Warn($"{I18nRes.DirectoryNotFound} {I18nRes.Path}: {directory}");
        return exists;
    }

    /// <summary>
    /// 从Json文件中读取数据并格式化,去除掉注释以及不合规的逗号
    /// </summary>
    /// <param name="file">文件</param>
    /// <returns>格式化后的数据</returns>
    public static string? JsonParse2String(string file)
    {
        if (File.Exists(file) is false)
            return null;
        string jsonData = File.ReadAllText(file);
        // 清除json中的注释
        jsonData = sr_jsonCommentsRegex.Replace(jsonData, "");
        // 清除json中不符合规定的逗号
        jsonData = sr_jsonCommasRegex.Replace(jsonData, "");
        // 将单引号转换成双引号
        jsonData = sr_jsonQuotesRegex.Replace(jsonData, "$1\"$2\"");
        // 将异常格式 id:" 变为 "id":"
        jsonData = sr_jsonPropertyRegex.Replace(jsonData, @"""id"":""");
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
            sr_logger.Error(ex, $"{I18nRes.LoadError} {file}");
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
            sr_logger.Error(ex, I18nRes.LoadError);
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
            sr_logger.Error(ex, I18nRes.LoadError);
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
            sr_logger.Error(ex, I18nRes.LoadError);
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
            sr_logger.Error(ex, I18nRes.LoadError);
            return false;
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
            sr_logger.Error(ex, $"{I18nRes.ProcessStartError} {link}");
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
            sr_logger.Error(ex, $"{I18nRes.ProcessStartError} {file}");
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
                    archiveFile = Path.Combine(destDirectory, Path.GetFileName(sourceDirectory));
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
            sr_logger.Error(ex, $"{I18nRes.ZipFileError} {I18nRes.Path}: {sourceDirectory}");
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
    public static async Task<bool> UnArchiveFileToDirectory(string sourceFile, string destDirectory)
    {
        if (FileExists(sourceFile) is false)
            return false;
        //读取压缩文件头,以判断压缩文件类型
        using StreamReader sr = new(sourceFile);
        string head = $"{sr.Read()}{sr.Read()}";
        sr.Close();
        var destDirectoryExists = true;
        if (Directory.Exists(destDirectory) is false)
        {
            destDirectoryExists = false;
            Directory.CreateDirectory(destDirectory);
        }
        try
        {
            await Task.Run(() =>
            {
                if (head == c_ZipFileHeadCode) //Zip文件
                {
                    using var archive = new Archive(sourceFile, new() { Encoding = Encoding.UTF8 });
                    archive.ExtractToDirectory(destDirectory);
                }
                else if (head == c_RarFileHeadCode) //Rar文件
                {
                    using var archive = new RarArchive(sourceFile);
                    archive.ExtractToDirectory(destDirectory);
                }
                else if (head == c_7ZFileHeadCode) //7z文件
                {
                    using var archive = new SevenZipArchive(sourceFile);
                    archive.ExtractToDirectory(destDirectory);
                }
                else
                    throw new(I18nRes.UnsupportedCompressedFiles);
            });
        }
        catch (Exception ex)
        {
            sr_logger.Error(ex, $"{I18nRes.ZipFileError}  {I18nRes.Path}: {sourceFile}");
            if (destDirectoryExists is false && Directory.Exists(destDirectory))
                Directory.Delete(destDirectory);
            return false;
        }
        return true;
    }

    /// <summary>
    /// 清理游戏日志
    /// </summary>
    /// <param name="toRecycleBin">至回收站</param>
    /// <param name="gFileCount">额外日志文件数量</param>
    public static void ClearGameLog(bool toRecycleBin = true, int gFileCount = 10)
    {
        var logFile = GameInfo.LogFile;
        if (File.Exists(logFile))
        {
            if (toRecycleBin)
                DeleteFileToRecycleBin(logFile);
            else
                File.Delete(logFile);
        }
        File.Create(logFile).Close();
        for (int i = 1; i < gFileCount; i++)
        {
            var gFile = $"{logFile}.{i}";
            if (File.Exists(gFile))
                File.Delete(gFile);
        }
        sr_logger.Info(I18nRes.GameLogCleanupCompleted);
    }

    /// <summary>
    /// 从文件获取只读流 (用于目标文件被其它进程访问的情况)
    /// </summary>
    /// <param name="file">文件</param>
    /// <param name="encoding">编码</param>
    /// <param name="detectEncodingFromByteOrderMarks">从字节顺序标记检测编码</param>
    /// <returns>流读取器</returns>
    public static StreamReader StreamReaderOnReadOnly(
        string file,
        Encoding? encoding = null,
        bool detectEncodingFromByteOrderMarks = false
    )
    {
        encoding ??= Encoding.UTF8;
        return new StreamReader(
            file,
            encoding,
            detectEncodingFromByteOrderMarks,
            new FileStreamOptions()
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.ReadWrite
            }
        );
    }

    /// <summary>
    /// 从流中读取行
    /// </summary>
    /// <param name="sr">流读取器</param>
    /// <returns>行数据</returns>
    public static IEnumerable<string> GetLinesOnStreamReader(StreamReader sr)
    {
        string? line;
        while ((line = sr.ReadLine()) is not null)
        {
            yield return line;
        }
    }

    /// <summary>
    /// 为主窗口设置模糊效果
    /// </summary>
    public static void SetMainWindowBlurEffect(bool mainWindowIsEnabled = false) =>
        MainWindowViewModel.Instance.SetBlurEffect(mainWindowIsEnabled);

    /// <summary>
    /// 取消主窗口的模糊效果
    /// </summary>
    public static void RemoveMainWindowBlurEffect() =>
        MainWindowViewModel.Instance.RemoveBlurEffect();
}
