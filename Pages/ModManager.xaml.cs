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
using StarsectorTools.Langs.MessageBox;
using StarsectorTools.Lib;
using StarsectorTools.Windows;
using Panuon.WPF.UI;
using System.ComponentModel;
using System.Xml.Linq;
using Aspose.Zip;
using Aspose.Zip.SevenZip;

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
        Dictionary<string, ModInfo> allModsInfo = new();
        Dictionary<string, ListBoxItem> listBoxItemsFromGroups = new();
        Dictionary<string, ModShowInfo> modsShowInfo = new();
        Dictionary<string, HashSet<string>> userGroups = new();
        Dictionary<string, HashSet<string>> modsIdFromGroups = new()
        {
            {ModGroupType.Libraries,new() },
            {ModGroupType.Megamods,new() },
            {ModGroupType.FactionMods,new() },
            {ModGroupType.ContentExpansions,new() },
            {ModGroupType.UtilityMods,new() },
            {ModGroupType.MiscellaneousMods,new() },
            {ModGroupType.BeautifyMods,new() },
            {ModGroupType.Unknown,new() },
        };
        Dictionary<string, ObservableCollection<ModShowInfo>> modsShowInfoFromGroup = new()
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
            public string Id { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string Author { get; set; } = null!;
            public string Version { get; set; } = null!;
            public string GameVersion { get; set; } = null!;
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
            public Style? UtilityStyle { get; set; }
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
            GetAllModsInfo();
            CheckEnabledMods();
            GetAllListBoxItems();
            GetAllGroup();
            InitializeDataGridItemsSource();
            CheckUserGroup();
            RefreshModsShowInfoContextMenu();
            RefreAllSizeOfListBoxItems();
            STLog.Instance.WriteLine(this, "初始化完成");
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

        private void TextBox_SearchMods_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchMods(TextBox_SearchMods.Text);
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
                GetEnabledMods(openFileDialog.FileName);
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
            STLog.Instance.WriteLine(this, $"分组菜单展开状态修改为 {groupMenuOpen}");
        }
        private void Grid_GroupMenu_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid_DataGrid.Margin = new Thickness(Grid_GroupMenu.ActualWidth, 0, 0, 0);
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
                SearchMods(TextBox_SearchMods.Text);
                CloseModInfo();
                GC.Collect();
            }
        }
        private void DataGridItem_GotFocus(object sender, RoutedEventArgs e)
        {
            //if (sender is DataGridRow row)
            //    ModInfoShowChange(row.Tag.ToString()!);
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
                string id = info.Id;
                string err = null!;
                foreach (var dependencie in modsShowInfo[id].Dependencies!.Split(" , "))
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
                RefreAllSizeOfListBoxItems();
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
                GetUserGroup(openFileDialog.FileName);
                RefreAllSizeOfListBoxItems();
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
                modsShowInfo[item.Id].UserDescription = new(TextBox_UserDescription.Text);
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
                        RefreAllSizeOfListBoxItems();
                        window.Close();
                    }
                }
                else
                    MessageBox.Show("创建失败,名字为空或者已存在相同名字的分组");
            };
            window.Button_Cancel.Click += (o, e) => window.Close();
            window.Closed += (o, e) => ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
        }

        private void Button_GameStart_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(ST.gameExePath))
            {
                Process process = new();
                process.StartInfo.FileName = "cmd";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardInput = true;
                if (process.Start())
                {
                    process.StandardInput.WriteLine($"cd /d {ST.gamePath}");
                    process.StandardInput.WriteLine($"starsector.exe");
                    process.Close();
                }
            }
            else
            {
                MessageBox.Show($"启动错误\n{ST.gameExePath}不存在");
            }
        }
        private void DataGrid_ModsShowList_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid grid && GroupBox_ModInfo.IsMouseOver == false && DataGrid_ModsShowList.IsMouseOver == false)
            {
                ClearDataGridSelected();
                //CloseModInfo();
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
                        if (allModsInfo.ContainsKey(id))
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

        private void DataGrid_ModsShowList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is Array array)
            {
                new Task(() =>
                {
                    foreach (string path in array)
                    {
                        if (File.Exists(path))
                            DropFile(path);
                        else
                            MessageBox.Show($"无法导入文件夹\n{path}");
                    }
                    GC.Collect();
                }).Start();
            }
        }
        
    }
}
