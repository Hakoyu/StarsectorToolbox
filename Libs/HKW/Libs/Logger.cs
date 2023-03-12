using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace HKW.Libs.Log4Cs
{
    /// <summary>日志等级</summary>
    public enum LogLevel
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

    /// <summary>
    /// 记录器
    /// </summary>
    public class Logger
    {
        /// <summary>日志文件</summary>
        public static string LogFile { get; private set; } = string.Empty;

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
            public LogLevel DefaultLevel { get; set; } = LogLevel.INFO;

            /// <summary>
            /// 默认附加
            /// </summary>
            public bool DefaultAppend { get; set; } = false;

            /// <summary>
            /// 默认过滤异常
            /// </summary>
            public bool DefaultFilterException { get; set; } = true;

            /// <summary>
            /// 默认显示命名空间
            /// </summary>
            public bool DefaultShowNamespace { get; set; } = true;

            /// <summary>
            /// 默认只显示基命名空间
            /// </summary>
            public bool DefaultOnlyBaseNamespace { get; set; } = true;

            /// <summary>
            /// 默认显示类名
            /// </summary>
            public bool DefaultShowClass { get; set; } = true;

            /// <summary>
            /// 默认显示方法名
            /// </summary>
            public bool DefaultShowMethod { get; set; } = false;

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
            public List<string> ExceptionFilter { get; set; } =
                new()
                {
                    "at System.",
                    "at MS.",
                    "at Microsoft.",
                };
        }

        private Logger()
        { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="logFile">日志文件</param>
        /// <param name="initializeOptions">初始化设置</param>
        public static void Initialize(
            string logFile,
            InitializeOptions? initializeOptions = null
        )
        {
            Options = initializeOptions ?? new();
            LogFile = logFile;
            sw?.Close();
            sw = new(LogFile, Options.DefaultAppend);
        }

        /// <summary>
        /// 字符串转换成日志等级
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>日志等级</returns>
        public static LogLevel LogLevelConverter(string str) =>
            str.ToUpper() switch
            {
                nameof(LogLevel.DEBUG) => LogLevel.DEBUG,
                nameof(LogLevel.INFO) => LogLevel.INFO,
                nameof(LogLevel.WARN) => LogLevel.WARN,
                nameof(LogLevel.ERROR) => LogLevel.ERROR,
                _ => LogLevel.INFO
            };

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">消息</param>
        public static void Debug(string message) =>
            RecordBase(message, LogLevel.DEBUG, null, Options.DefaultFilterException);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">消息</param>
        public static void Info(string message) =>
            RecordBase(message, LogLevel.INFO, null, Options.DefaultFilterException);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">消息</param>
        public static void Warring(string message) =>
            RecordBase(message, LogLevel.WARN, null, Options.DefaultFilterException);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">消息</param>
        public static void Error(string message) =>
            RecordBase(message, LogLevel.ERROR, null, Options.DefaultFilterException);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ex">异常</param>
        public static void Error(string message, Exception ex) =>
            RecordBase(message, LogLevel.ERROR, ex, Options.DefaultFilterException);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="ex">异常</param>
        /// <param name="filterException">过滤器</param>
        public static void Error(string message, Exception ex, bool filterException) =>
            RecordBase(message, LogLevel.ERROR, ex, filterException);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        /// <param name="ex">异常</param>
        /// <param name="filterException">过滤器</param>
        private static void RecordBase(
            string message,
            LogLevel logLevel,
            Exception? ex,
            bool filterException
        )
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
        /// 获取线程Id
        /// </summary>
        /// <returns>线程Id</returns>
        private static string GetThreadId()
        {
            return Options.DefaultShowThreadId
                ? Thread.CurrentThread.ManagedThreadId.ToString()
                : string.Empty;
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

        private static string GetOriginMessage(LogLevel logLevel)
        {
            string origin;
            if (logLevel is LogLevel.DEBUG || logLevel is LogLevel.ERROR)
                origin = GetOrigin(true, false, true, true);
            else
                origin = GetOrigin(
                    Options.DefaultShowNamespace,
                    Options.DefaultOnlyBaseNamespace,
                    Options.DefaultShowClass,
                    Options.DefaultShowMethod
                );
            return origin;
        }

        /// <summary>
        /// 获取源信息
        /// </summary>
        /// <param name="getNamespace">获取命名空间</param>
        /// <param name="onlyBaseNamespace">只要基命名空间</param>
        /// <param name="getClass">获取类</param>
        /// <param name="getMethod">获取方法</param>
        /// <returns>源信息</returns>
        private static string GetOrigin(
            bool getNamespace,
            bool onlyBaseNamespace,
            bool getClass,
            bool getMethod
        )
        {
            var method = new StackTrace()
                .GetFrames()
                .First(f => f.GetMethod()?.DeclaringType?.Name != nameof(Logger))
                ?.GetMethod();
            var strs = new List<string>();
            if (getNamespace && method?.DeclaringType?.Namespace is string strNamespace)
            {
                if (
                    onlyBaseNamespace
                    && strNamespace.Split(".")?.FirstOrDefault(defaultValue: null)
                        is string baseNamespace
                )
                    strNamespace = baseNamespace;
                strs.Add(strNamespace);
            }
            if (getClass && method?.DeclaringType?.Name is string strClass)
                strs.Add(strClass);
            if (getMethod && method?.Name is string strMethod)
                strs.Add(strMethod);
            return string.Join(".", strs);
        }

        private static string GetDataTimeMessage()
        {
            string dateTime = GetDataTime();
            if (!string.IsNullOrWhiteSpace(dateTime))
                dateTime += " ";
            return dateTime;
        }

        private static string GetThreadIdMessage()
        {
            string threadId = GetThreadId();
            if (!string.IsNullOrWhiteSpace(threadId))
                threadId = $":{threadId}";
            return threadId;
        }

        private static string GetExceptionMessage(Exception? ex, bool filterException)
        {
            if (ex is null)
                return string.Empty;
            string exMessage;
            if (filterException is true)
                exMessage = FilterException(ex);
            else
                exMessage = FilterPath(ex.ToString(), GetOrigin(true, true, false, false));
            return "\n" + exMessage;
        }

        /// <summary>
        /// 过滤异常
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns></returns>
        public static string FilterException(Exception ex)
        {
            var list = ex.ToString()
                .Split("\r\n")
                .Where(s => !Options.ExceptionFilter.Any(f => s.Contains(f)));
            return FilterPath(string.Join("\r\n", list), GetOrigin(true, true, false, false));
        }

        /// <summary>
        /// 过滤路径信息
        /// </summary>
        /// <param name="exMessage">错误信息</param>
        /// <param name="baseNamespace">基命名空间</param>
        /// <returns>过滤后的数据</returns>
        public static string FilterPath(string exMessage, string baseNamespace) =>
            Regex.Replace(exMessage, @$"[\S]:[\S]+(?={baseNamespace})", "");

        /// <summary>关闭</summary>
        private static void Close()
        {
            sw?.Close();
        }
    }
}