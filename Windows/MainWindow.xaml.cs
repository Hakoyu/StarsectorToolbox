using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HKW.WindowAccent;
using StarsectorTools.Libs;
using StarsectorTools.Pages;
using StarsectorTools.Tools.GameSettings;
using StarsectorTools.Tools.ModManager;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool menuOpen = false;
        Dictionary<string, Page> menuList = new();
        Settings settingMenu = null!;
        public MainWindow()
        {
            InitializeComponent();
            //限制最大化区域,不然会盖住任务栏
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            //STLog.Instance.LogLevel = STLogLevel.DEBUG;
            STLog.Instance.LogLevel = STLogLevel.INFO;
            STLog.Instance.WriteLine(I18n.InitializationCompleted);
            if (!SetConfig())
            {
                Close();
                return;
            }
            ChangeLanguage();
            //亚克力背景
            //WindowAccent.SetBlurBehind(this, Color.FromArgb(64, 0, 0, 0));
            ListBox_Menu.SelectedIndex = 0;
            //DirectoryInfo dirs = new(AppDomain.CurrentDomain.BaseDirectory);
            //foreach (FileInfo file in dirs.GetFiles())
            //{
            //}
            //Assembly assembly = Assembly.LoadFrom(@"C:\Users\HKW\Desktop\VS\WpfLibrary1\bin\Debug\net6.0-windows\WpfLibrary1.dll");
            //Type type = assembly.GetType("WpfLibrary1.Page1");
            //MethodInfo mi = type.GetMethod("MehtodName")!;
            //object obj = assembly.CreateInstance(type.FullName)!;
            //Frame_MainFrame.Content = obj;
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
            STLog.Instance.Close();
            Close();
            Process.GetCurrentProcess().Kill();
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
            if (ListBox_Menu.SelectedIndex >= 0)
            {
                Frame_MainFrame.Content = menuList[((ListBoxItem)ListBox_Menu.SelectedItem).Tag.ToString()!];
            }
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            settingMenu ??= new();
            Frame_MainFrame.Content = settingMenu;
            ListBox_Menu.SelectedIndex = -1;
        }
        private void Grid_Menu_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Frame_MainFrame.Margin = new Thickness(Grid_Menu.ActualWidth,0,0,0);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Keyboard.ClearFocus();
            // 将事件焦点转移到this
            DependencyObject scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, (FrameworkElement)Parent);
            ((ModManager)menuList[nameof(ModManager)]).CloseModInfo();
        }

        private void Frame_MainFrame_ContentRendered(object sender, EventArgs e)
        {
            STLog.Instance.WriteLine($"{I18n.ShowPage} {Frame_MainFrame.Content}");
        }
    }
}
