using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Panuon.WPF.UI;
using StarsectorToolbox.Libs;
using StarsectorToolbox.ViewModels.Main;
using StarsectorToolbox.Views.CrashReporter;
using I18nRes = StarsectorToolbox.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorToolbox.Views.Main;

/// <summary>
/// Interaction logic for Main.xaml
/// </summary>
internal partial class MainWindow : WindowX
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
        // 获取系统主题色
        //Application.Current.Resources["MainFontSize"] = 24.0;

        //var windowGlassBrush = SystemParameters.WindowGlassBrush;
        //Application.Current.Resources[nameof(SystemParameters.WindowGlassBrush)] = windowGlassBrush;
        //WindowXCaption.SetBackground(this, windowGlassBrush);
        //// 根据主题色的明亮程度来设置字体颜色
        //var color = (Color)ColorConverter.ConvertFromString(windowGlassBrush.ToString());
        //if (IsLightColor(color))
        //    WindowXCaption.SetForeground(this, windowGlassBrush);
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
    }

    private void SystemParameters_StaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.WindowGlassColor))
        {
            Application.Current.Resources[nameof(SystemParameters.WindowGlassBrush)] =
                SystemParameters.WindowGlassBrush;
            Application.Current.Resources["ShadowColor"] = SystemParameters.WindowGlassColor;
        }
    }

    //关闭
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        ViewModel.Close();
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
