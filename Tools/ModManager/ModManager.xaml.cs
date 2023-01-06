using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using HKW.TomlParse;
using StarsectorTools.Utils;
using I18n = StarsectorTools.Langs.Tools.ModManager.ModManager_I18n;

namespace StarsectorTools.Tools.ModManager
{
    /// <summary>
    /// ModManager.xaml 的交互逻辑
    /// </summary>
    public partial class ModManager : Page
    {
        public ModManager()
        {
            InitializeComponent();
            //throw new();
            LoadConfig();
            InitializeData();
        }

        private void Lable_CopyInfo_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(((Label)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)sender))).Content.ToString());
        }

        private void Button_CopyInfo_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(((Button)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)sender))).Content.ToString());
        }

        private void TextBlock_CopyInfo_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(((TextBlock)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)sender))).Text);
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveAllData();
            ResetRemindSaveThread();
        }

        private void Button_ImportEnabledList_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = I18n.ImportFromFile,
                Filter = $"Json {I18n.File}|*.json"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                GetEnabledMods(openFileDialog.FileName, true);
                RefreshCountOfListBoxItems();
            }
        }

        private void Button_ExportEnabledList_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = I18n.ExportToFile,
                Filter = $"Json {I18n.File}|*.json"
            };
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                SaveEnabledMods(saveFileDialog.FileName);
            }
        }

        private void Button_GroupMenu_Click(object sender, RoutedEventArgs e)
        {
            if (isGroupMenuOpen)
            {
                Button_GroupMenuIcon.Text = "📘";
                Grid_GroupMenu.Width = 30;
                ScrollViewer.SetVerticalScrollBarVisibility(ListBox_ModsGroupMenu, ScrollBarVisibility.Hidden);
            }
            else
            {
                Button_GroupMenuIcon.Text = "📖";
                Grid_GroupMenu.Width = double.NaN;
                ScrollViewer.SetVerticalScrollBarVisibility(ListBox_ModsGroupMenu, ScrollBarVisibility.Auto);
            }
            isGroupMenuOpen = !isGroupMenuOpen;
        }

        private void Grid_GroupMenu_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid_DataGrid.Margin = new Thickness(Grid_GroupMenu.ActualWidth, 0, Grid_RightSide.ActualWidth, 0);
        }

        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");

        private void TextBox_SearchMods_TextChanged(object sender, TextChangedEventArgs e) => RefreshDataGrid();

        private void ListBox_ModsGroupMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedIndex != -1 && listBox.SelectedItem is ListBoxItem item && item.Content is not Expander)
            {
                if (listBox.Name == ListBox_ModsGroupMenu.Name)
                {
                    ListBox_ModTypeGroup.SelectedIndex = -1;
                    ListBox_UserGroup.SelectedIndex = -1;
                }
                else if (listBox.Name == ListBox_ModTypeGroup.Name)
                {
                    ListBox_ModsGroupMenu.SelectedIndex = -1;
                    ListBox_UserGroup.SelectedIndex = -1;
                }
                else if (listBox.Name == ListBox_UserGroup.Name)
                {
                    ListBox_ModsGroupMenu.SelectedIndex = -1;
                    ListBox_ModTypeGroup.SelectedIndex = -1;
                }
                if (item != nowSelectedListBoxItem)
                {
                    nowSelectedListBoxItem = item;
                    if (allUserGroups.ContainsKey(item.ToolTip.ToString()!))
                        Expander_RandomEnable.Visibility = Visibility.Visible;
                    else
                        Expander_RandomEnable.Visibility = Visibility.Collapsed;
                    nowGroupName = item.Tag.ToString()!;
                    RefreshDataGrid();
                    ClearDataGridSelected();
                    CloseModDetails();
                }
            }
        }

        private void DataGrid_ModsShowList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            CloseModDetails();
            ClearDataGridSelected();
            DependencyObject scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, (FrameworkElement)Parent);
        }

        private void DataGridItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridRow row)
                ShowModDetails(row.Tag.ToString()!);
        }

        private void DataGridItem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridRow row)
                ShowModDetails(row.Tag.ToString()!);
        }

        private void DataGridItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 连续点击无效,需要 e.Handled = true
            e.Handled = true;
            if (sender is DataGridRow row)
                ChangeModInfoDetails(row.Tag.ToString()!);
        }

        private void DataGridItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is DataGridRow row)
                row.Background = (Brush)Application.Current.Resources["ColorLight2"];
        }

        private void DataGridItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is DataGridRow row)
                row.Background = (Brush)Application.Current.Resources["ColorBG"];
        }

        private void Button_Enabled_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                ChangeSelectedModsEnabled(!bool.Parse(button.Tag.ToString()!));
        }

        private void Button_Collected_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                ChangeSelectedModsCollected(!bool.Parse(button.Tag.ToString()!));
        }

        private void Button_ModPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                ST.OpenLink(button.Content.ToString()!);
        }

        private void Button_EnableDependencies_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_ModsShowList.SelectedItem is ModShowInfo showInfo)
            {
                string id = showInfo.Id;
                string err = null!;
                foreach (var dependencie in allModsShowInfo[id].Dependencies!.Split(" , "))
                {
                    if (allModsInfo.ContainsKey(dependencie))
                        ChangeModEnabled(dependencie, true);
                    else
                    {
                        err ??= $"{I18n.NotFoundDependencies}\n";
                        err += $"{dependencie}\n";
                    }
                }
                if (err != null)
                {
                    STLog.WriteLine(err, STLogLevel.WARN);
                    ST.ShowMessageBox(err, Panuon.WPF.UI.MessageBoxIcon.Warning);
                }
                CheckEnabledModsDependencies();
                RefreshCountOfListBoxItems();
                StartRemindSaveThread();
            }
        }

        private void Button_ImportUserData_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = I18n.ImportUserData,
                Filter = $"Toml {I18n.File}|*.toml"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                GetUserData(openFileDialog.FileName);
                RefreshModsContextMenu();
                RefreshCountOfListBoxItems();
            }
        }

        private void Button_ExportUserData_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = I18n.ExportUserData,
                Filter = $"Toml {I18n.File}|*.toml"
            };
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                SaveUserData(saveFileDialog.FileName);
            }
        }

        private void TextBox_UserDescription_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Keyboard.ClearFocus();
        }

        private void TextBox_UserDescription_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataGrid_ModsShowList.SelectedItem is ModShowInfo showInfo)
            {
                allModsShowInfo[showInfo.Id].UserDescription = TextBox_UserDescription.Text;
                StartRemindSaveThread();
            }
        }

        private void Button_AddUserGroup_Click(object sender, RoutedEventArgs e)
        {
            AddUserGroup window = new();
            window.Button_Yes.Click += (s, e) =>
            {
                string name = window.TextBox_Name.Text;
                if (name.Length > 0 && !allUserGroups.ContainsKey(name))
                {
                    if (name == ModTypeGroup.Collected || name == strUserCustomData)
                        ST.ShowMessageBox(string.Format(I18n.UserGroupCannotNamed, ModTypeGroup.Collected, strUserCustomData), setBlurEffect: false);
                    else
                    {
                        AddUserGroup(window.TextBox_Icon.Text, window.TextBox_Name.Text);
                        RefreshModsContextMenu();
                        RefreshCountOfListBoxItems();
                        window.Close();
                    }
                }
                else
                    ST.ShowMessageBox(I18n.UserGroupNamingFailed);
            };
            window.Button_Cancel.Click += (s, e) => window.Close();
            window.ShowDialog();
        }

        private void Button_GameStart_Click(object sender, RoutedEventArgs e)
        {
            if (ST.FileExists(GameInfo.ExeFile))
            {
                if (clearGameLogOnStart)
                    ClearGameLogFile();
                Process process = new();
                process.StartInfo.FileName = "cmd";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardInput = true;
                if (process.Start())
                {
                    process.StandardInput.WriteLine($"cd /d {GameInfo.Directory}");
                    process.StandardInput.WriteLine($"starsector.exe");
                    process.Close();
                    SaveAllData();
                    ResetRemindSaveThread();
                }
            }
        }

        private void DataGrid_ModsShowList_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid && GroupBox_ModInfo.IsMouseOver == false && DataGrid_ModsShowList.IsMouseOver == false)
                ClearDataGridSelected();
        }

        private void Button_ImportEnabledListFromSave_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = I18n.ImportFromSave,
                Filter = $"Xml {I18n.File}|*.xml"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                string? err = null;
                string filePath = $"{string.Join("\\", openFileDialog.FileName.Split("\\")[..^1])}\\descriptor.xml";
                if (ST.FileExists(filePath))
                {
                    IEnumerable<string> list = null!;
                    try
                    {
                        XElement xes = XElement.Load(filePath);
                        list = xes.Descendants("spec").Where(x => x.Element("id") != null).Select(x => (string)x.Element("id")!);
                    }
                    catch (Exception ex)
                    {
                        STLog.WriteLine($"{I18n.FileError} {I18n.Path}: {filePath}\n", ex);
                        ST.ShowMessageBox($"{I18n.FileError}\n{I18n.Path}: {filePath}\n", Panuon.WPF.UI.MessageBoxIcon.Question);
                        return;
                    }
                    var result = ST.ShowMessageBox(I18n.SelectImportMode, MessageBoxButton.YesNoCancel, Panuon.WPF.UI.MessageBoxIcon.Question);
                    if (result == MessageBoxResult.Yes)
                        ClearAllEnabledMods();
                    else if (result == MessageBoxResult.Cancel)
                        return;
                    foreach (string id in list)
                    {
                        if (allModsInfo.ContainsKey(id))
                            ChangeModEnabled(id, true);
                        else
                        {
                            STLog.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
                            err ??= $"{I18n.NotFoundMod}\n";
                            err += $"{id}\n";
                        }
                    }
                }
                if (err != null)
                {
                    STLog.WriteLine(err, STLogLevel.WARN);
                    ST.ShowMessageBox(err, Panuon.WPF.UI.MessageBoxIcon.Warning);
                }
            }
        }

        private void DataGrid_ModsShowList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is Array array)
            {
                STLog.WriteLine($"{I18n.ConfirmDragFiles} {I18n.Size}: {array.Length}");
                new Task(() =>
                {
                    int total = array.Length;
                    int completed = 0;
                    ModArchiveing window = null!;
                    Dispatcher.BeginInvoke(() =>
                    {
                        window = new ModArchiveing();
                        window.Label_Total.Content = total;
                        window.Label_Completed.Content = completed;
                        window.Label_Incomplete.Content = total;
                    });
                    foreach (string path in array)
                    {
                        if (ST.FileExists(path))
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                window.Label_Progress.Content = path;
                                window.ShowDialog();
                            });
                            DropFile(path);
                            Dispatcher.BeginInvoke(() =>
                            {
                                window.Label_Completed.Content = ++completed;
                                window.Label_Incomplete.Content = total - completed;
                            });
                        }
                    }
                    Dispatcher.BeginInvoke(() => window.Close());
                    GC.Collect();
                }).Start();
            }
        }

        private void ComboBox_SearchType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TextBox_SearchMods.Text.Length > 0)
                RefreshDataGrid();
        }

        private void Button_OpenModDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (ST.DirectoryExists(GameInfo.ModsDirectory))
                ST.OpenLink(GameInfo.ModsDirectory);
        }

        private void Button_OpenBackupDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (ST.DirectoryExists(backupDirectory))
                ST.OpenLink(backupDirectory);
        }

        private void Button_OpenSaveDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (ST.DirectoryExists(GameInfo.SaveDirectory))
                ST.OpenLink(GameInfo.SaveDirectory);
        }

        private void Button_RandomMods_Click(object sender, RoutedEventArgs e)
        {
            if (TextBox_MinRandomSize.Text.Length == 0 || TextBox_MaxRandomSize.Text.Length == 0)
            {
                ST.ShowMessageBox(I18n.RandomNumberCannotNull, Panuon.WPF.UI.MessageBoxIcon.Warning);
                return;
            }
            if (nowSelectedListBoxItem is ListBoxItem item && allUserGroups.ContainsKey(item.ToolTip.ToString()!))
            {
                string group = item.ToolTip.ToString()!;
                int minSize = int.Parse(TextBox_MinRandomSize.Text);
                int maxSize = int.Parse(TextBox_MaxRandomSize.Text);
                int count = allUserGroups[group].Count;
                if (maxSize > count)
                {
                    ST.ShowMessageBox(I18n.RandomNumberCannotGreaterTotal, Panuon.WPF.UI.MessageBoxIcon.Warning);
                    return;
                }
                else if (minSize > maxSize)
                {
                    ST.ShowMessageBox(I18n.MinRandomNumberCannotBeGreaterMaxRandomNumber, Panuon.WPF.UI.MessageBoxIcon.Warning);
                    return;
                }
                foreach (var info in allUserGroups[group])
                    ChangeModEnabled(info, false);
                int needSize = new Random(Guid.NewGuid().GetHashCode()).Next(minSize, maxSize + 1);
                HashSet<int> set = new();
                while (set.Count < needSize)
                    set.Add(new Random(Guid.NewGuid().GetHashCode()).Next(0, count));
                foreach (int i in set)
                    ChangeModEnabled(allUserGroups[group].ElementAt(i));
                CheckEnabledModsDependencies();
                RefreshCountOfListBoxItems();
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

        private void Grid_RightSide_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid_DataGrid.Margin = new Thickness(Grid_GroupMenu.ActualWidth, 0, Grid_RightSide.ActualWidth, 0);
        }

        private void Button_ImportUserGroup_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = I18n.ImportUserGroup,
                Filter = $"Toml {I18n.File}|*.toml"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                GetUserGroup(openFileDialog.FileName);
                RefreshModsContextMenu();
                RefreshCountOfListBoxItems();
                StartRemindSaveThread();
            }
        }

        private void Button_ExportUserGroup_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = I18n.ExportUserGroup,
                Filter = $"Toml {I18n.File}|*.toml"
            };
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                SaveUserGroup(saveFileDialog.FileName, ((ComboBoxItem)ComboBox_ExportUserGroup.SelectedItem).Tag.ToString()!);
            }
        }

        private void Button_CLoseModDetails_Click(object sender, RoutedEventArgs e)
        {
            CloseModDetails();
        }

        private void CheckBox_ClearLogOnStart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                clearGameLogOnStart = (bool)checkBox.IsChecked!;
                if (!ST.FileExists(ST.STConfigTomlFile))
                    return;
                TomlTable toml = TOML.Parse(ST.STConfigTomlFile);
                toml["Game"]["ClearLogOnStart"] = clearGameLogOnStart;
                toml.SaveTo(ST.STConfigTomlFile);
            }
        }
    }
}