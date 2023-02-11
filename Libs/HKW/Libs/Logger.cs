using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HKW.Libs.Log4Cs
{
    /// <summary>
    /// 记录器
    /// </summary>
    public class Logger
    {
        /// <summary>日志等级</summary>
        public enum Level
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
        /// <summary>日志文件</summary>
        public static string LogFile { get; private set; } = string.Empty;

        /// <summary>基命名空间</summary>
        public static string BaseNamespace { get; private set; } = string.Empty;

        /// <summary>设置</summary>
        public static InitializeOptions Options { get; set; } = null!;

        /// <summary>写入流</summary>
        private static StreamWriter sw = null!;

        /// <summary>读写锁</summary>
        private static ReaderWriterLockSlim rwLockS = new();

        /// <summary>
        /// 初始化设置
        /// </summary>
        public class InitializeOptions
        {
            /// <summary>
            /// 默认日志等级
            /// </summary>
            public Level DefaultLevel { get; set; } = Level.INFO;
            /// <summary>
            /// 默认附加
            /// </summary>
            public bool DefaultAppend { get; set; } = false;
            /// <summary>
            /// 默认过滤异常
            /// </summary>
            public bool DefaultFilterException { get; set; } = false;
            /// <summary>
            /// 默认显示方法名
            /// </summary>
            public bool DefaultShowMethod { get; set; } = true;
            /// <summary>
            /// 默认显示类名
            /// </summary>
            public bool DefaultShowClass { get; set; } = true;
            /// <summary>
            /// 默认显示命名空间
            /// </summary>
            public bool DefaultShowNameSpace { get; set; } = false;
            /// <summary>
            /// 默认显示线程Id
            /// </summary>
            public bool DefaultShowThreadId { get; set; } = false;
            /// <summary>
            /// 默认显示时间
            /// </summary>
            public bool DefaultShowTime { get; set; } = false;
            /// <summary>
            /// 默认显示日期
            /// </summary>
            public bool DefaultShowDate { get; set; } = false;
            /// <summary>
            /// 异常过滤器
            /// </summary>
            public List<string> ExceptionFilter { get; set; } = new()
            {
                "at System.",
                "at MS.",
                "at Microsoft.",
                "End of inner exception stack trace",
            };
        }

        private Logger() { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="baseNamespce">基命名空间</param>
        /// <param name="logFile">日志文件</param>
        /// <param name="initializeOptions">初始化设置</param>
        public static void Initialize(string baseNamespce, string logFile, InitializeOptions? initializeOptions = null)
        {
            Options = initializeOptions ?? new();
            LogFile = logFile;
            BaseNamespace = baseNamespce;
            sw?.Close();
            sw = new(LogFile, Options.DefaultAppend);
        }

        /// <summary>
        /// 字符串转换成日志等级
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>日志等级</returns>
        public static Level LevelConverter(string str) =>
            str.ToUpper() switch
            {
                nameof(Level.DEBUG) => Level.DEBUG,
                nameof(Level.INFO) => Level.INFO,
                nameof(Level.WARN) => Level.WARN,
                nameof(Level.ERROR) => Level.ERROR,
                _ => Level.INFO
            };

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">消息</param>
        public static void Debug(string message) =>
            RecordBase(message, Level.DEBUG, null, null);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">消息</param>
        public static void Info(string message) =>
            RecordBase(message, Level.INFO, null, null);
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">消息</param>
        public static void Warring(string message) =>
            RecordBase(message, Level.WARN, null, null);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">消息</param>
        public static void Error(string message) =>
            RecordBase(message, Level.ERROR, null, null);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ex">异常</param>
        public static void Error(string message, Exception ex) =>
            RecordBase(message, Level.ERROR, ex, null);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ex">异常</param>
        /// <param name="filterException">过滤器</param>
        public static void Error(string message, Exception ex, bool filterException) =>
            RecordBase(message, Level.ERROR, ex, filterException);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        public static void Record(string message) =>
            RecordBase(message, Options.DefaultLevel, null, null);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        public static void Record(string message, Level logLevel) =>
            RecordBase(message, logLevel, null, null);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ex">异常</param>
        public static void Record(string message, Exception ex) =>
            RecordBase(message, Level.ERROR, ex, null);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ex">异常</param>
        /// <param name="filterException">过滤器</param>
        public static void Record(string message, Exception ex, bool filterException) =>
            RecordBase(message, Level.ERROR, ex, filterException);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="ex">异常</param>
        public static void Record(string message, Level logLevel, Exception ex) =>
            RecordBase(message, logLevel, ex, null);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="ex">异常</param>
        /// <param name="filterException">过滤器</param>
        public static void Record(string message, Level logLevel, Exception ex, bool filterException) =>
            RecordBase(message, logLevel, ex, filterException);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="ex">异常</param>
        /// <param name="filterException">过滤器</param>
        private static void RecordBase(string message, Level logLevel, Exception? ex, bool? filterException)
        {
            rwLockS.EnterWriteLock();
            try
            {
                if (logLevel >= Options.DefaultLevel)
                {
                    string origin = GetOriginMessage(logLevel);
                    string dateTime = GetDataTimeMessage();
                    string threadId = GetThreadIdMessage();
                    string exMessage = GetExceptionMessage(ex, filterException);
                    sw.WriteLine($"{dateTime}[{origin}{threadId}] {logLevel} {message}{exMessage}");
                    sw.Flush();
                }
            }
            finally
            {
                rwLockS.ExitWriteLock();
            }
        }

        /// <summary>
        /// 获取源信息
        /// </summary>
        /// <param name="getClass">获取类</param>
        /// <param name="getNamespace">获取命名空间</param>
        /// <param name="getMethod">获取方法</param>
        /// <returns>源信息</returns>
        private static string GetOrigin(bool getClass, bool getNamespace, bool getMethod)
        {
            var method = new StackTrace().GetFrames().First(f => f.GetMethod()?.DeclaringType?.Name != nameof(Logger))?.GetMethod();
            var strs = new List<string>();
            if (getNamespace && method?.DeclaringType?.Namespace is string strNamespace)
                strs.Add(strNamespace);
            if (getClass && method?.DeclaringType?.Name is string strClass)
                strs.Add(strClass);
            if (getMethod && method?.Name is string strMethod)
                strs.Add(strMethod);
            return string.Join(".", strs);
        }
        /// <summary>
        /// 获取线程Id
        /// </summary>
        /// <returns>线程Id</returns>
        private static string GetThreadId()
        {
            return Options.DefaultShowThreadId ? Thread.CurrentThread.ManagedThreadId.ToString() : string.Empty;
        }
        /// <summary>
        /// 根据默认值获取日期时间
        /// </summary>
        /// <returns>日期时间</returns>
        private static string GetDataTime()
        {
            string dateTime = string.Empty;
            var nowDateTime = DateTime.Now;
            if (Options.DefaultShowDate)
                dateTime += $"{nowDateTime.Year}/{nowDateTime.Month}/{nowDateTime.Day} ";
            if (Options.DefaultShowTime)
                dateTime += nowDateTime.TimeOfDay.ToString();
            return dateTime;
        }

        private static string GetOriginMessage(Level logLevel)
        {
            string origin;
            if (logLevel == Level.DEBUG)
                origin = GetOrigin(true, true, true);
            else
                origin = GetOrigin(Options.DefaultShowClass, Options.DefaultShowNameSpace, Options.DefaultShowMethod);
            return origin;
        }

        private static string GetDataTimeMessage()
        {
            string dateTime = GetDataTime();
            if (!string.IsNullOrEmpty(dateTime))
                dateTime += " ";
            return dateTime;
        }

        private static string GetThreadIdMessage()
        {
            string threadId = GetThreadId();
            if (!string.IsNullOrEmpty(threadId))
                threadId = $":{threadId}";
            return threadId;
        }

        private static string GetExceptionMessage(Exception? ex, bool? filterException)
        {
            if (ex is null)
                return string.Empty;
            string exMessage;
            if (filterException is true)
                exMessage = FilterException(ex);
            else if (filterException is false)
                exMessage = ex.ToString();
            else
            {
                if (Options.DefaultFilterException)
                    exMessage = FilterException(ex);
                else
                    exMessage = ex.ToString();
            }
            if (!string.IsNullOrEmpty(exMessage))
                exMessage = $"\n{exMessage}";
            return exMessage;
        }

        /// <summary>
        /// 过滤异常
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns></returns>
        public static string FilterException(Exception ex)
        {
            var list = ex.ToString().Split("\r\n").Where(s => !Options.ExceptionFilter.Any(f => s.Contains(f)));
            return Regex.Replace(string.Join("\r\n", list), @$"[\S]+(?=. {BaseNamespace})", "");
        }

        /// <summary>关闭</summary>
        private static void Close()
        {
            sw?.Close();
        }
    }
}
