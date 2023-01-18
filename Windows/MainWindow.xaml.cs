﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using HKW.TomlParse;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Windows.MainWindow.MainWindow_I18n;

namespace StarsectorTools.Windows
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
            // 限制最大化区域,不然会盖住任务栏
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            // 亚克力背景
            //WindowAccent.SetBlurBehind(this, Color.FromArgb(64, 0, 0, 0));
            // 全局异常捕获
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            // 初始化设置
            if (!SetConfig())
            {
                Close();
                return;
            }
            // 初始化页面
            InitializeDirectories();
            SetSettingsPage();
            SetInfoPage();
            ChangeLanguage();
            ShowPage();

            // 获取系统主题色
            Application.Current.Resources["WindowGlassBrush"] = SystemParameters.WindowGlassBrush;
            // 根据主题色的明亮程度来设置字体颜色
            var color = (Color)ColorConverter.ConvertFromString(Grid_TitleBar.Background.ToString());
            if (Utils.IsLightColor(color))
                Label_Title.Foreground = (Brush)Application.Current.Resources["ColorBG"];

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

        private void Button_ExpandMainMenu_Click(object sender, RoutedEventArgs e)
        {
            if (menuOpen)
            {
                Button_MainMenuIcon.Text = "📘";
                Grid_MainMenu.Width = 30;
                ScrollViewer.SetVerticalScrollBarVisibility(ListBox_MainMenu, ScrollBarVisibility.Hidden);
            }
            else
            {
                Button_MainMenuIcon.Text = "📖";
                Grid_MainMenu.Width = double.NaN;
                ScrollViewer.SetVerticalScrollBarVisibility(ListBox_MainMenu, ScrollBarVisibility.Auto);
            }
            menuOpen = !menuOpen;
        }

        private void ListBox_Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedIndex != -1 && listBox.SelectedItem is ListBoxItem item && item.Content is not Expander)
            {
                var id = item.Tag.ToString()!;
                if (listBox.Name == ListBox_MainMenu.Name)
                {
                    Frame_MainFrame.Content = pages[id];
                    pageSelectedIndex = ListBox_MainMenu.SelectedIndex;
                    ListBox_ExpansionMenu.SelectedIndex = -1;
                    ButtonPageCancelPress();
                }
                else if (listBox.Name == ListBox_ExpansionMenu.Name)
                {
                    var info = allExpansionsInfo[item.Tag.ToString()!];
                    try
                    {
                        Frame_MainFrame.Content = expansionPages[id].Value;
                        exceptionPageSelectedIndex = ListBox_ExpansionMenu.SelectedIndex;
                        ListBox_MainMenu.SelectedIndex = -1;
                        ButtonPageCancelPress();
                    }
                    catch (Exception ex)
                    {
                        STLog.WriteLine($"{I18n.PageInitializeError} {info.ExpansionType.FullName}", ex);
                        Utils.ShowMessageBox($"{I18n.PageInitializeError}\n{info.ExpansionType.FullName}", STMessageBoxIcon.Error);
                        ListBox_ExpansionMenu.SelectedIndex = exceptionPageSelectedIndex;
                    }
                }
                GC.Collect();
            }
        }

        private void Button_SettingsPage_Click(object sender, RoutedEventArgs e)
        {
            Frame_MainFrame.Content = settingsPage;
            ListBox_MainMenu.SelectedIndex = -1;
            ListBox_ExpansionMenu.SelectedIndex = -1;
            ButtonPageCancelPress();
            Button_SettingsPage.Tag = true;
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

        private void Button_InfoPage_Click(object sender, RoutedEventArgs e)
        {
            Frame_MainFrame.Content = infoPage;
            ListBox_MainMenu.SelectedIndex = -1;
            ListBox_ExpansionMenu.SelectedIndex = -1;
            ButtonPageCancelPress();
            Button_InfoPage.Tag = true;
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

        private void Button_StartGame_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.FileExists(GameInfo.ExeFile))
            {
                SaveAllPages();
                if (clearGameLogOnStart)
                    ClearGameLogFile();
                System.Diagnostics.Process process = new();
                process.StartInfo.FileName = "cmd";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardInput = true;
                if (process.Start())
                {
                    process.StandardInput.WriteLine($"cd /d {GameInfo.BaseDirectory}");
                    process.StandardInput.WriteLine($"starsector.exe");
                    process.Close();
                }
            }
        }

        private void CheckBox_ClearLogOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                clearGameLogOnStart = (bool)checkBox.IsChecked!;
                if (!Utils.FileExists(ST.STConfigTomlFile))
                    return;
                TomlTable toml = TOML.Parse(ST.STConfigTomlFile);
                toml["Game"]["ClearLogOnStart"] = clearGameLogOnStart;
                toml.SaveTo(ST.STConfigTomlFile);
            }
        }
    }
}