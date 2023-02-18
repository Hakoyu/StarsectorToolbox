using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Panuon.WPF.UI;
using StarsectorTools.Libs.GameInfo;
using HKW.Extension;
using StarsectorTools.Libs.Utils;
using I18nRes = StarsectorTools.Langs.Pages.ModManager.ModManagerPageI18nRes;
using HKW.Libs.TomlParse;

namespace StarsectorTools.Pages.ModManager
{
    public partial class ModManagerPage
    {
        private static string userDataFile = $"{ST.CoreDirectory}\\UserData.toml";
        private static string userGroupFile = $"{ST.CoreDirectory}\\UserGroup.toml";
        private static string backupDirectory = $"{ST.CoreDirectory}\\Backup";
        private static string backupModsDirectory = $"{backupDirectory}\\Mods";
        private const string modInfoFile = "mod_info.json";
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
        private static readonly Uri modTypeGroupUri = new("/Resources/ModTypeGroup.toml", UriKind.Relative);

        /// <summary>模组分组列表的展开状态</summary>
        private bool expandGroupMenu = false;

        /// <summary>模组详情的展开状态</summary>
        private bool isShowModDetails = false;

        /// <summary>当前选择的模组ID</summary>
        private string? nowSelectedModId = null;

        /// <summary>当前选择的分组名称</summary>
        private string nowGroupName = ModTypeGroup.All;

        /// <summary>提醒保存配置的动画线程</summary>
        private Thread remindSaveThread = null!;

        /// <summary>当前选择的列表项</summary>
        private ListBoxItem nowSelectedListBoxItem = null!;

        /// <summary>已启用的模组ID</summary>
        private HashSet<string> allEnabledModsId = new();

        /// <summary>已收藏的模组ID</summary>
        private HashSet<string> allCollectedModsId = new();

        /// <summary>
        /// <para>全部模组信息</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 模组信息</para>
        /// </summary>
        private Dictionary<string, ModInfo> allModInfos = new();

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
        /// <para>全部分组包含的模组显示信息列表</para>
        /// <para><see langword="Key"/>: 分组名称</para>
        /// <para><see langword="Value"/>: 包含的模组显示信息的列表</para>
        /// </summary>
        private Dictionary<string, ObservableCollection<ModShowInfo>> allModShowInfoGroups = new();


        //private class GroupData : IEnumerable<ModShowInfo>, IEnumerable
        //{
        //    public string Name { get; set; }
        //    private HashSet<ModShowInfo> modShowInfos;
        //    public ReadOnlySet<ModShowInfo> ModShowInfos { get; private set; }
        //    public GroupData(string name, IEnumerable<ModShowInfo> modShowInfos)
        //    {
        //        Name = name;
        //        ModShowInfos = new(this.modShowInfos = new(modShowInfos));
        //    }

        //    public new bool Add()
        //    {
        //        return false;
        //    }

        //    public IEnumerator<ModShowInfo> GetEnumerator() => ModShowInfos.GetEnumerator();

        //    IEnumerator IEnumerable.GetEnumerator() => ModShowInfos.GetEnumerator();
        //}
        //private ViewModel viewModel;
        //public partial class ViewModel : ObservableObject
        //{
        //    [ObservableProperty]
        //    ICollectionView? collectionView;
        //    HashSet<ModShowInfo> modShowInfos;
        //    [ObservableProperty]
        //    string? filterText;
        //    public string filterType = strName;
        //    partial void OnFilterTextChanged(string? value) => CollectionView?.Refresh();
        //    public ViewModel(IEnumerable<ModShowInfo> modShowInfos)
        //    {
        //        this.modShowInfos = new(modShowInfos);
        //        ChangeCollectionView(modShowInfos);
        //    }
        //    public void ChangeCollectionView(IEnumerable<ModShowInfo> modShowInfos)
        //    {
        //        CollectionView = CollectionViewSource.GetDefaultView(modShowInfos);
        //        CollectionView.Filter = (o) =>
        //        {
        //            if (string.IsNullOrWhiteSpace(filterText))
        //                return true;
        //            if (o is not ModShowInfo showInfo)
        //                return true;
        //            return filterType switch
        //            {
        //                strName => showInfo.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase),
        //                strId => showInfo.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase),
        //                strAuthor => showInfo.Author.Contains(filterText, StringComparison.OrdinalIgnoreCase),
        //                strUserDescription => showInfo.UserDescription.Contains(filterText, StringComparison.OrdinalIgnoreCase),
        //                _ => throw new NotImplementedException()
        //            };
        //        };
        //    }
        //}

        /// <summary>模组显示信息</summary>
        internal partial class ModShowInfo : ObservableObject
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

            /// <summary>图标资源</summary>
            public BitmapImage? ImageSource { get; set; } = null!;

            /// <summary>前置模组</summary>
            [ObservableProperty]
            private string? dependencies;

            /// <summary>前置模组列表</summary>
            public HashSet<string>? DependenciesSet;

            /// <summary>展开启用前置的按钮</summary>
            [ObservableProperty]
            private bool missDependencies;

            /// <summary>用户描述</summary>
            [ObservableProperty]
            private string userDescription = string.Empty;

            /// <summary>右键菜单</summary>
            [ObservableProperty]
            private ContextMenu contextMenu = null!;
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            ResetRemindSaveThread();
        }

