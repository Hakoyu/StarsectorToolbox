using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
using StarsectorTools.Langs.MessageBox;
using StarsectorTools.Lib;
using StarsectorTools.Windows;
using Panuon.WPF.UI;

namespace StarsectorTools.Pages
{
    /// <summary>
    /// Page_ModManager.xaml 的交互逻辑
    /// </summary>
    public partial class Page_ModManager : Page
    {
        public Page_ModManager()
        {
            InitializeComponent();
            InitializeData();
            GetAllMods();
            GetAllModGroup();
            GetEnabledMods();
            GetAllListBoxItem();
            InitializeDataGridItemsSource();
            CheckUserGroup();
            SetAllSizeInListBoxItem();
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

        private void TextBox_ModsSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchMods();
        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            SeveAllData();
        }

        private void Button_ImportEnabledList_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "导入已启用模组列表",
                Filter = "Json File|*.json"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                enabledModsId.Clear();
                string datas = File.ReadAllText(openFileDialog.FileName);
                string nope = null!;
                JsonNode enabledModsJson = JsonNode.Parse(datas)!;
                JsonArray enabledModsJsonArray = enabledModsJson["enabledMods"]!.AsArray();
                foreach (var mod in enabledModsJsonArray)
                {
                    var key = mod!.ToString();
                    if (allModsInfo.ContainsKey(key))
                    {
                        ModEnabledChange(key, true);
                    }
                    else
                    {
                        nope ??= "并未找到导入列表中的以下模组:\n";
                        nope += $"{key}\n";
                    }
                }
                if (nope != null)
                    MessageBox.Show(nope, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_ExportEnabledList_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "导出已启用模组列表",
                Filter = "Json File|*.json"
            };
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                SaveEnabledMods(saveFileDialog.FileName);
            }
        }

        private void Button_GroupMenu_Click(object sender, RoutedEventArgs e)
        {
            if (groupMenuOpen)
            {
                Button_GroupMenu.Content = "📘";
                Grid_GroupMenu.Width = 30;
            }
            else
            {
                Button_GroupMenu.Content = "📖";
                Grid_GroupMenu.Width = double.NaN;
            }
            groupMenuOpen = !groupMenuOpen;
        }
        private void Grid_GroupMenu_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid_DataGrid.Margin = new Thickness() { Left = Grid_GroupMenu.ActualWidth, Top = 0, Right = 0, Bottom = 0 };
        }

        private void ListBox_ModsGroupMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedIndex != -1 && listBox.SelectedItem is ListBoxItem item && item.Content is not Expander)
            {
                if (listBox.Name == ListBox_ModsGroupMenu.Name)
                {
                    ListBox_EnableStatus.SelectedIndex = -1;
                    ListBox_GroupType.SelectedIndex = -1;
                    ListBox_UserGroup.SelectedIndex = -1;
                }
                else if (listBox.Name == ListBox_EnableStatus.Name)
                {
                    ListBox_ModsGroupMenu.SelectedIndex = -1;
                    ListBox_GroupType.SelectedIndex = -1;
                    ListBox_UserGroup.SelectedIndex = -1;
                }
                else if (listBox.Name == ListBox_GroupType.Name)
                {
                    ListBox_ModsGroupMenu.SelectedIndex = -1;
                    ListBox_EnableStatus.SelectedIndex = -1;
                    ListBox_UserGroup.SelectedIndex = -1;
                }
                else if (listBox.Name == ListBox_UserGroup.Name)
                {
                    ListBox_ModsGroupMenu.SelectedIndex = -1;
                    ListBox_EnableStatus.SelectedIndex = -1;
                    ListBox_GroupType.SelectedIndex = -1;
                }
                nowGroup = item.Tag.ToString()!;
                DataGrid_ModsShowList.ItemsSource = allShowModInfoAtGroup[nowGroup];
                CloseModInfo();
            }
        }

        private void DataGridItem_GotFocus(object sender, RoutedEventArgs e)
        {
            ShowModInfo((DataGridRow)sender);
        }
        private void DataGridItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 连续点击无效,需要 e.Handled = true
            e.Handled = true;
            ShowModInfo((DataGridRow)sender);
        }

        private void Button_Enabled_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                SelectedModsEnabledChange(!bool.Parse(button.ToolTip.ToString()!));
        }

        private void Button_Collected_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                SelectedModsCollectedChange(!bool.Parse(button.ToolTip.ToString()!));
        }

        private void Button_ModPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
                Process.Start("Explorer.exe", button.Content.ToString()!);
        }

        private void Button_EnableDependencies_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_ModsShowList.SelectedItem is ModShowInfo info)
            {
                string id = info.Id!;
                string err = null!;
                foreach (var dependencie in allShowModInfo[id].Dependencies!.Split(" , "))
                {
                    if (allModsInfo.ContainsKey(dependencie))
                        ModEnabledChange(dependencie, true);
                    else
                    {
                        err ??= "作为前置的以下模组不存在\n";
                        err += $"{dependencie}\n";
                    }
                }
                if (err != null)
                    MessageBox.Show(err);
                CheckEnabledModsDependencies();
                SetAllSizeInListBoxItem();
            }
        }

        private void Button_ImportUserGroup_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "导入用户分组",
                Filter = "Toml File|*.toml"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                GetAllUserGroup(openFileDialog.FileName);
            }
        }

        private void Button_ExportUserGroup_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = "导出用户列表",
                Filter = "Toml File|*.toml"
            };
            if (saveFileDialog.ShowDialog().GetValueOrDefault())
            {
                SaveUserGroup(saveFileDialog.FileName);
            }
        }

        private void TextBox_UserDescription_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Keyboard.ClearFocus();
        }

        private void TextBox_UserDescription_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataGrid_ModsShowList.SelectedItem is ModShowInfo item)
                allShowModInfo[item.Id!].UserDescription = new(TextBox_UserDescription.Text);
            TextBox_UserDescription.Text = "";
        }
        private void Button_AddGroup_Click(object sender, RoutedEventArgs e)
        {
            Window_AddGroup window = new();
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;
            window.Show();
            window.Button_OK.Click += (o, e) =>
            {
                if (window.TextBox_Name.Text.Length > 0 && !allUserGroup.ContainsKey(window.TextBox_Name.Text))
                {
                    AddUserGroup(window.TextBox_Icon.Text, window.TextBox_Name.Text);
                    window.Close();
                }
                else
                    MessageBox.Show("创建失败,名字为空或者已存在相同名字的分组");
            };
            window.Button_Cancel.Click += (o, e) => window.Close();
            window.Closed += (o, e) => ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
        }
        void AddUserGroup(string icon, string name)
        {
            ListBoxItem item = new();
            SetListBoxItemData(item, name);
            ContextMenu contextMenu = new();
            MenuItem menuItem = new();
            menuItem.Header = "重命名分组";
            menuItem.Click += (o, e) =>
            {
                Window_AddGroup window = new();
                ((MainWindow)Application.Current.MainWindow).IsEnabled = false;
                window.Show();
                window.Button_OK.Click += (o, e) =>
                {
                    string _icon = window.TextBox_Icon.Text;
                    string _name = window.TextBox_Name.Text;
                    if (_name.Length > 0 && !allUserGroup.ContainsKey(_name))
                    {
                        ListBoxItemHelper.SetIcon(item, window.TextBox_Icon.Text);
                        var temp = allUserGroup[name];
                        allUserGroup.Remove(name);
                        allUserGroup.Add(_name, temp);
                        allGroupListBoxItems.Remove(name);
                        allGroupListBoxItems.Add(_name, item);
                        var _temp = allShowModInfoAtGroup[name];
                        allShowModInfoAtGroup.Remove(name);
                        allShowModInfoAtGroup.Add(_name, _temp);
                        SetListBoxItemData(item, _name);
                        window.Close();
                    }
                    else
                        MessageBox.Show("命名失败,名字为空或者已存在相同名字的分组");
                };
                window.Button_Cancel.Click += (o, e) => window.Close();
                window.Closed += (o, e) => ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
            };
            contextMenu.Items.Add(menuItem);
            menuItem = new();
            menuItem.Header = "删除分组";
            menuItem.Click += (o, e) =>
            {
                var _itme = (ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)o));
                var _name = _itme.Content.ToString()!.Split(" ")[0];
                ListBox_UserGroup.Items.Remove(_itme);
                allUserGroup.Remove(_name);
                allGroupListBoxItems.Remove(_name);
                allShowModInfoAtGroup.Remove(_name);
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBoxItemHelper.SetIcon(item, icon);
            ListBox_UserGroup.Items.Add(item);
            allUserGroup.Add(name, new());
            allGroupListBoxItems.Add(name, item);
            allShowModInfoAtGroup.Add(name, new());
            SetAllSizeInListBoxItem();
            RefreshShowModsItemContextMenu();
        }
        void SetListBoxItemData(ListBoxItem item, string name)
        {
            item.Content = $"{name} ";
            item.ToolTip = name;
            item.Tag = name;
        }
    }
}
