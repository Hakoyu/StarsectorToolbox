using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StarsectorTools.Libs.GameInfo;
using HKW.Libs.TomlParse;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;
using StarsectorTools.Tools.ModManager;
using StarsectorTools.Tools.GameSettings;
using HKW.Libs.Log4Cs;
using System.IO;
using Panuon.WPF.UI;
using StarsectorTools.Pages;
using HKW.Models.DialogModels;

namespace StarsectorTools.Windows.MainWindow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>StarsectorTools配置文件资源链接</summary>
        private static readonly Uri resourcesConfigUri = new("\\Resources\\Config.toml", UriKind.Relative);
        internal static MainWindowViewModel ViewModel => (MainWindowViewModel)((MainWindow)Application.Current.MainWindow).DataContext;
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
            Logger.Initialize(nameof(StarsectorTools), ST.LogFile);
            // 全局异常捕获
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            // 获取系统主题色
            Application.Current.Resources["WindowGlassBrush"] = SystemParameters.WindowGlassBrush;
            // 根据主题色的明亮程度来设置字体颜色
            var color = (Color)ColorConverter.ConvertFromString(Grid_TitleBar.Background.ToString());
            if (Utils.IsLightColor(color))
                Label_Title.Foreground = (Brush)Application.Current.Resources["ColorBG"];
            // 初始化设置
            // 初始化页面
            // SetSettingsPage();
            // SetInfoPage();
            // ChangeLanguage();
            // ShowPage();
            // 注册消息窗口
            RegisterMessageBoxModel();
            // 注册打开文件对话框
            RegisterOpenFileDialogModel();
            // 注册保存文件对话框
            RegisterSaveFileDialogModel();

            try
            {
                using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
                DataContext = new MainWindowViewModel(sr.ReadToEnd());
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.InitializationError}: {nameof(MainWindowViewModel)}", ex, false);
                Close();
                return;
            }
            InitializePage();
            STLog.WriteLine(I18n.InitializationCompleted);
        }

        private void RegisterMessageBoxModel()
        {
            // 消息长度限制
            int messageLengthLimits = 8192;
            MessageBoxModel.InitializeHandler((d) =>
                {
                    string message = d.Message.Length < messageLengthLimits
                        ? d.Message
                        : d.Message[..messageLengthLimits] + $".........{I18n.ExcessivelyLongMessages}.........";
                    var button = ButtonConverter(d.Button);
                    var icon = IconConverter(d.Icon);
                    MessageBoxResult result;
                    if (d.Tag is false)
                    {
                        result = MessageBoxX.Show(message, d.Caption, button, icon);
                    }
                    else
                    {
                        SetBlurEffect();
                        result = MessageBoxX.Show(message, d.Caption, button, icon);
                        RemoveBlurEffect();
                    }
                    if (message.Length == messageLengthLimits)
                        GC.Collect();
                    return ResultConverter(result);
                });
            static MessageBoxButton ButtonConverter(MessageBoxModel.Button? button) =>
                button switch
                {
                    MessageBoxModel.Button.OK => MessageBoxButton.OK,
                    MessageBoxModel.Button.OKCancel => MessageBoxButton.OKCancel,
                    MessageBoxModel.Button.YesNo => MessageBoxButton.YesNo,
                    MessageBoxModel.Button.YesNoCancel => MessageBoxButton.YesNoCancel,
                    _ => MessageBoxButton.OK,
                };
            static MessageBoxIcon IconConverter(MessageBoxModel.Icon? icon) =>
                icon switch
                {
                    MessageBoxModel.Icon.None => MessageBoxIcon.None,
                    MessageBoxModel.Icon.Info => MessageBoxIcon.Info,
                    MessageBoxModel.Icon.Warning => MessageBoxIcon.Warning,
                    MessageBoxModel.Icon.Error => MessageBoxIcon.Error,
                    MessageBoxModel.Icon.Success => MessageBoxIcon.Success,
                    MessageBoxModel.Icon.Question => MessageBoxIcon.Question,
                    _ => MessageBoxIcon.Info,
                };
            static MessageBoxModel.Result ResultConverter(MessageBoxResult result) =>
                result switch
                {
                    MessageBoxResult.None => MessageBoxModel.Result.None,
                    MessageBoxResult.OK => MessageBoxModel.Result.OK,
                    MessageBoxResult.Cancel => MessageBoxModel.Result.Cancel,
                    MessageBoxResult.Yes => MessageBoxModel.Result.Yes,
                    MessageBoxResult.No => MessageBoxModel.Result.No,
                    _ => MessageBoxModel.Result.None,
                };
        }

        private void RegisterOpenFileDialogModel()
        {
            OpenFileDialogModel.InitializeHandler((d) =>
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog()
                {
                    Title = d.Title,
                    Filter = d.Filter,
                    Multiselect = d.Multiselect,
                };
                openFileDialog.ShowDialog();
                return openFileDialog.FileNames;
            });
        }
        private void RegisterSaveFileDialogModel()
        {
            SaveFileDialogModel.InitializeHandler((d) =>
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = d.Title,
                    Filter = d.Filter,
                };
                saveFileDialog.ShowDialog();
                return saveFileDialog.FileName;
            });
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception.Source == nameof(StarsectorTools))
            {
                STLog.WriteLine(I18n.GlobalException, e.Exception, false);
                MessageBoxModel.Show(new($"{I18n.GlobalExceptionMessage}\n\n{STLog.SimplifyException(e.Exception)}")
                {
                    Icon = MessageBoxModel.Icon.Error,
                });
            }
            else
            {
                STLog.WriteLine($"{I18n.GlobalExpansionException}: {e.Exception.Source}", e.Exception, false);
                MessageBoxModel.Show(new($"{string.Format(I18n.GlobalExpansionExceptionMessage, e.Exception.Source)}\n\n{STLog.SimplifyException(e.Exception)}")
                {
                    Icon = MessageBoxModel.Icon.Error,
                });
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
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
                Button_TitleMax.Content = "🔳";
            }
            else
            {
                WindowState = WindowState.Normal;
                Button_TitleMax.Content = "🔲";
            }
        }

        //关闭
        private void Button_TitleClose_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Close();
            Close();
        }

        private void ListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 禁止右键项时会选中项
            e.Handled = true;
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

        private void RefreshExpansionMenu_Click(object sender, RoutedEventArgs e)
        {
            RefreshExpansionPages();
        }

        private void InitializePage()
        {
            // 添加页面
            ViewModel.InfoPage = new InfoPage();
            ViewModel.SettingsPage = new SettingsPage();
            // 主界面必须在View中生成,拓展及调试拓展可以在ViewModel中使用反射
            InitializeMainPage();
            //InitializeExpansionPage();
            //InitializeExpansionDebugPage();
        }

        private void InitializeMainPage()
        {
            //添加主要页面
            ViewModel.AddMainPageItem(new()
            {
                Icon = "🌐",
                Tag = CreatePage(typeof(ModManagerPage)),
            });
            ViewModel.AddMainPageItem(new()
            {
                Icon = "⚙",
                Tag = CreatePage(typeof(GameSettingsPage))
            });
        }
        private void InitializeExpansionPage()
        {
            // 添加拓展页面
        }
        private void InitializeExpansionDebugPage()
        {
            // 添加拓展调试页面
        }

        private Page? CreatePage(Type type)
        {
            try
            {
                return (Page)type.Assembly.CreateInstance(type.FullName!)!;
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.PageInitializeError}: {type.FullName}", ex);
                Utils.ShowMessageBox($"{I18n.PageInitializeError}:\n{type.FullName}", STMessageBoxIcon.Error);
                return null;
            }
        }
    }
}