        /// <summary>
        /// 保存
        /// </summary>
        public void Save()
        {
            SaveAllData();
            ResetRemindSaveThread();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            remindSaveThread = new(RemindSave);
            ModsInfo.AllModsInfo = new(allModInfos = new());
            ModsInfo.AllEnabledModsId = new(allEnabledModsId = new());
            ModsInfo.AllCollectedModsId = new(allCollectedModsId = new());
            ModsInfo.AllUserGroups = (allUserGroups = new()).AsReadOnly<string, HashSet<string>, IReadOnlySet<string>>();
            //ModsInfo.AllUserGroups = (allUserGroups = new())
            allListBoxItems = new();
            allModsShowInfo = new();
            allModsTypeGroup = new();
            allModShowInfoGroups = new()
            {
                [ModTypeGroup.All] = new(),
                [ModTypeGroup.Enabled] = new(),
                [ModTypeGroup.Disabled] = new(),
                [ModTypeGroup.Libraries] = new(),
                [ModTypeGroup.MegaMods] = new(),
                [ModTypeGroup.FactionMods] = new(),
                [ModTypeGroup.ContentExtensions] = new(),
                [ModTypeGroup.UtilityMods] = new(),
                [ModTypeGroup.MiscellaneousMods] = new(),
                [ModTypeGroup.BeautifyMods] = new(),
                [ModTypeGroup.UnknownMods] = new(),
                [ModTypeGroup.Collected] = new(),
            };
            GetAllModsInfo();
            GetAllListBoxItems();
            GetTypeGroup();
            GetAllModsShowInfo();
            CheckEnabledMods();
            CheckEnabledModsDependencies();
            CheckUserData();
            RefreshModsContextMenu();
            RefreshCountOfListBoxItems();
            ResetRemindSaveThread();
            GC.Collect();
        }

        private void GetAllModsInfo()
        {
            int errSize = 0;
            DirectoryInfo dirInfo = new(GameInfo.ModsDirectory);
            string err = string.Empty;
            foreach (var dir in dirInfo.GetDirectories())
            {
                if (ModInfo.Parse($"{dir.FullName}\\{modInfoFile}") is ModInfo info)
                {
                    allModInfos.Add(info.Id, info);
                    STLog.WriteLine($"{I18nRes.ModAddSuccess}: {dir.FullName}", STLogLevel.DEBUG);
                }
                else
                {
                    err += $"{dir.FullName}\n";
                    errSize++;
                }
            }
            STLog.WriteLine(I18nRes.ModAddCompleted, STLogLevel.INFO, allModInfos.Count, errSize);
            if (!string.IsNullOrWhiteSpace(err))
                Utils.ShowMessageBox($"{I18nRes.ModAddFailed} {I18nRes.Size}: {errSize}\n{err}", STMessageBoxIcon.Warning);
        }

        private void CheckEnabledMods()
        {
            if (Utils.FileExists(GameInfo.EnabledModsJsonFile))
                GetEnabledMods(GameInfo.EnabledModsJsonFile);
            else
                SaveEnabledMods(GameInfo.EnabledModsJsonFile);
        }

        private void ImportMode()
        {
            var result = Utils.ShowMessageBox(I18nRes.SelectImportMode, MessageBoxButton.YesNoCancel, STMessageBoxIcon.Question);
            if (result == MessageBoxResult.Yes)
                ClearAllEnabledMods();
            else if (result == MessageBoxResult.Cancel)
                return;
        }

        private void GetEnabledMods(string filePath, bool importMode = false)
        {
            try
            {
                string err = string.Empty;
                int errSize = 0;
                JsonNode enabledModsJson = Utils.JsonParse(filePath)!;
                if (enabledModsJson == null)
                    throw new ArgumentNullException();
                if (enabledModsJson.AsObject().Count != 1 || enabledModsJson.AsObject().ElementAt(0).Key != strEnabledMods)
                    throw new ArgumentNullException();
                if (importMode)
                    ImportMode();
                JsonArray enabledModsJsonArray = enabledModsJson[strEnabledMods]!.AsArray();
                STLog.WriteLine($"{I18nRes.LoadEnabledModsFile} {I18nRes.Path}: {filePath}");
                foreach (var modId in enabledModsJsonArray)
                {
                    var id = modId!.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    if (allModInfos.ContainsKey(id))
                    {
                        STLog.WriteLine($"{I18nRes.EnableMod} {id}", STLogLevel.DEBUG);
                        ChangeModEnabled(id, true);
                    }
                    else
                    {
                        STLog.WriteLine($"{I18nRes.NotFoundMod} {id}", STLogLevel.WARN);
                        err += $"{id}\n";
                        errSize++;
                    }
                }
                STLog.WriteLine($"{I18nRes.EnableMod} {I18nRes.Size}: {allEnabledModsId.Count}");
                if (!string.IsNullOrWhiteSpace(err))
                    Utils.ShowMessageBox($"{I18nRes.NotFoundMod} {I18nRes.Size}: {errSize}\n{err}", STMessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18nRes.LoadError} {I18nRes.Path}: {filePath}", ex);
                Utils.ShowMessageBox($"{I18nRes.LoadError}\n{I18nRes.Path}: {filePath}", STMessageBoxIcon.Error);
            }
        }

        private void CheckUserData()
        {
            if (Utils.FileExists(userDataFile))
                GetUserData(userDataFile);
            else
                SaveUserData(userDataFile);
            if (Utils.FileExists(userGroupFile))
                GetUserGroup(userGroupFile);
            else
                SaveUserGroup(userGroupFile);
        }

