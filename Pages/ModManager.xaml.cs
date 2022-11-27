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
using System.ComponentModel;
using System.Xml.Linq;

namespace StarsectorTools.Pages
{
    /// <summary>
    /// ModManager.xaml 的交互逻辑
    /// </summary>
    public partial class ModManager : Page
    {
        const string modGroupPath = @"ModGroup.toml";
        readonly static Uri modGroupUri = new("/Resources/ModGroup.toml", UriKind.Relative);
        const string userGroupPath = @"UserGroup.toml";
        bool groupMenuOpen = false;
        bool showModInfo = false;
        string? nowSelectedMod = null;
        string nowGroup = ModGroupType.All;
        HashSet<string> enabledModsId = new();
        HashSet<string> collectedModsId = new();
        Dictionary<string, ModInfo> modsInfo = new();
        Dictionary<string, ListBoxItem> listBoxItemsFromGroups = new();
        Dictionary<string, ModShowInfo> modsShowInfo = new();
        Dictionary<string, HashSet<string>> userGroups = new();
        Dictionary<string, ObservableCollection<ModShowInfo>> modShowInfoFromGroup = new()
        {
            {ModGroupType.All,new() },
            {ModGroupType.Enabled,new() },
            {ModGroupType.Disable,new() },
            {ModGroupType.Libraries,new() },
            {ModGroupType.Megamods,new() },
            {ModGroupType.FactionMods,new() },
            {ModGroupType.ContentExpansions,new() },
            {ModGroupType.UtilityMods,new() },
            {ModGroupType.MiscellaneousMods,new() },
            {ModGroupType.BeautifyMods,new() },
            {ModGroupType.Unknown,new() },
            {ModGroupType.Collected,new() },
        };
        static class ModGroupType
        {
            /// <summary>全部模组</summary>
            public const string All = "All";
            /// <summary>已启用模组</summary>
            public const string Enabled = "Enabled";
            /// <summary>未启用模组</summary>
            public const string Disable = "Disable";
            /// <summary>前置模组</summary>
            public const string Libraries = "Libraries";
            /// <summary>大型模组</summary>
            public const string Megamods = "Megamods";
            /// <summary>派系模组</summary>
            public const string FactionMods = "FactionMods";
            /// <summary>内容模组</summary>
            public const string ContentExpansions = "ContentExpansions";
            /// <summary>功能模组</summary>
            public const string UtilityMods = "UtilityMods";
            /// <summary>闲杂模组</summary>
            public const string MiscellaneousMods = "MiscellaneousMods";
            /// <summary>美化模组</summary>
            public const string BeautifyMods = "BeautifyMods";
            /// <summary>全部模组</summary>
            public const string Unknown = "Unknown";
            /// <summary>已收藏模组</summary>
            public const string Collected = "Collected";
        };
        class ButtonStyle
        {
            public Style Enabled = null!;
            public Style Disable = null!;
            public Style Collected = null!;
            public Style Uncollected = null!;
        }
        readonly ButtonStyle buttonStyle = new();
        class LabelStyle
        {
            public Style VersionNormal = null!;
            public Style VersionWarn = null!;
            public Style IsUtility = null!;
            public Style NotUtility = null!;
        }
        readonly LabelStyle labelStyle = new();
        public class ModShowInfo : INotifyPropertyChanged
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Author { get; set; }
            public string? Version { get; set; }
            public string? GameVersion { get; set; }
            private Style? gameVersionStyle = null;
            public Style? GameVersionStyle
            {
                get { return gameVersionStyle; }
                set
                {
                    gameVersionStyle = value;
                    PropertyChanged?.Invoke(this, new(nameof(GameVersionStyle)));
                }
            }
            public bool? Utility { get; set; }
            private Style? utilityStyle = null;
            public Style? UtilityStyle
            {
                get { return utilityStyle; }
                set
                {
                    utilityStyle = value;
                    //PropertyChanged?.Invoke(this, new(nameof(UtilityStyle)));
                }
            }
            public bool? Enabled { get; set; }
            public string? ImagePath { get; set; }
            public string? Group { get; set; }
            private string? dependencies { get; set; }
            public string? Dependencies
            {
                get { return dependencies; }
                set
                {
                    dependencies = value;
                    PropertyChanged?.Invoke(this, new(nameof(Dependencies)));
                }
            }
            public List<string>? DependenciesList { get; set; }
            private double rowDetailsHight { get; set; }
            public double RowDetailsHight
            {
                get { return rowDetailsHight; }
                set
                {
                    rowDetailsHight = value;
                    PropertyChanged?.Invoke(this, new(nameof(RowDetailsHight)));
                }
            }
            private string? userDescription { get; set; }
            public string? UserDescription
            {
                get { return userDescription; }
                set
                {
                    userDescription = value;
                    PropertyChanged?.Invoke(this, new(nameof(UserDescription)));
                }
            }
            private Style? enabledStyle = null;
            public Style? EnabledStyle
            {
                get { return enabledStyle; }
                set
                {
                    enabledStyle = value;
                    PropertyChanged?.Invoke(this, new(nameof(EnabledStyle)));
                }
            }
            public bool? Collected { get; set; }
            private Style? collectedStyle = null;
            public Style? CollectedStyle
            {
                get { return collectedStyle; }
                set
                {
                    collectedStyle = value;
                    PropertyChanged?.Invoke(this, new(nameof(CollectedStyle)));
                }
            }
            private ContextMenu? contextMenu = null;
            public ContextMenu? ContextMenu
            {
                get { return contextMenu; }
                set
                {
                    contextMenu = value;
                    PropertyChanged?.Invoke(this, new(nameof(ContextMenu)));
                }
            }
            public event PropertyChangedEventHandler? PropertyChanged;
        }
        public ModManager()
        {
            InitializeComponent();
            InitializeData();
            GetAllMods();
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
            if (TextBox_ModsSearch.Text.Length > 0 && TextBox_ModsSearch.Text is string text)
            {
                ObservableCollection<ModShowInfo> showModInfos = new();
                switch (((ComboBoxItem)ComboBox_SearchType.SelectedItem).Tag.ToString()!)
                {
                    case "Name":
                        foreach (var info in modShowInfoFromGroup[nowGroup].Where(i => i.Name!.Contains(text)))
                            showModInfos.Add(info);
                        break;
                    case "Id":
                        foreach (var info in modShowInfoFromGroup[nowGroup].Where(i => i.Id!.Contains(text)))
                            showModInfos.Add(info);
                        break;
                    case "Author":
                        foreach (var info in modShowInfoFromGroup[nowGroup].Where(i => i.Author!.Contains(text)))
                            showModInfos.Add(info);
                        break;
                    case "UserDescription":
                        foreach (var info in modShowInfoFromGroup[nowGroup].Where(i => i.UserDescription!.Contains(text)))
                            showModInfos.Add(info);
                        break;
                }
                DataGrid_ModsShowList.ItemsSource = showModInfos;
            }
            else
                DataGrid_ModsShowList.ItemsSource = modShowInfoFromGroup[nowGroup];
            GC.Collect();
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
                    if (modsInfo.ContainsKey(key))
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
                ClearDataGridSelected();
                DataGrid_ModsShowList.ItemsSource = modShowInfoFromGroup[nowGroup];
                CloseModInfo();
                GC.Collect();
            }
        }
        private void DataGridItem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridRow row)
                ModInfoShowChange(row.Tag.ToString()!);
        }

        private void DataGridItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is DataGridRow row)
                ShowModInfo(row.Tag.ToString()!);
        }
        private void DataGridItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 连续点击无效,需要 e.Handled = true
            e.Handled = true;
            if (sender is DataGridRow row)
                ModInfoShowChange(row.Tag.ToString()!);
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
                foreach (var dependencie in modsShowInfo[id].Dependencies!.Split(" , "))
                {
                    if (modsInfo.ContainsKey(dependencie))
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
                modsShowInfo[item.Id!].UserDescription = new(TextBox_UserDescription.Text);
        }
        private void Button_AddGroup_Click(object sender, RoutedEventArgs e)
        {
            Window_AddGroup window = new();
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;
            window.Show();
            window.Button_OK.Click += (o, e) =>
            {
                string name = window.TextBox_Name.Text;
                if (name.Length > 0 && !userGroups.ContainsKey(name))
                {
                    if (name == "Collected" || name == "UserModsData")
                        MessageBox.Show("不能命名为Collected或UserModsData");
                    else
                    {
                        AddUserGroup(window.TextBox_Icon.Text, window.TextBox_Name.Text);
                        window.Close();
                    }
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
                    if (_name.Length > 0 && !userGroups.ContainsKey(_name))
                    {
                        ListBoxItemHelper.SetIcon(item, window.TextBox_Icon.Text);
                        var temp = userGroups[name];
                        userGroups.Remove(name);
                        userGroups.Add(_name, temp);

                        listBoxItemsFromGroups.Remove(name);
                        listBoxItemsFromGroups.Add(_name, item);

                        var _temp = modShowInfoFromGroup[name];
                        modShowInfoFromGroup.Remove(name);
                        modShowInfoFromGroup.Add(_name, _temp);

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
                userGroups.Remove(_name);
                listBoxItemsFromGroups.Remove(_name);
                modShowInfoFromGroup.Remove(_name);
            };
            contextMenu.Items.Add(menuItem);
            item.ContextMenu = contextMenu;
            ListBoxItemHelper.SetIcon(item, icon);
            ListBox_UserGroup.Items.Add(item);
            userGroups.Add(name, new());
            listBoxItemsFromGroups.Add(name, item);
            modShowInfoFromGroup.Add(name, new());
            SetAllSizeInListBoxItem();
            RefreshShowModsItemContextMenu();
        }
        void SetListBoxItemData(ListBoxItem item, string name)
        {
            item.Content = $"{name} ";
            item.ToolTip = name;
            item.Tag = name;
        }

        private void Button_GameStart_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Global.gameExePath))
            {
                Process process = new();
                process.StartInfo.FileName = "cmd";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardInput = true;
                if (process.Start())
                {
                    process.StandardInput.WriteLine($"cd /d {Global.gamePath}");
                    process.StandardInput.WriteLine($"starsector.exe");
                    process.Close();
                }
            }
            else
            {
                MessageBox.Show($"启动错误\n{Global.gameExePath}不存在");
            }
        }
        private void DataGrid_ModsShowList_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid grid && GroupBox_ModInfo.IsMouseOver == false && DataGrid_ModsShowList.IsMouseOver == false)
            {
                ClearDataGridSelected();
                CloseModInfo();
            }
        }
        void ClearDataGridSelected()
        {
            while (DataGrid_ModsShowList.SelectedItems.Count > 0)
            {
                if (DataGrid_ModsShowList.ItemContainerGenerator.ContainerFromItem(DataGrid_ModsShowList.SelectedItems[0]) is DataGridRow row)
                    row.IsSelected = false;
            }
        }

        private void Button_ImportEnabledListFromSave_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = "从游戏存档导入启动列表",
                Filter = "Xml File|*.xml"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                string? err = null;
                string filePath = $"{string.Join("\\", openFileDialog.FileName.Split("\\")[..^1])}\\descriptor.xml";
                if (File.Exists(filePath))
                {
                    IEnumerable<string> list = null!;
                    try
                    {
                        XElement xes = XElement.Load(filePath);
                        list = xes.Descendants("spec").Where(x => x.Element("id") != null).Select(x => (string)x.Element("id")!);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"存档文件错误\n{ex}");
                        return;
                    }
                    ClearEnabledMod();
                    foreach (string id in list)
                    {
                        if (modsInfo.ContainsKey(id))
                            ModEnabledChange(id, true);
                        else
                        {
                            err ??= "存档中的以下模组不存在\n";
                            err += $"{id}\n";
                        }
                    }
                }
                else
                    MessageBox.Show($"存档文件不存在\n位置:{filePath}");
                if (err != null)
                    MessageBox.Show(err);
            }
        }
    }
}
