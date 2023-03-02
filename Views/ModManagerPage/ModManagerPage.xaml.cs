using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HKW.Libs.Log4Cs;
using StarsectorTools.Libs.Utils;
using StarsectorTools.ViewModels.ModManagerPage;
using I18nRes = StarsectorTools.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorTools.Views.ModManagerPage
{
    /// <summary>
    /// ModManagerPage.xaml 的交互逻辑
    /// </summary>
    public partial class ModManagerPage : Page, ISTPage
    {
        public bool NeedSave => ViewModel.IsRemindSave;

        internal ModManagerPageViewModel ViewModel => (ModManagerPageViewModel)DataContext;

        /// <summary>
        ///
        /// </summary>
        public ModManagerPage()
        {
            InitializeComponent();
            DataContext = new ModManagerPageViewModel(true);

        }

        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");

        private void Button_AddUserGroup_Click(object sender, RoutedEventArgs e)
        {
            AddUserGroupWindow window = new();
            window.Button_Yes.Click += (s, e) =>
            {
                if (ViewModel.TryAddUserGroup(window.TextBox_Icon.Text, window.TextBox_Name.Text))
                    window.Close();
            };
            window.Button_Cancel.Click += (s, e) => window.Close();
            window.ShowDialog();
        }

        private async void DataGrid_ModsShowList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is Array fileArray)
            {
                Logger.Info($"{I18nRes.ConfirmDragFiles} {I18nRes.Size}: {fileArray.Length}");
                Utils.SetMainWindowBlurEffect(false);
                foreach (string file in fileArray)
                    await ViewModel.DropFile(file);
                Utils.RemoveMainWindowBlurEffect();
                // TODO:需要在ViewModel内部实现
                //ViewModel.DataGridDropFile(fileArray);
                //new Task(() =>
                //{
                //    int total = fileArray.Length;
                //    int completed = 0;
                //    ModArchiveingWindow window = null!;
                //    Dispatcher.InvokeAsync(() =>
                //    {
                //        window = new();
                //        window.Label_Total.Content = total;
                //        window.Label_Completed.Content = completed;
                //        window.Label_Incomplete.Content = total;
                //    });
                //    foreach (string file in fileArray)
                //    {
                //        if (Utils.FileExists(file))
                //        {
                //            Dispatcher.InvokeAsync(() =>
                //            {
                //                ViewModel.DropFile(file);
                //                window.Label_Progress.Content = file;
                //                window.ShowDialog();
                //                window.Label_Completed.Content = ++completed;
                //                window.Label_Incomplete.Content = total - completed;
                //            });
                //        }
                //    }
                //    Dispatcher.InvokeAsync(() => window.Close());
                //    GC.Collect();
                //}).Start();
            }
        }

        private void ListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 禁止右键项时会选中项
            e.Handled = true;
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
    }
}