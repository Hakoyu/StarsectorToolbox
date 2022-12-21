using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using StarsectorTools.Libs;
using I18n = StarsectorTools.Langs.Tools.ModManager.ModManager_I18n;

namespace StarsectorTools.Tools.ModManager
{
    /// <summary>模组分组类型</summary>
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
        private const string modGroupFile = "ModGroup.toml";
        private const string userDataFile = "UserData.toml";
        private const string userGroupFile = "UserGroup.toml";
        private const string modInfoJsonFile = "mod_info.json";
        private const string backupModsDirectory = "BackUp\\Mods";
        private const string backupDirectory = "Backup";
        private const string strEnabledMods = "enabledMods";
        private const string strAll = "All";
        private const string strId = "Id";
        private const string strIcon = "Icon";
        private const string strMods = "Mods";
        private const string strUserCustomData = "UserCustomData";
        private const string strUserDescription = "UserDescription";
        private const string strName = "Name";
        private const string strAuthor = "Author";

        /// <summary>记录了模组类型的嵌入资源链接</summary>
        private static readonly Uri modGroupUri = new("/Resources/ModGroup.toml", UriKind.Relative);

        /// <summary>模组分组列表的展开状态</summary>
        private bool isGroupMenuOpen = false;

        /// <summary>模组详情的展开状态</summary>
        private bool isShowModDetails = false;

        /// <summary>当前选择的模组ID</summary>
        private string? nowSelectedModId = null;

        /// <summary>当前选择的分组名称</summary>
        private string nowGroupName = ModGroupType.All;

        /// <summary>提醒保存配置的动画线程</summary>
        private Thread remindSaveThread = null!;

        /// <summary>当前选择的列表项</summary>
        private ListBoxItem nowSelectedListBoxItem = null!;

        /// <summary>已启用的模组ID</summary>
        private HashSet<string> allEnabledModsId = new();
        /// <summary>已启用的模组ID</summary>
        public static HashSet<string> AllEnabledModsId { get; private set; } = null!;

        /// <summary>已收藏的模组ID</summary>
        private HashSet<string> allCollectedModsId = new();
        /// <summary>已收藏的模组ID</summary>
        public static HashSet<string> AllCollectedModsId { get; private set; } = null!;

        /// <summary>
        /// <para>全部模组信息</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 模组信息</para>
        /// </summary>
        private Dictionary<string, ModInfo> allModsInfo = new();
        /// <summary>
        /// <para>全部模组信息</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 模组信息</para>
        /// </summary>
        public static Dictionary<string, ModInfo> AllModsInfo { get; private set; } = null!;
        /// <summary>
        /// <para>全部分组列表项</para>
        /// <para><see langword="Key"/>: 列表项Tag或ModGroupType</para>
        /// <para><see langword="Value"/>: 列表项</para>
        /// </summary>
        private Dictionary<string, ListBoxItem> allListBoxItems = new();

        /// <summary>
        /// <para>全部模组显示信息</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 模组显示信息</para>
        /// </summary>
        private Dictionary<string, ModShowInfo> allModsShowInfo = new();

        /// <summary>
        /// <para>全部模组所在的类型分组</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 所在分组</para>
        /// </summary>
        private Dictionary<string, string> allModsTypeGroup = new();

        /// <summary>
        /// <para>全部用户分组</para>
        /// <para><see langword="Key"/>: 分组名称</para>
        /// <para><see langword="Value"/>: 包含的模组</para>
        /// </summary>
        private Dictionary<string, HashSet<string>> allUserGroups = new();
        /// <summary>
        /// <para>全部用户分组</para>
        /// <para><see langword="Key"/>: 分组名称</para>
        /// <para><see langword="Value"/>: 包含的模组</para>
        /// </summary>
        public static Dictionary<string, HashSet<string>> AllUserGroups { get; private set; } = null!;

        /// <summary>
        /// <para>全部分组包含的模组显示信息列表</para>
        /// <para><see langword="Key"/>: 分组名称</para>
        /// <para><see langword="Value"/>: 包含的模组显示信息的列表</para>
        /// </summary>
        private Dictionary<string, ObservableCollection<ModShowInfo>> allModShowInfoGroups = new();

        /// <summary>模组显示信息</summary>
        public partial class ModShowInfo : ObservableObject
        {
            /// <summary>ID</summary>
            public string Id { get; set; } = null!;

            /// <summary>名称</summary>
            public string Name { get; set; } = null!;

            /// <summary>作者</summary>
            public string Author { get; set; } = null!;

            /// <summary>是否启用</summary>
            [ObservableProperty]
            private bool isEnabled = false;

            /// <summary>收藏状态</summary>
            [ObservableProperty]
            private bool isCollected = false;

            /// <summary>模组版本</summary>
            public string Version { get; set; } = null!;

            /// <summary>模组支持的游戏版本</summary>
            public string GameVersion { get; set; } = null!;

            /// <summary>模组支持的游戏版本是否与当前游戏版本一至</summary>
            public bool IsSameToGameVersion { get; set; } = false;

            /// <summary>是否为功能性模组</summary>
            public bool IsUtility { get; set; } = false;

            /// <summary>图标路径</summary>
            //public string IconPath { get; set; } = string.Empty;
            /// <summary>图标资源</summary>
            public BitmapImage? ImageSource { get; set; } = null!;

            /// <summary>前置模组</summary>
            [ObservableProperty]
            private string? dependencies;

            /// <summary>前置模组列表</summary>
            public List<string>? DependenciesList;

            /// <summary>显示启用前置按钮的行高</summary>
            [ObservableProperty]
            private bool missDependencies;

