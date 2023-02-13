﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using HKW.Libs.TomlParse;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using static HKW.Extension.SetExtension;
using I18n = StarsectorTools.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorTools.Pages.ModManager
{
    /// <summary>
    /// ModManagerPage.xaml 的交互逻辑
    /// </summary>
    public partial class ModManagerPage : Page, ISTPage
    {
        public bool NeedSave { get; private set; } = false;

        public string NameI18n { get; private set; } = "";

        public string DescriptionI18n { get; private set; } = "";

        public ReadOnlySet<string> I18nSet { get; private set; } = new(new()
        {
            "zh-CN"
        });

        /// <summary>
        /// 
        /// </summary>
        public ModManagerPage()
        {
            InitializeComponent();
            //throw new();
            InitializeData();
        }
        public bool ChangeLanguage()
        {
            return false;
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
                Title = I18n.ImportEnabledList,
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
                Title = I18n.ExportEnabledList,
                Filter = $"Json {I18n.File}|*.json"
            };
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                SaveEnabledMods(saveFileDialog.FileName);
            }
        }

        private void Button_ExpandGroupMenu_Click(object sender, RoutedEventArgs e)
        {
            if (expandGroupMenu)
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
            expandGroupMenu = !expandGroupMenu;
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
                Utils.OpenLink(button.Content.ToString()!);
        }

        private void Button_EnableDependencies_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_ModsShowList.SelectedItem is ModShowInfo showInfo)
            {
                string id = showInfo.Id;
                string err = null!;
                foreach (var dependencie in allModsShowInfo[id].Dependencies!.Split(" , "))
                {
                    if (allModInfos.ContainsKey(dependencie))
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
                    Utils.ShowMessageBox(err, STMessageBoxIcon.Warning);
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
                        Utils.ShowMessageBox(string.Format(I18n.UserGroupCannotNamed, ModTypeGroup.Collected, strUserCustomData), setBlurEffect: false);
                    else
                    {
                        AddUserGroup(window.TextBox_Icon.Text, window.TextBox_Name.Text);
                        RefreshModsContextMenu();
                        RefreshCountOfListBoxItems();
                        window.Close();
                    }
                }
                else
                    Utils.ShowMessageBox(I18n.UserGroupNamingFailed);
            };
            window.Button_Cancel.Click += (s, e) => window.Close();
            window.ShowDialog();
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
                if (Utils.FileExists(filePath))
                {
                    IEnumerable<string> list = null!;
                    try
                    {
                        XElement xes = XElement.Load(filePath);
                        list = xes.Descendants("spec").Where(x => x.Element("id") != null).Select(x => x.Element("id")!.Value);
                    }
                    catch (Exception ex)
                    {
                        STLog.WriteLine($"{I18n.FileError} {I18n.Path}: {filePath}\n", ex);
                        Utils.ShowMessageBox($"{I18n.FileError}\n{I18n.Path}: {filePath}\n", STMessageBoxIcon.Question);
                        return;
                    }
                    var result = Utils.ShowMessageBox(I18n.SelectImportMode, MessageBoxButton.YesNoCancel, STMessageBoxIcon.Question);
                    if (result == MessageBoxResult.Yes)
                        ClearAllEnabledMods();
                    else if (result == MessageBoxResult.Cancel)
                        return;
                    foreach (string id in list)
                    {
                        if (allModInfos.ContainsKey(id))
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
                    Utils.ShowMessageBox(err, STMessageBoxIcon.Warning);
                }
            }
        }

        private void DataGrid_ModsShowList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is Array fileArray)
            {
                STLog.WriteLine($"{I18n.ConfirmDragFiles} {I18n.Size}: {fileArray.Length}");
                new Task(() =>
                {
                    int total = fileArray.Length;
                    int completed = 0;
                    ModArchiveing window = null!;
                    Dispatcher.InvokeAsync(() =>
                    {
                        window = new ModArchiveing();
                        window.Label_Total.Content = total;
                        window.Label_Completed.Content = completed;
                        window.Label_Incomplete.Content = total;
                    });
                    foreach (string file in fileArray)
                    {
                        if (Utils.FileExists(file))
                        {
                            Dispatcher.InvokeAsync(() =>
                            {
                                window.Label_Progress.Content = file;
                                window.ShowDialog();
                            });
                            DropFile(file);
                            Dispatcher.InvokeAsync(() =>
                            {
                                window.Label_Completed.Content = ++completed;
                                window.Label_Incomplete.Content = total - completed;
                            });
                        }
                    }
                    Dispatcher.InvokeAsync(() => window.Close());
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
            if (Utils.DirectoryExists(GameInfo.ModsDirectory))
                Utils.OpenLink(GameInfo.ModsDirectory);
        }

        private void Button_OpenBackupDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.DirectoryExists(backupDirectory))
                Utils.OpenLink(backupDirectory);
        }

        private void Button_OpenSaveDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Utils.DirectoryExists(GameInfo.SaveDirectory))
                Utils.OpenLink(GameInfo.SaveDirectory);
        }

        private void Button_RandomMods_Click(object sender, RoutedEventArgs e)
        {
            if (TextBox_MinRandomSize.Text.Length == 0 || TextBox_MaxRandomSize.Text.Length == 0)
            {
                Utils.ShowMessageBox(I18n.RandomNumberCannotNull, STMessageBoxIcon.Warning);
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
                    Utils.ShowMessageBox(I18n.RandomNumberCannotGreaterTotal, STMessageBoxIcon.Warning);
                    return;
                }
                else if (minSize > maxSize)
                {
                    Utils.ShowMessageBox(I18n.MinRandomNumberCannotBeGreaterMaxRandomNumber, STMessageBoxIcon.Warning);
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
    }
}