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
using StarsectorTools.Lib;
using StarsectorTools.Windows;
using Panuon.WPF.UI;
using System.ComponentModel;
using System.Xml.Linq;
using StarsectorTools.Tools.ModManager;
using I18n = StarsectorTools.Langs.Tools.ModManager.ModManager_I18n;

namespace StarsectorTools.Tools.ModManager
{
    public static class ModGroupType
    {
        /// <summary>全部模组</summary>
        public const string All = nameof(All);
        /// <summary>已启用模组</summary>
        public const string Enabled = nameof(Enabled);
        /// <summary>未启用模组</summary>
        public const string Disabled = nameof(Disabled);
        /// <summary>前置模组</summary>
        public const string Libraries = nameof(Libraries);
        /// <summary>大型模组</summary>
        public const string MegaMods = nameof(MegaMods);
        /// <summary>派系模组</summary>
        public const string FactionMods = nameof(FactionMods);
        /// <summary>内容模组</summary>
        public const string ContentExpansions = nameof(ContentExpansions);
        /// <summary>功能模组</summary>
        public const string UtilityMods = nameof(UtilityMods);
        /// <summary>闲杂模组</summary>
        public const string MiscellaneousMods = nameof(MiscellaneousMods);
        /// <summary>美化模组</summary>
        public const string BeautifyMods = nameof(BeautifyMods);
        /// <summary>全部模组</summary>
        public const string UnknownMods = nameof(UnknownMods);
        /// <summary>已收藏模组</summary>
        public const string Collected = nameof(Collected);
    }
    /// <summary>
    /// ModManager.xaml 的交互逻辑
    /// </summary>
    public partial class ModManager : Page
    {
        const string modGroupFile = "ModGroup.toml";
        const string modBackupDirectory = @"BackUp\Mods";
        const string backupDirectory = "Backup";
        readonly static Uri modGroupUri = new("/Resources/ModGroup.toml", UriKind.Relative);
        const string userGroupFile = "UserData.toml";
        const string collected = "Collecte";
        const string userCustomData = "UserCustomData";
        bool groupMenuOpen = false;
        bool showModInfo = false;
        string? nowSelectedMod = null;
        string nowGroup = ModGroupType.All;
        Thread remindSaveThread = null!;
        ListBoxItem? nowSelectedListBoxItem = null;
        HashSet<string> allEnabledModsId = new();
        HashSet<string> allCollectedModsId = new();
        Dictionary<string, ModInfo> allModsInfo = new();
        Dictionary<string, ListBoxItem> allListBoxItemsFromGroups = new();
        Dictionary<string, ModShowInfo> allModsShowInfo = new();
        Dictionary<string, HashSet<string>> allUserGroups = new();
        Dictionary<string, HashSet<string>> modsIdFromGroups = new();
        Dictionary<string, ObservableCollection<ModShowInfo>> modsShowInfoFromGroup = new();

        class ButtonStyle
        {
            public Style Enabled = null!;
            public Style Disable = null!;
            public Style Collecte = null!;
            public Style CancelCollection = null!;
        }
        readonly ButtonStyle buttonStyle = new();
        class LabelStyle
        {
            public Style GameVersionNormal = null!;
            public Style GameVersionWarn = null!;
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
            public Style? GameVersionStyle { get; set; }
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
            RefreshList();
            STLog.Instance.WriteLine(I18n.InitialisationComplete);
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
            SaveAllData();
            ResetRemindSaveThread();
        }

