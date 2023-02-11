using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HKW.Libs.Log4Cs;
using HKW.ViewModels.Dialog;
using Panuon.WPF.UI;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows.MainWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        ///
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            // 限制最大化区域,不然会盖住任务栏(已在XAML中实现)
            // MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            // MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            // 亚克力背景
            // WindowAccent.SetBlurBehind(this, Color.FromArgb(64, 0, 0, 0));
            // 注册日志
            Logger.Initialize(nameof(StarsectorTools), ST.LogFile);

            // 全局异常捕获
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            // 获取系统主题色
            Application.Current.Resources["WindowGlassBrush"] = SystemParameters.WindowGlassBrush;
            // 根据主题色的明亮程度来设置字体颜色
            var color = (Color)
                ColorConverter.ConvertFromString(Grid_TitleBar.Background.ToString());
            if (Utils.IsLightColor(color))
                Label_Title.Foreground = (Brush)Application.Current.Resources["ColorBG"];

            // 注册数据
            RegisterData();

            // 初始化ViewModel
            try
            {
                using StreamReader sr =
                    new(Application.GetResourceStream(resourcesConfigUri).Stream);
                DataContext = new MainWindowViewModel(sr.ReadToEnd());
            }
            catch (Exception ex)
            {
                Logger.Record(
                    $"{I18n.InitializationError}: {nameof(MainWindowViewModel)}",
                    ex,
                    false
                );
                Environment.Exit(-1);
                return;
            }
            // 初始化页面
            InitializePage();
            Logger.Record(I18n.InitializationCompleted);
        }

        private void OnDispatcherUnhandledException(
            object sender,
            DispatcherUnhandledExceptionEventArgs e
        )
        {
            if (e.Exception.Source is nameof(StarsectorTools))
            {
                STLog.WriteLine(I18n.GlobalException, e.Exception, false);
                MessageBoxVM.Show(
                    new($"{I18n.GlobalExceptionMessage}\n\n{STLog.SimplifyException(e.Exception)}")
                    {
                        Icon = MessageBoxVM.Icon.Error,
                    }
                );
            }
            else
            {
                STLog.WriteLine(
                    $"{I18n.GlobalExpansionException}: {e.Exception.Source}",
                    e.Exception,
                    false
                );
                MessageBoxVM.Show(
                    new(
                        $"{string.Format(I18n.GlobalExpansionExceptionMessage, e.Exception.Source)}\n\n{STLog.SimplifyException(e.Exception)}"
                    )
                    {
                        Icon = MessageBoxVM.Icon.Error,
                    }
                );
            }
            e.Handled = true;
        }

        //窗体移动
        private void Grid_TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        //最小化
        private void Button_TitleMin_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        //最大化
        private void Button_TitleMax_Click(object sender, RoutedEventArgs e)
        {
            //检测当前窗口状态
            if (WindowState is WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        //关闭
        private void Button_TitleClose_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Close();
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            DependencyObject scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, (FrameworkElement)Parent);
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = MouseWheelEvent,
                Source = sender,
            };
            if (sender is Control control && control.Parent is UIElement ui)
                ui.RaiseEvent(eventArg);
            e.Handled = true;
        }

        private void Frame_MainFrame_ContentRendered(object sender, EventArgs e)
        {
            if (sender is not Frame frame)
                return;
            while (frame.CanGoBack)
                frame.RemoveBackEntry();
            GC.Collect();
        }
    }
}