            /// <summary>用户描述</summary>
            [ObservableProperty]
            private string userDescription = string.Empty;

            /// <summary>右键菜单</summary>
            [ObservableProperty]
            private ContextMenu contextMenu = null!;
        }
        public ModManager()
        {
            InitializeComponent();
            InitializeData();
            STLog.Instance.WriteLine(I18n.InitialisationComplete);
            //DataContext = viewModel = new(allModsShowInfo.Values);
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
                ST.OpenFile(button.Content.ToString()!);
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
                    STLog.Instance.WriteLine(err, STLogLevel.WARN);
                    ST.ShowMessageBox(err, MessageBoxImage.Warning);
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
                    if (name == ModGroupType.Collected || name == strUserCustomData)
                        ST.ShowMessageBox(string.Format(I18n.UserGroupCannotNamed, ModGroupType.Collected, strUserCustomData));
                    else
                    {
                        AddUserGroup(window.TextBox_Icon.Text, window.TextBox_Name.Text);
                        RefreshModsContextMenu();
                        RefreshCountOfListBoxItems();
                        window.Close();
                    }
                }
                else
                    ST.ShowMessageBox(I18n.AddUserNamingFailed);
            };
            window.Button_Cancel.Click += (s, e) => window.Close();
            window.ShowDialog();
        }

        private void Button_GameStart_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(ST.gameExeFile))
            {
                Process process = new();
                process.StartInfo.FileName = "cmd";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardInput = true;
                if (process.Start())
                {
                    process.StandardInput.WriteLine($"cd /d {ST.gameDirectory}");
                    process.StandardInput.WriteLine($"starsector.exe");
                    process.Close();
                    SaveAllData();
                    ResetRemindSaveThread();
                }
            }
            else
            {
                STLog.Instance.WriteLine($"{I18n.NotFoundFile}\n {I18n.Path}: {ST.gameExeFile}", STLogLevel.WARN);
                ST.ShowMessageBox($"{I18n.NotFoundFile}\n {I18n.Path}: {ST.gameExeFile}", MessageBoxImage.Warning);
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
                        STLog.Instance.WriteLine($"{I18n.FileError} {I18n.Path}: {filePath}\n", ex);
                        ST.ShowMessageBox($"{I18n.FileError}\n{I18n.Path}: {filePath}\n", MessageBoxImage.Error);
                        return;
                    }
                    var result = ST.ShowMessageBox(I18n.SelectImportMode, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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
                    ST.ShowMessageBox($"{I18n.FileNotExist}\n{I18n.Path}: {filePath}");
                }
                if (err != null)
                {
                    STLog.Instance.WriteLine(err, STLogLevel.WARN);
                    ST.ShowMessageBox(err, MessageBoxImage.Warning);
                }
            }
        }

        private void DataGrid_ModsShowList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is Array array)
            {
                STLog.Instance.WriteLine($"{I18n.ConfirmDragFiles} {I18n.Size}: {array.Length}");
                ST.SetMainWindowBlurEffect();
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
                                window.ShowDialog();
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
                            ST.ShowMessageBox($"{I18n.FileError}\n{I18n.Path}: {path}", MessageBoxImage.Warning);
                        }
                    }
                    Dispatcher.BeginInvoke(() =>
                    {
                        window.Close();
                        ST.RemoveMainWIndowBlurEffect();
                    });
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
            if (Directory.Exists(ST.gameModsDirectory))
                ST.OpenFile(ST.gameModsDirectory);
            else
            {
                STLog.Instance.WriteLine($"{I18n.FolderNotExist} {I18n.Path}: {ST.gameModsDirectory}", STLogLevel.WARN);
                ST.ShowMessageBox($"{I18n.FolderNotExist}\n{I18n.Path}: {ST.gameModsDirectory}", MessageBoxImage.Warning);
            }
        }

        private void Button_OpenBackupDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(backupDirectory))
                ST.OpenFile(backupDirectory);
            else
            {
                STLog.Instance.WriteLine($"{I18n.FolderNotExist} {I18n.Path}: {backupDirectory}", STLogLevel.WARN);
                ST.ShowMessageBox($"{I18n.FolderNotExist}\n{I18n.Path}: {backupDirectory}", MessageBoxImage.Warning);
            }
        }

        private void Button_OpenSaveDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(ST.gameSaveDirectory))
                ST.OpenFile(ST.gameSaveDirectory);
            else
            {
                STLog.Instance.WriteLine($"{I18n.FolderNotExist} {I18n.Path}: {ST.gameSaveDirectory}", STLogLevel.WARN);
                ST.ShowMessageBox($"{I18n.FolderNotExist}\n{I18n.Path}: {ST.gameSaveDirectory}", MessageBoxImage.Warning);
            }
        }

        private void Button_RandomMods_Click(object sender, RoutedEventArgs e)
        {
            if (TextBox_MinRandomSize.Text.Length == 0 || TextBox_MaxRandomSize.Text.Length == 0)
            {
                ST.ShowMessageBox(I18n.RandomNumberCannotNull, MessageBoxImage.Warning);
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
                    ST.ShowMessageBox(I18n.RandomNumberCannotLess0, MessageBoxImage.Warning);
                    return;
                }
                else if (maxSize > count)
                {
                    ST.ShowMessageBox(I18n.RandomNumberCannotGreaterTotal, MessageBoxImage.Warning);
                    return;
                }
                else if (minSize > maxSize)
                {
                    ST.ShowMessageBox(I18n.MinRandomNumberCannotGreaterMaxRandomNumber, MessageBoxImage.Warning);
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
    }
}