        private void Button_ImportEnabledList_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = I18n.ImportEnabledModsListFromFile,
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
                Title = I18n.ExportEnabledModsListToFile,
                Filter = $"Json {I18n.File}|*.json"
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
            groupMenuOpen = !groupMenuOpen;
        }
        private void Grid_GroupMenu_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid_DataGrid.Margin = new Thickness(Grid_GroupMenu.ActualWidth, 0, Grid_RightSide.ActualWidth, 0);
        }
        private void TextBox_NumberInput(object sender, TextCompositionEventArgs e) => e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        private void ListBox_ModsGroupMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedIndex != -1 && listBox.SelectedItem is ListBoxItem item && item.Content is not Expander)
            {
                if (listBox.Name == ListBox_ModsGroupMenu.Name)
                {
                    ListBox_GroupType.SelectedIndex = -1;
                    ListBox_UserGroup.SelectedIndex = -1;
                }
                else if (listBox.Name == ListBox_GroupType.Name)
                {
                    ListBox_ModsGroupMenu.SelectedIndex = -1;
                    ListBox_UserGroup.SelectedIndex = -1;
                }
                else if (listBox.Name == ListBox_UserGroup.Name)
                {
                    ListBox_ModsGroupMenu.SelectedIndex = -1;
                    ListBox_GroupType.SelectedIndex = -1;
                }
                nowSelectedListBoxItem = item;
                if (allUserGroups.ContainsKey(item.ToolTip.ToString()!))
                    Expander_RandomEnable.Visibility = Visibility.Visible;
                else
                    Expander_RandomEnable.Visibility = Visibility.Collapsed;
                nowGroup = item.Tag.ToString()!;
                ClearDataGridSelected();
                SearchMods(TextBox_SearchMods.Text);
                CloseModInfo();
                GC.Collect();
            }
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
                ChangeModInfoShow(row.Tag.ToString()!);
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
                ST.OpenFile(button.Content.ToString()!);
        }

        private void Button_EnableDependencies_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_ModsShowList.SelectedItem is ModShowInfo info)
            {
                string id = info.Id;
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
                    MessageBox.Show(err);
                CheckEnabledModsDependencies();
                RefreshCountOfListBoxItems();
                StartRemindSaveThread();
            }
        }

        private void Button_ImportUserData_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = I18n.ImportUserDataFile,
                Filter = $"Toml {I18n.File}|*.toml"
            };
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                GetUserGroup(openFileDialog.FileName);
                RefreshModsContextMenu();
                RefreshCountOfListBoxItems();
            }
        }

        private void Button_ExportUserData_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Title = I18n.ExportUserDataFile,
                Filter = $"Toml {I18n.File}|*.toml"
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
            {
                allModsShowInfo[item.Id].UserDescription = TextBox_UserDescription.Text;
                StartRemindSaveThread();
            }
        }
        private void Button_AddUserGroup_Click(object sender, RoutedEventArgs e)
        {
            AddUserGroup window = new();
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;
            window.Show();
            window.Button_Yes.Click += (o, e) =>
            {
                string name = window.TextBox_Name.Text;
                if (name.Length > 0 && !allUserGroups.ContainsKey(name))
                {
                    if (name == ModGroupType.Collected || name == userCustomData)
                        MessageBox.Show(ST.I18nParseKey(I18n.UserGroupCannotNamed, ModGroupType.Collected, userCustomData));
                    else
                    {
                        AddUserGroup(window.TextBox_Icon.Text, window.TextBox_Name.Text);
                        RefreshModsContextMenu();
                        RefreshCountOfListBoxItems();
                        window.Close();
                    }
                }
                else
                    MessageBox.Show(I18n.AddUserNamingFailed);
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
                    SaveAllData();
                    ResetRemindSaveThread();
                }
            }
            else
            {
                STLog.Instance.WriteLine($"{I18n.NotFoundFile}\n {I18n.Path}: {ST.gameExePath}", STLogLevel.WARN);
                MessageBox.Show($"{I18n.NotFoundFile}\n {I18n.Path}: {ST.gameExePath}");
            }
        }
        private void DataGrid_ModsShowList_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid grid && GroupBox_ModInfo.IsMouseOver == false && DataGrid_ModsShowList.IsMouseOver == false)
                ClearDataGridSelected();
        }

        private void Button_ImportEnabledListFromSave_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Title = I18n.ImportEnabledModsListFromSave,
                Filter = $"Xml {I18n.File}|*.xml"
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
                        STLog.Instance.WriteLine($"{I18n.FileError} {I18n.Path}: {filePath}\n{ex}", STLogLevel.WARN);
                        MessageBox.Show($"{I18n.FileError}\n{I18n.Path}: {filePath}\n{ex}");
                        return;
                    }
                    var result = MessageBox.Show(I18n.SelectImportMode, "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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
                            STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
                            err ??= $"{I18n.NotFoundMod}\n";
                            err += $"{id}\n";
                        }
                    }
                }
                else
                {
                    STLog.Instance.WriteLine($"{I18n.FileNotExist} {I18n.Path}: {filePath}", STLogLevel.WARN);
                    MessageBox.Show($"{I18n.FileNotExist}\n{I18n.Path}: {filePath}");
                }
                if (err != null)
                    MessageBox.Show(err);
            }
        }

        private void DataGrid_ModsShowList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is Array array)
            {
                STLog.Instance.WriteLine($"{I18n.ConfirmDragFiles} {I18n.Size}: {array.Length}");
                Dispatcher.BeginInvoke(() => ((MainWindow)Application.Current.MainWindow).IsEnabled = false);
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
                        if (File.Exists(path))
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                window.Label_Progress.Content = path;
                                window.Show();
                            });
                            DropFile(path);
                            Dispatcher.BeginInvoke(() =>
                            {
                                window.Label_Completed.Content = ++completed;
                                window.Label_Incomplete.Content = total - completed;
                            });
                        }
                        else
                        {
                            STLog.Instance.WriteLine($"{I18n.FileError} {I18n.Path}: {path}", STLogLevel.WARN);
                            MessageBox.Show($"{I18n.FileError}\n{I18n.Path}: {path}");
                        }
                    }
                    Dispatcher.BeginInvoke(() =>
                    {
                        window.Close();
                        ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
                    });
                    GC.Collect();
                }).Start();
            }
        }

        private void ComboBox_SearchType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchMods(TextBox_SearchMods.Text);
        }

        private void Button_OpenModDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ST.gameModsPath))
                ST.OpenFile(ST.gameModsPath);
            else
            {
                STLog.Instance.WriteLine($"{I18n.FolderNotExist} {I18n.Path}: {ST.gameModsPath}", STLogLevel.WARN);
                MessageBox.Show($"{I18n.FolderNotExist}\n{I18n.Path}: {ST.gameModsPath}");
            }
        }

        private void Button_OpenBackupDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(backupDirectory))
                ST.OpenFile(backupDirectory);
            else
            {
                STLog.Instance.WriteLine($"{I18n.FolderNotExist} {I18n.Path}: {backupDirectory}", STLogLevel.WARN);
                MessageBox.Show($"{I18n.FolderNotExist}\n{I18n.Path}: {backupDirectory}");
            }
        }

        private void Button_OpenSaveDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ST.gameSavePath))
                ST.OpenFile(ST.gameSavePath);
            else
            {
                STLog.Instance.WriteLine($"{I18n.FolderNotExist} {I18n.Path}: {ST.gameSavePath}", STLogLevel.WARN);
                MessageBox.Show($"{I18n.FolderNotExist}\n{I18n.Path}: {ST.gameSavePath}");
            }
        }

        private void Button_RandomMods_Click(object sender, RoutedEventArgs e)
        {
            if (TextBox_MinRandomSize.Text.Length == 0 || TextBox_MaxRandomSize.Text.Length == 0)
            {
                MessageBox.Show(I18n.RandomNumberCannotNull, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (nowSelectedListBoxItem is ListBoxItem item && allUserGroups.ContainsKey(item.ToolTip.ToString()!))
            {
                string group = item.ToolTip.ToString()!;
                int minSize = int.Parse(TextBox_MinRandomSize.Text);
                int maxSize = int.Parse(TextBox_MaxRandomSize.Text);
                int count = allUserGroups[group].Count;
                if (minSize < 0)
                {
                    MessageBox.Show(I18n.RandomNumberCannotLess0, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (maxSize > count)
                {
                    MessageBox.Show(I18n.RandomNumberCannotGreaterTotal);
                    return;
                }
                else if (minSize > maxSize)
                {
                    MessageBox.Show(I18n.MinRandomNumberCannotGreaterMaxRandomNumber);
                    return;
                }
                foreach (var info in allUserGroups[group])
                    ChangeModEnabled(info, false);
                int needSize = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray())).Next(minSize, maxSize + 1);
                HashSet<int> set = new();
                while (set.Count < needSize)
                    set.Add(new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray())).Next(0, count));
                foreach (int i in set)
                    ChangeModEnabled(allUserGroups[group].ElementAt(i));
                CheckEnabledModsDependencies();
                RefreshCountOfListBoxItems();
            }
        }

        private void Button_RefreshList_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
            STLog.Instance.WriteLine(I18n.RefreshComplete);
        }

        private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                if (sender is Control control && control.Parent is UIElement ui)
                    ui.RaiseEvent(eventArg);
            }
        }

        private void Grid_RightSide_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Grid_DataGrid.Margin = new Thickness(Grid_GroupMenu.ActualWidth, 0, Grid_RightSide.ActualWidth, 0);
        }
    }
}