        private void GetUserGroup(string filePath)
        {
            STLog.WriteLine($"{I18nRes.LoadUserGroup} {I18nRes.Path}: {filePath}");
            try
            {
                string err = string.Empty;
                int errSize = 0;
                TomlTable toml = TOML.Parse(filePath);
                foreach (var kv in toml)
                {
                    if (kv.Key == ModTypeGroup.Collected || kv.Key == strUserCustomData)
                        continue;
                    string group = kv.Key;
                    if (!allUserGroups.ContainsKey(group))
                    {
                        AddUserGroup(kv.Value[strIcon]!, group);
                        foreach (string id in kv.Value[strMods].AsTomlArray)
                        {
                            if (string.IsNullOrWhiteSpace(id))
                                continue;
                            if (allModsShowInfo.ContainsKey(id))
                            {
                                if (allUserGroups[group].Add(id))
                                    allModShowInfoGroups[group].Add(allModsShowInfo[id]);
                            }
                            else
                            {
                                STLog.WriteLine($"{I18nRes.NotFoundMod} {id}", STLogLevel.WARN);
                                err += $"{id}\n";
                                errSize++;
                            }
                        }
                    }
                    else
                    {
                        STLog.WriteLine($"{I18nRes.DuplicateUserGroupName} {group}");
                        err ??= $"{I18nRes.DuplicateUserGroupName} {group}";
                    }
                }
                if (!string.IsNullOrWhiteSpace(err))
                    Utils.ShowMessageBox($"{I18nRes.NotFoundMod} {I18nRes.Size}: {errSize}\n{err}", STMessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18nRes.FileError} {filePath}", ex);
                Utils.ShowMessageBox($"{I18nRes.FileError} {filePath}", STMessageBoxIcon.Error);
            }
        }

        private void GetUserData(string filePath)
        {
            STLog.WriteLine($"{I18nRes.LoadUserData} {I18nRes.Path}: {filePath}");
            try
            {
                string err = string.Empty;
                int errSize = 0;
                TomlTable toml = TOML.Parse(filePath);
                STLog.WriteLine(I18nRes.LoadCollectedList);
                foreach (string id in toml[ModTypeGroup.Collected].AsTomlArray)
                {
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    if (allModsShowInfo.ContainsKey(id))
                    {
                        ChangeModCollected(id, true);
                    }
                    else
                    {
                        STLog.WriteLine($"{I18nRes.NotFoundMod} {id}", STLogLevel.WARN);
                        err += $"{id}\n";
                    }
                }
                if (!string.IsNullOrWhiteSpace(err))
                    Utils.ShowMessageBox($"{I18nRes.LoadCollectedList} {I18nRes.NotFoundMod} {I18nRes.Size}: {errSize}\n{err}", STMessageBoxIcon.Warning);
                err = string.Empty;
                errSize = 0;
                STLog.WriteLine(I18nRes.LoadUserCustomData);
                foreach (var dict in toml[strUserCustomData].AsTomlArray)
                {
                    var id = dict[strId].AsString;
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    if (allModsShowInfo.ContainsKey(id))
                    {
                        var info = allModsShowInfo[id];
                        info.UserDescription = dict[strUserDescription];
                    }
                    else
                    {
                        STLog.WriteLine($"{I18nRes.NotFoundMod} {id}", STLogLevel.WARN);
                        err ??= $"{I18nRes.NotFoundMod}\n";
                        err += $"{id}\n";
                    }
                }
                if (!string.IsNullOrWhiteSpace(err))
                    Utils.ShowMessageBox($"{I18nRes.LoadUserCustomData} {I18nRes.NotFoundMod} {I18nRes.Size}: {errSize}\n{err}", STMessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18nRes.UserDataLoadError} {I18nRes.Path}: {filePath}", ex);
                Utils.ShowMessageBox($"{I18nRes.UserDataLoadError}\n{I18nRes.Path}: {filePath}", STMessageBoxIcon.Error);
            }
        }

        private void GetAllListBoxItems()
        {
            //foreach (ListBoxItem item in ListBox_ModsGroupMenu.Items)
            //{
            //    if (item.Content is string str)
            //        allListBoxItems.Add(item.Tag.ToString()!, item);
            //    else if (item.Content is Expander expander && expander.Content is ListBox listBox)
            //        foreach (ListBoxItem item1 in listBox.Items)
            //            allListBoxItems.Add(item1.Tag.ToString()!, item1);
            //}
            //STLog.WriteLine(I18nRes.ListBoxItemsRetrievalCompleted);
        }

        private void GetTypeGroup()
        {
            using StreamReader sr = new(Application.GetResourceStream(modTypeGroupUri).Stream);
            TomlTable toml = TOML.Parse(sr);
            foreach (var kv in toml)
                foreach (string id in kv.Value.AsTomlArray)
                    allModsTypeGroup.Add(id, kv.Key);
            STLog.WriteLine(I18nRes.TypeGroupRetrievalCompleted);
        }

        private string CheckTypeGroup(string id)
        {
            return allModsTypeGroup.ContainsKey(id) ? allModsTypeGroup[id] : ModTypeGroup.UnknownMods;
        }

        private void GetAllModsShowInfo()
        {
            foreach (var modInfo in allModInfos.Values)
                AddModShowInfo(modInfo, false);
            STLog.WriteLine($"{I18nRes.ModShowInfoSetSuccess} {I18nRes.Size}: {allModInfos.Count}");
            //ListBox_ModsGroupMenu.SelectedIndex = 0;
        }

        private void RefreshDataGrid()
        {
            string text = Dispatcher.Invoke(() => TextBox_SearchMods.Text);
            string type = Dispatcher.Invoke(() => ((ComboBoxItem)ComboBox_SearchType.SelectedItem).Tag.ToString()!);
            if (text.Length > 0)
            {
                ShowDataGridItems(GetSearchModsShowInfo(text, type));
                STLog.WriteLine($"{I18nRes.SearchMod} {text}");
            }
            else
            {
                ShowDataGridItems(allModShowInfoGroups[nowGroupName]);
                STLog.WriteLine($"{I18nRes.ShowGroup} {nowGroupName}");
            }
        }

        private void ShowDataGridItems(IEnumerable<ModShowInfo> infos)
        {
            DataGrid_ModsShowList.Items.Clear();
            foreach (var info in infos)
                Dispatcher.InvokeAsync(() => DataGrid_ModsShowList.Items.Add(info), DispatcherPriority.Background);
        }

