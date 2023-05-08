using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StarsectorToolbox.Models.ST;
using StarsectorToolbox.ViewModels.ModManager;

namespace StarsectorToolbox.Views.ModManager;

/// <summary>
/// ModManager.xaml 的交互逻辑
/// </summary>
internal partial class ModManagerPage : Page, ISTPage
{
    public bool NeedSave => ViewModel.IsRemindSave;

    internal ModManagerPageViewModel ViewModel => (ModManagerPageViewModel)DataContext;

    /// <summary>
    ///
    /// </summary>
    public ModManagerPage()
    {
        DataContext = new ModManagerPageViewModel(true);
        ViewModel.AddUserGroupWindow = new AddUserGroupWindowViewModel(new AddUserGroupWindow());
        InitializeComponent();
    }

    private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !Regex.IsMatch(e.Text, "[0-9]");

    private async void DataGrid_ModsShowList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is Array fileArray)
        {
            // TODO:需要在ViewModel内部实现
            await ViewModel.DropFiles(fileArray);
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
