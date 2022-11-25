using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace HKW.WindowAccent
{
    public class WindowAccent
    {
        /// <summary>
        /// 强调状态
        /// </summary>
        public enum AccentState
        {
            /// <summary>
            /// 禁用
            /// </summary>
            DISABLED,
            /// <summary>
            /// 渐变
            /// </summary>
            GRADIENT,
            /// <summary>
            /// 透明渐变
            /// </summary>
            TRANSPARENTGRADIENT,
            /// <summary>
            /// 高斯模糊
            /// </summary>
            BLURBEHIND,
            /// <summary>
            /// 亚克力
            /// </summary>
            ACRYLICBLURBEHIND,
            /// <summary>
            /// 无效状态
            /// </summary>
            INVALID_STATE
        }
        /// <summary>
        /// 强调状态
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            /// <summary>
            /// 强调状态 
            /// </summary>
            public AccentState AccentState;
            /// <summary>
            /// 强调标志 (必须为2)
            /// </summary>
            public uint AccentFlags;
            /// <summary>
            /// 混合色
            /// </summary>
            public uint GradientColor;
            /// <summary>
            /// 动画ID
            /// </summary>
            public uint AnimationId;
        }
        /// <summary>
        /// 窗口组合属性数据
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        /// <summary>
        /// 窗口组合属性
        /// </summary>
        internal enum WindowCompositionAttribute
        {
            // ...
            /// <summary>
            /// 透明度策略
            /// </summary>
            WCA_ACCENT_POLICY = 19,
            // ...
        }
        /// <summary>
        /// 设置高斯模糊
        /// </summary>
        /// <param name="window">主窗口(MainWindow)</param>
        /// <param name="argb">背景颜色(ARGB)</param>
        public static void SetBlurBehind(Window window, uint argb)
        {
            SetWindowAccent(window, AccentState.BLURBEHIND, argb);
        }
        /// <summary>
        /// 设置高斯模糊
        /// </summary>
        /// <param name="window">主窗口(MainWindow)</param>
        /// <param name="color">背景颜色</param>
        public static void SetBlurBehind(Window window, Color color)
        {
            SetWindowAccent(window, AccentState.BLURBEHIND, color);
        }
        /// <summary>
        /// 设置亚克力模糊
        /// </summary>
        /// <param name="window">主窗口(MainWindow)</param>
        /// <param name="argb">背景颜色(ARGB)</param>
        public static void SetAcrylicBlurBehind(Window window, uint argb)
        {
            SetWindowAccent(window, AccentState.ACRYLICBLURBEHIND, argb);
        }
        /// <summary>
        /// 设置亚克力模糊
        /// </summary>
        /// <param name="window">主窗口(MainWindow)</param>
        /// <param name="color">背景颜色</param>
        public static void SetAcrylicBlurBehind(Window window, Color color)
        {
            SetWindowAccent(window, AccentState.ACRYLICBLURBEHIND, color);
        }
        /// <summary>
        /// 设置窗口强调类型
        /// </summary>
        /// <param name="window">主窗口(MainWindow)</param>
        /// <param name="state">强调状态</param>
        /// <param name="argb">背景颜色(ARGB)</param>
        public static void SetWindowAccent(Window window, AccentState state, uint argb)
        {
            window.SourceInitialized += (o, e) => EnableAccent(window, state, argb);
        }
        /// <summary>
        /// 设置窗口强调类型
        /// </summary>
        /// <param name="window">主窗口(MainWindow)</param>
        /// <param name="state">强调状态</param>
        /// <param name="color">背景颜色</param>
        public static void SetWindowAccent(Window window, AccentState state, Color color)
        {
            window.SourceInitialized += (o, e) => EnableAccent(window, state, Color2Argb(color));
        }

        static void EnableAccent(Window window, AccentState state, uint argb)
        {
            // 窗口操作助手
            var windowHelper = new WindowInteropHelper(window);
            // 设置强调状态
            var accent = new AccentPolicy
            {
                // 设置状态
                AccentState = state,
                // 设置强调标志
                AccentFlags = 2,
                //设置混合色
                GradientColor = argb
            };
            // 获取结构大小
            var accentStructSize = Marshal.SizeOf(accent);
            // 获取结构指针
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            // 结构转入
            Marshal.StructureToPtr(accent, accentPtr, false);
            // 设置窗口组合属性数据
            var data = new WindowCompositionAttributeData
            {
                // 强调状态
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                // 结构大小
                SizeOfData = accentStructSize,
                // 结构指针
                Data = accentPtr
            };
            // 设置窗口组合属性
            _ = SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            // 释放结构
            Marshal.FreeHGlobal(accentPtr);
        }
        /// <summary>
        /// 设置窗口组合属性
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        internal static Color Argb2Color(uint argb)
        {
            return Color.FromArgb((byte)((argb & 0xFF000000) >> 24), (byte)(argb & 0xFF), (byte)((argb & 0xFF00) >> 8), (byte)((argb & 0xFF0000) >> 16));
        }
        internal static uint Color2Argb(Color color)
        {
            return (uint)color.A << 24 | (uint)color.B << 16 | (ushort)(color.G << 8 | color.R);
        }
    }
}