        private ObservableCollection<ModShowInfo> GetSearchModsShowInfo(string text, string type) =>
        new ObservableCollection<ModShowInfo>(type switch
        {
            strName => allModShowInfoGroups[nowGroupName].Where(i => i.Name.Contains(text, StringComparison.OrdinalIgnoreCase)),
            strId => allModShowInfoGroups[nowGroupName].Where(i => i.Id.Contains(text, StringComparison.OrdinalIgnoreCase)),
            strAuthor => allModShowInfoGroups[nowGroupName].Where(i => i.Author.Contains(text, StringComparison.OrdinalIgnoreCase)),
            strUserDescription => allModShowInfoGroups[nowGroupName].Where(i => i.UserDescription.Contains(text, StringComparison.OrdinalIgnoreCase)),
            _ => null!
        });

        private ModShowInfo CreateModShowInfo(ModInfo info)
        {
            return new ModShowInfo()
            {
                IsCollected = allCollectedModsId.Contains(info.Id),
                IsEnabled = allEnabledModsId.Contains(info.Id),
                IsUtility = info.IsUtility,
                Name = info.Name,
                Id = info.Id,
                Author = info.Author.Trim(),
                Version = info.Version,
                GameVersion = info.GameVersion,
                IsSameToGameVersion = info.GameVersion == GameInfo.Version,
                MissDependencies = false,
                DependenciesSet = info.Dependencies is not null ? new(info.Dependencies.Select(i => i.Id)) : null!,
                ImageSource = GetIcon($"{info.ModDirectory}\\icon.ico"),
            };
            BitmapImage? GetIcon(string filePath)
            {
                if (!Utils.FileExists(filePath, false))
                    return null;
                try
                {
                    using Stream stream = new StreamReader(filePath).BaseStream;
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes);
                    BitmapImage bitmap = new();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(bytes);
                    bitmap.EndInit();
                    return bitmap;
                }
                catch (Exception ex)
                {
                    STLog.WriteLine($"{I18nRes.IconLoadError} {I18nRes.Path}: {filePath}", ex);
                    return null;
                }
            }
        }

        private void RefreshCountOfListBoxItems()
        {
            foreach (var item in allListBoxItems.Values)
            {
                int size = allModShowInfoGroups[item.Tag.ToString()!].Count;
                item.Content = $"{item.ToolTip} ({size})";
                STLog.WriteLine($"{I18nRes.ModCountInGroupRefresh} {item.Content}", STLogLevel.DEBUG);
            }
            STLog.WriteLine(I18nRes.ModCountInGroupRefreshCompleted);
        }

        private void RefreshModsContextMenu()
        {
            foreach (var showInfo in allModsShowInfo.Values)
                showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            STLog.WriteLine($"{I18nRes.ContextMenuRefreshCompleted} {I18nRes.Size}: {allModsShowInfo.Values.Count}");
        }

        private ContextMenu CreateModShowContextMenu(ModShowInfo showInfo)
        {
            STLog.WriteLine($"{showInfo.Id} {I18nRes.AddContextMenu}", STLogLevel.DEBUG);
            ContextMenu contextMenu = new();
            // 标记菜单项是否被创建
            contextMenu.Tag = false;
            // 被点击时才加载菜单,可以降低内存占用
            contextMenu.Loaded += (s, e) =>
            {
                if (contextMenu.Tag is true)
                    return;
                contextMenu.Style = (Style)Application.Current.Resources["ContextMenuBaseStyle"];
                // 启用或禁用
                MenuItem menuItem = new();
                menuItem.Header = showInfo.IsEnabled ? I18nRes.DisableSelectedMods : I18nRes.EnabledSelectedMods;
                menuItem.Click += (s, e) => ChangeSelectedModsEnabled();
                contextMenu.Items.Add(menuItem);
                STLog.WriteLine($"{I18nRes.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 收藏或取消收藏
                menuItem = new();
                menuItem.Header = showInfo.IsCollected ? I18nRes.UncollectSelectedMods : I18nRes.CollectSelectedMods;
                menuItem.Click += (s, e) => ChangeSelectedModsCollected();
                contextMenu.Items.Add(menuItem);
                STLog.WriteLine($"{I18nRes.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 打开模组文件夹
                menuItem = new();
                menuItem.Header = I18nRes.OpenModDirectory;
                menuItem.Click += (s, e) =>
                {
                    STLog.WriteLine($"{I18nRes.OpenModDirectory} {I18nRes.Path}: {allModInfos[showInfo.Id].ModDirectory}");
                    Utils.OpenLink(allModInfos[showInfo.Id].ModDirectory);
                };
                contextMenu.Items.Add(menuItem);
                STLog.WriteLine($"{I18nRes.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 删除模组至回收站
                menuItem = new();
                menuItem.Header = I18nRes.DeleteMod;
                menuItem.Click += (s, e) =>
                {
                    string path = allModInfos[showInfo.Id].ModDirectory;
                    if (Utils.ShowMessageBox($"{I18nRes.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18nRes.Path}: {path}\n", MessageBoxButton.YesNo, STMessageBoxIcon.Warning) == MessageBoxResult.Yes)
                    {
                        STLog.WriteLine($"{I18nRes.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18nRes.Path}: {path}\n");
                        RemoveMod(showInfo.Id);
                        RefreshDataGrid();
                        CloseModDetails();
                        Utils.DeleteDirToRecycleBin(path);
                        RefreshCountOfListBoxItems();
                        StartRemindSaveThread();
                    }
                };
                contextMenu.Items.Add(menuItem);
                // 添加至用户分组
                if (allUserGroups.Count > 0)
                {
                    menuItem = new();
                    menuItem.Header = I18nRes.AddModToUserGroup;
                    foreach (var group in allUserGroups.Keys)
                    {
                        if (!allUserGroups[group].Contains(showInfo.Id))
                        {
                            MenuItem groupItem = new();
                            groupItem.Header = group;
                            groupItem.Style = (Style)Application.Current.Resources["MenuItemBaseStyle"];
                            groupItem.Click += (s, e) =>
                            {
                                ChangeSelectedModsInUserGroup(group, true);
                            };
                            menuItem.Items.Add(groupItem);
                        }
                    }
                    if (menuItem.Items.Count > 0)
                    {
                        contextMenu.Items.Add(menuItem);
                        STLog.WriteLine($"{I18nRes.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                    }
                }
                // 从用户分组中删除
                var groupWithMod = allUserGroups.Where(g => g.Value.Contains(showInfo.Id));
                if (groupWithMod.Count() > 0)
                {
                    menuItem = new();
                    menuItem.Header = I18nRes.RemoveFromUserGroup;
                    foreach (var group in groupWithMod)
                    {
                        MenuItem groupItem = new();
                        groupItem.Header = group.Key;
                        groupItem.Style = (Style)Application.Current.Resources["MenuItemBaseStyle"];
                        groupItem.Click += (s, e) =>
                        {
                            ChangeSelectedModsInUserGroup(group.Key, false);
                        };
                        menuItem.Items.Add(groupItem);
                    }
                    if (menuItem.Items.Count > 0)
                    {
                        contextMenu.Items.Add(menuItem);
                        STLog.WriteLine($"{I18nRes.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                    }
                }
                contextMenu.Tag = true;
            };
            return contextMenu;
        }

        private void ChangeSelectedModsInUserGroup(string group, bool isInGroup)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo showInfo = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ChangeModInUserGroup(group, showInfo.Id, isInGroup);
                // 如果已选择数量没有变化,则继续下一个选项
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModDetails();
            RefreshCountOfListBoxItems();
            StartRemindSaveThread();
        }

        private void ChangeModInUserGroup(string group, string id, bool isInGroup)
        {
            ModShowInfo showInfo = allModsShowInfo[id];
            if (isInGroup)
            {
                if (allUserGroups[group].Add(id))
                {
                    allModShowInfoGroups[group].Add(allModsShowInfo[id]);
                    STLog.WriteLine($"{id} {I18nRes.AddModToUserGroup} {group}", STLogLevel.DEBUG);
                }
            }
            else
            {
                if (allUserGroups[group].Remove(id))
                {
                    allModShowInfoGroups[group].Remove(allModsShowInfo[id]);
                    STLog.WriteLine($"{id} {I18nRes.RemoveFromUserGroup} {group}", STLogLevel.DEBUG);
                }
            }
            showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
        }

        private void ChangeSelectedModsEnabled(bool? enabled = null)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo showInfo = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ChangeModEnabled(showInfo.Id, enabled);
                // 如果已选择数量没有变化,则继续下一个选项
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModDetails();
            RefreshCountOfListBoxItems();
            CheckEnabledModsDependencies();
            StartRemindSaveThread();
        }

        private void ClearAllEnabledMods()
        {
            while (allEnabledModsId.Count > 0)
                ChangeModEnabled(allEnabledModsId.ElementAt(0), false);
            STLog.WriteLine(I18nRes.DisableAllEnabledMods);
        }

        private void ChangeModEnabled(string id, bool? enabled = null)
        {
            ModShowInfo showInfo = allModsShowInfo[id];
            showInfo.IsEnabled = (bool)(enabled is null ? !showInfo.IsEnabled : enabled);
            showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            if (showInfo.IsEnabled is true)
            {
                if (allEnabledModsId.Add(showInfo.Id))
                {
                    allModShowInfoGroups[ModTypeGroup.Enabled].Add(showInfo);
                    allModShowInfoGroups[ModTypeGroup.Disabled].Remove(showInfo);
                }
            }
            else
            {
                if (allEnabledModsId.Remove(showInfo.Id))
                {
                    allModShowInfoGroups[ModTypeGroup.Enabled].Remove(showInfo);
                    allModShowInfoGroups[ModTypeGroup.Disabled].Add(showInfo);
                    showInfo.MissDependencies = false;
                }
            }
            STLog.WriteLine($"{id} {I18nRes.ChangeEnabledStateTo} {showInfo.IsEnabled}", STLogLevel.DEBUG);
        }

        private void CheckEnabledModsDependencies()
        {
            foreach (var showInfo in allModShowInfoGroups[ModTypeGroup.Enabled])
            {
                if (showInfo.DependenciesSet != null)
                {
                    showInfo.Dependencies = string.Join(" , ", showInfo.DependenciesSet.Where(s => !allEnabledModsId.Contains(s)));
                    if (showInfo.Dependencies.Length > 0)
                    {
                        STLog.WriteLine($"{showInfo.Id} {I18nRes.NotEnableDependencies} {showInfo.Dependencies}");
                        showInfo.MissDependencies = true;
                    }
                    else
                        showInfo.MissDependencies = false;
                }
            }
        }

        private void ChangeSelectedModsCollected(bool? collected = null)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo showInfo = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ChangeModCollected(showInfo.Id, collected);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModDetails();
            RefreshCountOfListBoxItems();
            StartRemindSaveThread();
        }

        private void ChangeModCollected(string id, bool? collected = null)
        {
            ModShowInfo showInfo = allModsShowInfo[id];
            showInfo.IsCollected = (bool)(collected is null ? !showInfo.IsCollected : collected);
            showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            if (showInfo.IsCollected is true)
            {
                if (allCollectedModsId.Add(showInfo.Id))
                    allModShowInfoGroups[ModTypeGroup.Collected].Add(showInfo);
            }
            else
            {
                if (allCollectedModsId.Remove(showInfo.Id))
                    allModShowInfoGroups[ModTypeGroup.Collected].Remove(showInfo);
            }
            STLog.WriteLine($"{id} {I18nRes.ChangeCollectStateTo} {showInfo.IsCollected}", STLogLevel.DEBUG);
        }

        private void SaveAllData()
        {
            SaveEnabledMods(GameInfo.EnabledModsJsonFile);
            SaveUserData(userDataFile);
            SaveUserGroup(userGroupFile);
        }

        private void SaveEnabledMods(string filePath)
        {
            JsonObject jsonObject = new()
            {
                [strEnabledMods] = new JsonArray()
            };
            foreach (var mod in allEnabledModsId)
                jsonObject[strEnabledMods]!.AsArray().Add(mod);
            jsonObject.SaveTo(filePath);
            STLog.WriteLine($"{I18nRes.EnabledListSaveCompleted} {I18nRes.Path}: {filePath}");
        }

        private void SaveUserData(string filePath)
        {
            TomlTable toml = new()
            {
                [ModTypeGroup.Collected] = new TomlArray(),
                [strUserCustomData] = new TomlArray(),
            };
            foreach (var info in allModsShowInfo.Values)
            {
                if (info.IsCollected is true)
                    toml[ModTypeGroup.Collected].Add(info.Id);
                if (info.UserDescription!.Length > 0)
                {
                    toml[strUserCustomData].Add(new TomlTable()
                    {
                        [strId] = info.Id,
                        [strUserDescription] = info.UserDescription!.Length > 0 ? info.UserDescription : "",
                    });
                }
            }
            toml.SaveTo(filePath);
            STLog.WriteLine($"{I18nRes.SaveUserDataSuccess} {I18nRes.Path}: {filePath}");
        }

        private void SaveUserGroup(string filePath, string group = strAll)
        {
            TomlTable toml = new();
            if (group == strAll)
            {
                foreach (var groupData in allUserGroups)
                    Save(groupData.Key);
            }
            else
            {
                Save(group);
            }
            toml.SaveTo(filePath);
            STLog.WriteLine($"{I18nRes.UserGroupSaveCompleted} {I18nRes.Path}: {filePath}");
            void Save(string name)
            {
                var mods = allUserGroups[name];
                toml.Add(name, new TomlTable()
                {
                    [strIcon] = ((Emoji.Wpf.TextBlock)ListBoxItemHelper.GetIcon(allListBoxItems[name])).Text,
                    [strMods] = new TomlArray(),
                });
                foreach (var id in mods)
                    toml[name][strMods].Add(id);
            }
        }

        private void ChangeModInfoDetails(string id)
        {
            if (isShowModDetails)
            {
                if (nowSelectedModId != id)
                    SetModDetails(id);
                else
                    CloseModDetails();
            }
            else
            {
                ShowModDetails(id);
            }
        }

        private void ShowModDetails(string id)
        {
            if (nowSelectedModId == id)
                return;
            Grid_ModDetails.Visibility = Visibility.Visible;
            isShowModDetails = true;
            nowSelectedModId = id;
            SetModDetails(id);
        }

        private void CloseModDetails()
        {
            Grid_ModDetails.Visibility = Visibility.Hidden;
            isShowModDetails = false;
            nowSelectedModId = null;
            TextBox_UserDescription.Text = "";
            STLog.WriteLine($"{I18nRes.CloseDetails} {nowSelectedModId}", STLogLevel.DEBUG);
        }

        private void SetModDetails(string id)
        {
            var info = allModInfos[id];
            var showInfo = allModsShowInfo[id];
            if (showInfo.ImageSource != null)
                Image_ModImage.Source = showInfo.ImageSource;
            else
                Image_ModImage.Source = null;
            Label_ModName.Content = showInfo.Name;
            Label_ModId.Content = showInfo.Id;
            Label_ModVersion.Content = showInfo.Version;
            Label_GameVersion.Content = showInfo.GameVersion;
            Button_ModPath.Content = info.ModDirectory;
            TextBlock_ModAuthor.Text = showInfo.Author;
            if (info.Dependencies is not null)
            {
                GroupBox_ModDependencies.Visibility = Visibility.Visible;
                TextBlock_ModDependencies.Text = string.Join("\n", info.Dependencies.Select(i => $"{I18nRes.Name}: {i.Name} ID: {i.Id} " + (i.Version is not null ? $"{I18nRes.Version} {i.Version}" : ""))!);
            }
            else
                GroupBox_ModDependencies.Visibility = Visibility.Collapsed;
            TextBlock_ModDescription.Text = info.Description;
            TextBox_UserDescription.Text = showInfo.UserDescription!;
            STLog.WriteLine($"{I18nRes.ShowDetails} {id}", STLogLevel.DEBUG);
        }

        private void DropFile(string filePath)
        {
            string tempPath = "Temp";
            if (!Utils.UnArchiveFileToDir(filePath, tempPath))
            {
                Utils.ShowMessageBox($"{I18nRes.UnzipError}\n {I18nRes.Path}:{filePath}");
                return;
            }
            DirectoryInfo dirs = new(tempPath);
            var filesInfo = dirs.GetFiles(modInfoFile, SearchOption.AllDirectories);
            if (filesInfo.FirstOrDefault(defaultValue: null) is FileInfo fileInfo && fileInfo.FullName is string jsonPath)
            {
                string directoryName = Path.GetFileName(fileInfo.DirectoryName)!;
                if (ModInfo.Parse(Utils.JsonParse(jsonPath)!, $"{GameInfo.ModsDirectory}\\{directoryName}") is not ModInfo newModInfo)
                {
                    Utils.ShowMessageBox($"{I18nRes.FileError}\n{I18nRes.Path}: {filePath}");
                    return;
                }
                if (allModInfos.ContainsKey(newModInfo.Id))
                {
                    var originalModInfo = allModInfos[newModInfo.Id];
                    var result = Dispatcher.Invoke(() => Utils.ShowMessageBox($"{newModInfo.Id}\n{string.Format(I18nRes.DuplicateModExists, originalModInfo.Version, newModInfo.Version)}",
                                                                  MessageBoxButton.YesNoCancel,
                                                                  STMessageBoxIcon.Question,
                                                                  false));
                    if (result == MessageBoxResult.Yes)
                    {
                        Utils.CopyDirectory(originalModInfo.ModDirectory, $"{backupModsDirectory}\\{tempPath}");
                        string tempDirectory = $"{backupModsDirectory}\\{tempPath}";
                        Utils.ArchiveDirToDir(tempDirectory, backupModsDirectory, directoryName);
                        Directory.Delete(tempDirectory, true);
                        Directory.Delete(originalModInfo.ModDirectory, true);
                        Utils.CopyDirectory(Path.GetDirectoryName(jsonPath)!, GameInfo.ModsDirectory);
                        Dispatcher.InvokeAsync(() =>
                        {
                            RemoveMod(newModInfo.Id);
                            AddMod(newModInfo);
                            RefreshCountOfListBoxItems();
                            StartRemindSaveThread();
                        });
                        STLog.WriteLine($"{I18nRes.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}");
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        Utils.DeleteDirToRecycleBin(originalModInfo.ModDirectory);
                        Utils.CopyDirectory(Path.GetDirectoryName(jsonPath)!, GameInfo.ModsDirectory);
                        Dispatcher.InvokeAsync(() =>
                        {
                            RemoveMod(newModInfo.Id);
                            AddMod(newModInfo);
                            RefreshCountOfListBoxItems();
                            StartRemindSaveThread();
                        });
                        STLog.WriteLine($"{I18nRes.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}");
                    }
                }
                else
                {
                    Utils.CopyDirectory(Path.GetDirectoryName(jsonPath)!, GameInfo.ModsDirectory);
                    Dispatcher.InvokeAsync(() =>
                    {
                        AddMod(newModInfo);
                        RefreshCountOfListBoxItems();
                        StartRemindSaveThread();
                    });
                }
                RefreshDataGrid();
            }
            else
            {
                STLog.WriteLine($"{I18nRes.ZipFileError} {I18nRes.Path}: {filePath}");
                Utils.ShowMessageBox($"{I18nRes.ZipFileError}\n{I18nRes.Path}: {filePath}");
            }
            dirs.Delete(true);
        }

        private void RemoveMod(string id)
        {
            var modInfo = allModInfos[id];
            allModInfos.Remove(id);
            RemoveModShowInfo(id);
            STLog.WriteLine($"{I18nRes.RemoveMod} {id} {modInfo.Version}", STLogLevel.DEBUG);
        }

        private void RemoveModShowInfo(string id)
        {
            var modShowInfo = allModsShowInfo[id];
            // 从总分组中删除
            allModsShowInfo.Remove(id);
            allModShowInfoGroups[ModTypeGroup.All].Remove(modShowInfo);
            // 从类型分组中删除
            allModShowInfoGroups[CheckTypeGroup(id)].Remove(modShowInfo);
            // 从已启用或已禁用分组中删除
            if (modShowInfo.IsEnabled)
            {
                allEnabledModsId.Remove(id);
                allModShowInfoGroups[ModTypeGroup.Enabled].Remove(modShowInfo);
            }
            else
                allModShowInfoGroups[ModTypeGroup.Disabled].Remove(modShowInfo);
            // 从已收藏中删除
            if (modShowInfo.IsCollected)
            {
                allCollectedModsId.Remove(id);
                allModShowInfoGroups[ModTypeGroup.Collected].Remove(modShowInfo);
            }
            // 从用户分组中删除
            foreach (var userGroup in allUserGroups)
            {
                if (userGroup.Value.Contains(id))
                {
                    userGroup.Value.Remove(id);
                    allModShowInfoGroups[userGroup.Key].Remove(modShowInfo);
                }
            }
        }

        private void AddMod(ModInfo modInfo)
        {
            allModInfos.Add(modInfo.Id, modInfo);
            AddModShowInfo(modInfo);
            STLog.WriteLine($"{I18nRes.RemoveMod} {modInfo.Id} {modInfo.Version}", STLogLevel.DEBUG);
        }

        private void AddModShowInfo(ModInfo modInfo, bool createContextMenu = true)
        {
            if (allModsShowInfo.ContainsKey(modInfo.Id))
                return;
            ModShowInfo showInfo = CreateModShowInfo(modInfo);
            // 添加至总分组
            allModsShowInfo.Add(showInfo.Id, showInfo);
            allModShowInfoGroups[ModTypeGroup.All].Add(showInfo);
            // 添加至类型分组
            allModShowInfoGroups[CheckTypeGroup(modInfo.Id)].Add(showInfo);
            // 添加至已启用或已禁用分组
            if (showInfo.IsEnabled)
                allModShowInfoGroups[ModTypeGroup.Enabled].Add(showInfo);
            else
                allModShowInfoGroups[ModTypeGroup.Disabled].Add(showInfo);
            // 添加至已收藏分组
            if (showInfo.IsCollected)
                allModShowInfoGroups[ModTypeGroup.Collected].Add(showInfo);
            // 添加至用户分组
            foreach (var userGroup in allUserGroups)
            {
                if (userGroup.Value.Contains(modInfo.Id))
                {
                    userGroup.Value.Add(modInfo.Id);
                    allModShowInfoGroups[userGroup.Key].Add(showInfo);
                }
            }
            if (createContextMenu)
                showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            STLog.WriteLine($"{I18nRes.AddMod} {showInfo.Id} {showInfo.Version}", STLogLevel.DEBUG);
        }

        private void ClearDataGridSelected()
        {
            DataGrid_ModsShowList.UnselectAll();
        }

        private void AddUserGroup(string icon, string name)
        {
            ListBoxItem listBoxItem = new();
            // 调用全局资源需要写全
            listBoxItem.Style = (Style)Application.Current.Resources["ListBoxItemBaseStyle"];
            SetListBoxItemData(listBoxItem, name);
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenuBaseStyle"];
            // 重命名分组
            MenuItem menuItem = new();
            menuItem.Header = I18nRes.RenameUserGroup;
            menuItem.Click += (s, e) =>
            {
                RenameUserGroup((ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)s)));
            };
            contextMenu.Items.Add(menuItem);
            STLog.WriteLine($"{I18nRes.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
            // 删除分组
            menuItem = new();
            menuItem.Header = I18nRes.RemoveUserGroup;
            menuItem.Click += (s, e) =>
            {
                RemoveUserGroup((ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)s)));
            };
            contextMenu.Items.Add(menuItem);
            STLog.WriteLine($"{I18nRes.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);

            listBoxItem.ContextMenu = contextMenu;
            ListBoxItemHelper.SetIcon(listBoxItem, new Emoji.Wpf.TextBlock() { Text = icon });
            ListBox_UserGroup.Items.Add(listBoxItem);
            allUserGroups.Add(name, new());
            allListBoxItems.Add(name, listBoxItem);
            allModShowInfoGroups.Add(name, new());
            StartRemindSaveThread();
            ComboBox_ExportUserGroup.Items.Add(new ComboBoxItem() { Content = name, Tag = name, Style = (Style)Application.Current.Resources["ComboBoxItemBaseStyle"] });
            STLog.WriteLine($"{I18nRes.AddUserGroup} {icon} {name}");
        }

        private void RemoveUserGroup(ListBoxItem listBoxItem)
        {
            if (Utils.ShowMessageBox(I18nRes.ConfirmUserGroupDeletion, MessageBoxButton.YesNo, STMessageBoxIcon.Question) == MessageBoxResult.No)
                return;
            var name = listBoxItem.ToolTip.ToString()!;
            if (nowSelectedListBoxItem == listBoxItem)
                ListBox_ModsGroupMenu.SelectedIndex = 0;
            ListBox_UserGroup.Items.Remove(listBoxItem);
            allUserGroups.Remove(name);
            allListBoxItems.Remove(name);
            allModShowInfoGroups.Remove(name);
            RefreshModsContextMenu();
            StartRemindSaveThread();
            Expander_RandomEnable.Visibility = Visibility.Collapsed;
            // 删除导出用户分组下拉列表的此分组选择
            for (int i = 0; i < ComboBox_ExportUserGroup.Items.Count; i++)
            {
                if (ComboBox_ExportUserGroup.Items.GetItemAt(i) is ComboBoxItem comboBoxItem && comboBoxItem.Content.ToString()! == name)
                {
                    // 如果此选项正被选中,则选定到All
                    if (ComboBox_ExportUserGroup.SelectedIndex == i)
                        ComboBox_ExportUserGroup.SelectedIndex = 0;
                    ComboBox_ExportUserGroup.Items.RemoveAt(i);
                }
            }
        }

        private void RenameUserGroup(ListBoxItem listBoxItem)
        {
            string icon = ((Emoji.Wpf.TextBlock)ListBoxItemHelper.GetIcon(listBoxItem)).Text;
            string name = listBoxItem.ToolTip.ToString()!;
            AddUserGroup window = new();
            window.TextBox_Icon.Text = icon;
            window.TextBox_Name.Text = name;
            window.Button_Yes.Click += (s, e) =>
            {
                string _icon = window.TextBox_Icon.Text;
                string _name = window.TextBox_Name.Text;
                if (_name == ModTypeGroup.Collected || _name == strUserCustomData)
                {
                    Utils.ShowMessageBox(string.Format(I18nRes.UserGroupCannotNamed, ModTypeGroup.Collected, strUserCustomData), STMessageBoxIcon.Warning, false);
                    return;
                }
                if (name == _name || !allUserGroups.ContainsKey(_name))
                {
                    ListBoxItemHelper.SetIcon(listBoxItem, new Emoji.Wpf.TextBlock() { Text = _icon });
                    // 重命名组名称
                    var temp = allUserGroups[name];
                    allUserGroups.Remove(name);
                    allUserGroups.Add(_name, temp);
                    // 重命名组名称
                    var _temp = allModShowInfoGroups[name];
                    allModShowInfoGroups.Remove(name);
                    allModShowInfoGroups.Add(_name, _temp);
                    // 重命名列表项
                    allListBoxItems.Remove(name);
                    allListBoxItems.Add(_name, listBoxItem);
                    window.Close();
                    SetListBoxItemData(listBoxItem, _name);
                    RefreshCountOfListBoxItems();
                    RefreshModsContextMenu();
                    StartRemindSaveThread();
                }
                else
                    Utils.ShowMessageBox(I18nRes.UserGroupNamingFailed);
            };
            window.Button_Cancel.Click += (s, e) => window.Close();
            window.ShowDialog();
        }

        private void SetListBoxItemData(ListBoxItem item, string name)
        {
            item.Content = name;
            item.ToolTip = name;
            item.Tag = name;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void RemindSave()
        {
            while (remindSaveThread.ThreadState != ThreadState.Unstarted)
            {
                Dispatcher.InvokeAsync(() => Button_Save.Tag = true);
                Thread.Sleep(1000);
                Dispatcher.InvokeAsync(() => Button_Save.Tag = false);
                Thread.Sleep(1000);
            }
        }

        private void StartRemindSaveThread()
        {
            if (remindSaveThread.ThreadState == ThreadState.Unstarted)
                remindSaveThread.Start();
        }

        private void ResetRemindSaveThread()
        {
            if (remindSaveThread.ThreadState != ThreadState.Unstarted)
                remindSaveThread.Join(1);
            remindSaveThread = new(RemindSave);
        }
    }
}