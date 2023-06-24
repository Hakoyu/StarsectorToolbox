using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StarsectorToolbox.Libs;
using StarsectorToolbox.ViewModels.Main;
using StarsectorToolbox.Views.CrashReporter;
using I18nRes = StarsectorToolbox.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorToolbox.Views.Main;

/// <summary>
/// Interaction logic for Main.xaml
/// </summary>
internal partial class MainWindow : Window
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();
    internal MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

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

        // 获取系统主题色
        Application.Current.Resources[nameof(SystemParameters.WindowGlassBrush)] =
            SystemParameters.WindowGlassBrush;
        Application.Current.Resources["MainFontSize"] = 24.0;
        // 根据主题色的明亮程度来设置字体颜色
        var color = (Color)ColorConverter.ConvertFromString(Grid_TitleBar.Background.ToString());
        if (IsLightColor(color))
            Label_Title.Foreground = (Brush)Application.Current.Resources["ColorBG"];
        // 注册数据
        RegisterData();
        // 初始化ViewModel
        try
        {
            DataContext = new MainWindowViewModel(true);
        }
        catch (Exception ex)
        {
            sr_logger.Error(ex, $"{I18nRes.InitializationError}: {nameof(MainWindowViewModel)}");
            //MessageBoxVM.Show(
            //    new($"{I18nRes.InitializationError}: {nameof(MainWindowViewModel)}")
            //    {
            //        Icon = MessageBoxVM.Icon.Error
            //    }
            //);
            Environment.Exit(-1);
            return;
        }
        // 注册主窗口模糊效果触发器
        ViewModel.RegisterChangeWindowEffectEvent(SetBlurEffect, RemoveBlurEffect);
        // 初始化页面
        InitializePage();
        // 初始化游戏异常收集器
        ViewModel.CrashReporterWindow = new(new CrashReporterWindow());
        sr_logger.Info(I18nRes.InitializationCompleted);
        GC.Collect();
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
        if (WindowState == WindowState.Normal)
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
        // 清理过时页面
        while (frame.CanGoBack)
            frame.RemoveBackEntry();
        GC.Collect();
    }
}
