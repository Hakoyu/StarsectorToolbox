using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HKW.TomlParse;
using StarsectorTools.Libs;
using StarsectorTools.Pages;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>拓展目录</summary>
        private const string expansionDirectories = "Expansion";
        /// <summary>拓展信息文件</summary>
        private const string expansionInfoFile = "Expansion.toml";
        private const string strName = "Name";
        private const string strDescription = "Description";
        private bool menuOpen = false;
        private Dictionary<string, Lazy<Page>> menus = new();
        private Dictionary<string, Lazy<Page>> expansionMenus = new();
        private Dictionary<string, ExpansionInfo> allExceptionInfo = new();
        private Settings settingMenu = null!;
        private Info infoMenu = null!;
        private int menuSelectedIndex = -1;
        private int exceptionMenuSelectedIndex = -1;
        /// <summary>拓展信息</summary>
        class ExpansionInfo
        {
            /// <summary>ID</summary>
            public string Id { get; private set; } = null!;
            /// <summary>名称</summary>
            public string Name { get; private set; } = null!;
            /// <summary>作者</summary>
            public string Author { get; private set; } = null!;
            /// <summary>图标</summary>
            public string Icon { get; private set; } = null!;
            /// <summary>版本</summary>
            public string Version { get; private set; } = null!;
            /// <summary>支持的工具箱版本</summary>
            public string ToolsVersion { get; private set; } = null!;
            /// <summary>描述</summary>
            public string Description { get; private set; } = null!;
            /// <summary>拓展Id</summary>
            public string ExpansionId { get; private set; } = null!;
            /// <summary>拓展文件</summary>
            public string ExpansionFile { get; private set; } = null!;
            public ExpansionInfo(TomlTable table)
            {
                foreach (var info in table)
                    SetInfo(info.Key, info.Value.AsString);
            }
            public void SetInfo(string key, string value)
            {
                switch (key)
                {
                    case nameof(Id): Id = value; break;
                    case nameof(Name): Name = value; break;
                    case nameof(Author): Author = value; break;
                    case nameof(Icon): Icon = value; break;
                    case nameof(Version): Version = value; break;
                    case nameof(ToolsVersion): ToolsVersion = value; break;
                    case nameof(Description): Description = value; break;
                    case nameof(ExpansionId): ExpansionId = value; break;
                    case nameof(ExpansionFile): ExpansionFile = value; break;
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            //限制最大化区域,不然会盖住任务栏
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            // 全局错误捕获
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            InitializeDirectories();
            if (!SetConfig())
            {
                Close();
                return;
            }
            ChangeLanguage();
            //亚克力背景
            //WindowAccent.SetBlurBehind(this, Color.FromArgb(64, 0, 0, 0));
            ListBox_Menu.SelectedIndex = 0;
            Grid_TitleBar.Background = SystemParameters.WindowGlassBrush;
            var color = (Color)ColorConverter.ConvertFromString(Grid_TitleBar.Background.ToString());
            if (ST.IsLightColor(color))
                Label_Title.Foreground = (Brush)Application.Current.Resources["ColorBG"];

            //DirectoryInfo dirs = new(AppDomain.CurrentDomain.BaseDirectory);
            //foreach (FileInfo file in dirs.GetFiles())
            //{
            //}
            //Assembly assembly = Assembly.LoadFrom(@"C:\Users\HKW\Desktop\WpfLibrary1.dll");
            //Type type = assembly.GetType("WpfLibrary1.Page1")!;
            //MethodInfo mi = type.GetMethod("MehtodName")!;
            //object obj = assembly.CreateInstance(type.FullName!)!;
            //Frame_MainFrame.Content = obj;
            STLog.Instance.WriteLine(I18n.InitializationCompleted);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            SetBlurEffect();
            STLog.Instance.WriteLine(I18n.GlobalException, e.Exception);
            ST.ShowMessageBox(I18n.GlobalExceptionMessage, MessageBoxImage.Error);
            e.Handled = true;
            RemoveBlurEffect();
        }

        //窗体移动
        private void Grid_TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        //最小化
        private void Button_TitleMin_Click(object sender, RoutedEventArgs e)
        {
            //Visibility = Visibility.Hidden;
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
            ClearMenu();
            STLog.Instance.Close();
            Close();
        }

        private void Button_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            if (menuOpen)
            {
                Button_MainMenuIcon.Text = "📘";
                Grid_Menu.Width = 30;
                ScrollViewer.SetVerticalScrollBarVisibility(ListBox_Menu, ScrollBarVisibility.Hidden);
            }
            else
            {
                Button_MainMenuIcon.Text = "📖";
                Grid_Menu.Width = double.NaN;
                ScrollViewer.SetVerticalScrollBarVisibility(ListBox_Menu, ScrollBarVisibility.Auto);
            }
            menuOpen = !menuOpen;
        }

        private void ListBox_Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedIndex != -1 && listBox.SelectedItem is ListBoxItem item && item.Content is not Expander)
            {
                try
                {
                    if (listBox.Name == ListBox_Menu.Name)
                    {
                        Frame_MainFrame.Content = menus[item.Tag.ToString()!].Value;
                        menuSelectedIndex = ListBox_Menu.SelectedIndex;
                        ListBox_ExpansionMenu.SelectedIndex = -1;
                    }
                    else if (listBox.Name == ListBox_ExpansionMenu.Name)
                    {
                        Frame_MainFrame.Content = expansionMenus[item.Tag.ToString()!].Value;
                        exceptionMenuSelectedIndex = ListBox_ExpansionMenu.SelectedIndex;
                        ListBox_Menu.SelectedIndex = -1;
                    }
                }
                catch (Exception ex)
                {
                    STLog.Instance.WriteLine($"{I18n.InitializationError} {item.Content}", ex);
                    ST.ShowMessageBox($"{I18n.InitializationError}\n{item.Content}", MessageBoxImage.Error);
                    if (listBox.Name == ListBox_Menu.Name)
                        ListBox_Menu.SelectedIndex = menuSelectedIndex;
                    else if (listBox.Name == ListBox_ExpansionMenu.Name)
                        ListBox_ExpansionMenu.SelectedIndex = exceptionMenuSelectedIndex;
                }
            }
            //if (ListBox_Menu.SelectedIndex >= 0 && ListBox_Menu.SelectedItem is ListBoxItem item)
            //{
            //    try
            //    {
            //        Frame_MainFrame.Content = menus[item.Tag.ToString()!].Value;
            //        menuSelectedIndex = ListBox_Menu.SelectedIndex;
            //    }
            //    catch (Exception ex)
            //    {
            //        STLog.Instance.WriteLine($"{I18n.InitializationError} {item.Tag}", ex);
            //        ST.ShowMessageBox($"{I18n.InitializationError}\n{item.Tag}", MessageBoxImage.Error);
            //        ListBox_Menu.SelectedIndex = menuSelectedIndex;
            //    }
            //}
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            settingMenu ??= new();
            Frame_MainFrame.Content = settingMenu;
            ListBox_Menu.SelectedIndex = -1;
        }

        private void Grid_Menu_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Frame_MainFrame.Margin = new Thickness(Grid_Menu.ActualWidth, 0, 0, 0);
        }

        private void Frame_MainFrame_ContentRendered(object sender, EventArgs e)
        {
            STLog.Instance.WriteLine($"{I18n.ShowPage} {Frame_MainFrame.Content}");
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

        private void Button_Info_Click(object sender, RoutedEventArgs e)
        {
            infoMenu ??= new();
            Frame_MainFrame.Content = infoMenu;
            ListBox_Menu.SelectedIndex = -1;
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
            RefreshExpansionMenu();
        }
    }
}