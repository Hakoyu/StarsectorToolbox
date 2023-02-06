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
        private static string LogFile = null!;

        /// <summary>基命名空间</summary>
        private static string BaseNamespace = null!;

        /// <summary>写入流</summary>
        private static StreamWriter sw = null!;

        /// <summary>读写锁</summary>
        private static ReaderWriterLockSlim rwLockS = new();

        /// <summary>设置</summary>
        private static InitializeOptions options = null!;

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
            /// 默认启动异常过滤器
            /// </summary>
            public bool DefaultEnableExceptionFilter { get; set; } = false;
            /// <summary>
            /// 默认显示命名空间
            /// </summary>
            public bool DefaultShowNameSpace { get; set; } = false;

            /// <summary>
            /// 默认显示时间
            /// </summary>
            public bool DefaultShowTime { get; set; } = true;

            /// <summary>
            /// 默认显示日期
            /// </summary>
            public bool DefaultShowDate { get; set; } = true;
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
        /// <param name="baseNamespce">基础命名空间</param>
        /// <param name="logFile">日志文件</param>
        /// <param name="initializeOptions">初始化设置</param>
        public static void Initialize(string baseNamespce, string logFile, InitializeOptions? initializeOptions = null)
        {
            options = initializeOptions ?? new();
            LogFile = logFile;
            BaseNamespace = baseNamespce;
            sw = new(LogFile, options.DefaultAppend);
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
            return Thread.CurrentThread.ManagedThreadId.ToString();
        }
        /// <summary>
        /// 根据默认值获取日期时间
        /// </summary>
        /// <returns>日期时间</returns>
        private static string? GetDataTime()
        {
            string dateTime = string.Empty;
            var nowDateTime = DateTime.Now;
            if (options.DefaultShowDate)
                dateTime += $"{nowDateTime.Year}/{nowDateTime.Month}/{nowDateTime.Day} ";
            if (options.DefaultShowTime)
                dateTime += nowDateTime.TimeOfDay.ToString();
            if (!string.IsNullOrEmpty(dateTime))
                dateTime += " ";
            return dateTime;
        }
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message"></param>
        public static void Record(string message)
        {
            Record(message, options.DefaultLevel);
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="logLevel">日志等级</param>
        public static void Record(string message, Level logLevel)
        {
            rwLockS.EnterWriteLock();
            try
            {
                if (logLevel >= options.DefaultLevel)
                {
                    string? origin;
                    if (logLevel == Level.DEBUG)
                        origin = GetOrigin(true, true, true);
                    else
                        origin = GetOrigin(true, false, false);
                    string? dateTime = GetDataTime();
                    sw.WriteLine($"{dateTime}[{origin}:{GetThreadId()}] {logLevel} {message}");
                    sw.Flush();
                }
            }
            finally
            {
                rwLockS.ExitWriteLock();
            }
        }

        /// <summary>
        /// Exception解析 用来精简异常的堆栈输出
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns></returns>
        public static string SimplifyException(Exception ex)
        {
            var list = ex.ToString().Split("\r\n").Where(s => !options.ExceptionFilter.Any(f => s.Contains(f)));
            return Regex.Replace(string.Join("\r\n", list), @$"[\S]+(?=. {BaseNamespace})", "");
        }

        /// <summary>关闭</summary>
        private static void Close()
        {
            sw?.Close();
        }
    }
}
