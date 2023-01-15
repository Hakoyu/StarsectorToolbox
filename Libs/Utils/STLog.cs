using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using StarsectorTools.Windows;

namespace StarsectorTools.Libs.Utils
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
        public const string LogFile = $"{ST.CoreDirectory}\\StarsectorTools.log";

        /// <summary>日志等级</summary>
        public static STLogLevel LogLevel { get; private set; } = STLogLevel.INFO;

        /// <summary>写入流</summary>
        private static StreamWriter sw = new(LogFile);

        /// <summary>读写锁</summary>
        internal static ReaderWriterLockSlim rwLockS = new();

        /// <summary>堆栈过滤</summary>
        private static List<string> shieldOutput = new()
        {
            "at System.",
            "at MS.",
            "at Microsoft.",
            "End of inner exception stack trace",
        };

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
            return $"{method?.DeclaringType?.FullName}.{method?.Name}";
        }

        /// <summary>
        /// 获取所在类名
        /// </summary>
        /// <returns></returns>
        private static string? GetClassName()
        {
            var method = new StackTrace().GetFrames().First(f => f.GetMethod()?.DeclaringType?.Name != nameof(STLog)).GetMethod();
            if (method?.DeclaringType?.Namespace?.Contains(nameof(StarsectorTools)) is true)
                return $"{nameof(StarsectorTools)}.{method?.DeclaringType?.Name}";
            else
                return method?.DeclaringType?.FullName;
        }

        internal static void SetLogLevel(STLogLevel logLevel)
        {
            LogLevel = logLevel;
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
        /// <param name="simplifyException">简化异常信息</param>
        /// <param name="args">插入的对象</param>
        public static void WriteLine(string message, Exception ex, bool simplifyException = true, params object[] args)
        {
            rwLockS.EnterWriteLock();
            try
            {
                sw.WriteLine($"[{GetClassName()}] {STLogLevel.ERROR} {KeyParse(message, args)}");
                if (simplifyException)
                    sw.WriteLine(SimplifyException(ex));
                else
                    sw.WriteLine(ex);
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
        public static string SimplifyException(Exception ex)
        {
            var list = ex.ToString().Split("\r\n").Where(s => !shieldOutput.Any(s1 => s.Contains(s1)));
            return Regex.Replace(string.Join("\r\n", list), @$"[\S]+(?={nameof(StarsectorTools)})", "");
        }

        /// <summary>关闭</summary>
        internal static void Close()
        {
            sw?.Close();
        }
    }
}