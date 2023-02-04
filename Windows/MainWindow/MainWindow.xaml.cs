using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StarsectorTools.Libs.GameInfo;
using HKW.TomlParse;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;
using StarsectorTools.Tools.ModManager;
using HKW.Model;
using System.IO;

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
            // 全局异常捕获
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            // 初始化设置
            if (!SetConfig())
            {
                Close();
                return;
            }
            // 初始化页面
            SetSettingsPage();
            SetInfoPage();
            ChangeLanguage();
            //ShowPage();

            // 获取系统主题色
            Application.Current.Resources["WindowGlassBrush"] = SystemParameters.WindowGlassBrush;
            // 根据主题色的明亮程度来设置字体颜色
            var color = (Color)ColorConverter.ConvertFromString(Grid_TitleBar.Background.ToString());
            if (Utils.IsLightColor(color))
                Label_Title.Foreground = (Brush)Application.Current.Resources["ColorBG"];
            using StreamReader sr = new(Application.GetResourceStream(resourcesConfigUri).Stream);
            DataContext = new MainWindowViewModel();
            // 注册消息窗口
            MessageBoxModel.SetHandler((m) =>
            {
                return MessageBoxModel.Result.None;
                //return (MessageBoxModel.Result)Utils.ShowMessageBox(m.Description, (MessageBoxButton)m.Button, (STMessageBoxIcon)m.Icon, m.Tag is not true);
            });
            // 注册打开文件对话框
            OpenFileDialogModel.SetHandler((d) =>
            {
                //新建文件选择
                var openFileDialog = new Microsoft.Win32.OpenFileDialog()
                {
                    Title = d.Title,
                    Filter = d.Filter,
                    Multiselect = d.Multiselect,
                };
                openFileDialog.ShowDialog();
                return openFileDialog.FileNames;
            });
            // 注册保存文件对话框
            SaveFileDialogModel.SetHandler((d) =>
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = d.Title,
                    Filter = d.Filter,
                };
                return saveFileDialog.FileName;
            });

            ViewModel.AddPage("😃", "name", "nameI18n", "tooltip", CreatePage(typeof(ModManager)));

            STLog.WriteLine(I18n.InitializationCompleted);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (e.Exception.Source == nameof(StarsectorTools))
            {
                STLog.WriteLine(I18n.GlobalException, e.Exception, false);
                Utils.ShowMessageBox($"{I18n.GlobalExceptionMessage}\n\n{STLog.SimplifyException(e.Exception)}", STMessageBoxIcon.Error);
            }
            else
            {
                STLog.WriteLine($"{I18n.GlobalExpansionException}: {e.Exception.Source}", e.Exception, false);
                Utils.ShowMessageBox($"{string.Format(I18n.GlobalExpansionExceptionMessage, e.Exception.Source)}\n\n{STLog.SimplifyException(e.Exception)}", STMessageBoxIcon.Error);
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
            ClearPages();
            STLog.Close();
            Close();
        }

        private void Frame_MainFrame_ContentRendered(object sender, EventArgs e)
        {
            STLog.WriteLine($"{I18n.ShowPage} {Frame_MainFrame.Content}");
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

        private void CheckBox_ClearLogOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                clearGameLogOnStart = (bool)checkBox.IsChecked!;
                if (!Utils.FileExists(ST.ConfigTomlFile))
                    return;
                TomlTable toml = TOML.Parse(ST.ConfigTomlFile);
                toml["Game"]["ClearLogOnStart"] = clearGameLogOnStart;
                toml.SaveTo(ST.ConfigTomlFile);
            }
        }
    }
}