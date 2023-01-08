using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using HKW.TomlParse;
using Panuon.WPF.UI;
using StarsectorTools.Libs.Utils;
using I18n = StarsectorTools.Langs.Tools.ModManager.ModManager_I18n;

namespace StarsectorTools.Tools.ModManager
{
    public partial class ModManager
    {
        private const string userDataFile = $"{ST.CoreDirectory}\\UserData.toml";
        private const string userGroupFile = $"{ST.CoreDirectory}\\UserGroup.toml";
        private const string backupDirectory = $"{ST.CoreDirectory}\\Backup";
        private const string backupModsDirectory = $"{backupDirectory}\\Mods";
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
        private bool clearGameLogOnStart = false;

        /// <summary>记录了模组类型的嵌入资源链接</summary>
        private static readonly Uri modTypeGroupUri = new("/Resources/ModTypeGroup.toml", UriKind.Relative);

        /// <summary>模组分组列表的展开状态</summary>
        private bool isGroupMenuOpen = false;

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
        private Dictionary<string, ModInfo> allModsInfo = new();

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
        private Dictionary<string, ExternalReadOnlySet<string>> allUserGroups = new();

        /// <summary>
        /// <para>全部分组包含的模组显示信息列表</para>
        /// <para><see langword="Key"/>: 分组名称</para>
        /// <para><see langword="Value"/>: 包含的模组显示信息的列表</para>
        /// </summary>
        private Dictionary<string, ObservableCollection<ModShowInfo>> allModShowInfoGroups = new();

        /// <summary>模组显示信息</summary>
        private partial class ModShowInfo : ObservableObject
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

        private void LoadConfig()
        {
            if (!Utils.FileExists(ST.STConfigTomlFile))
                return;
            TomlTable toml = TOML.Parse(ST.STConfigTomlFile);
            try
            {
                clearGameLogOnStart = toml["Game"]["ClearLogOnStart"].AsBoolean;
            }
            catch
            {
                toml["Game"]["ClearLogOnStart"] = false;
                toml.SaveTo(ST.STConfigTomlFile);
            }
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            remindSaveThread = new(RemindSave);
            ModsInfo.AllModsInfo = new(allModsInfo = new());
            ModsInfo.AllEnabledModsId = new(allEnabledModsId = new());
            ModsInfo.AllCollectedModsId = new(allCollectedModsId = new());
            ModsInfo.AllUserGroups = new (allUserGroups = new());
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
                [ModTypeGroup.ContentExpansions] = new(),
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
            int size = 0;
            DirectoryInfo dirs = new(GameInfo.ModsDirectory);
            string err = null!;
            foreach (var dir in dirs.GetDirectories())
            {
                try
                {
                    ModInfo info = GetModInfo($"{dir.FullName}\\{modInfoFile}");
                    allModsInfo.Add(info.Id, info);
                    STLog.WriteLine($"{I18n.ModAddSuccess}: {info.Id}", STLogLevel.DEBUG);
                }
                catch (Exception ex)
                {
                    STLog.WriteLine($"{I18n.ModAddFailed}: {dir.Name}", ex);
                    err ??= $"{I18n.ModAddFailed}\n";
                    err += $"{dir.Name}\n";
                    size++;
                }
            }
            STLog.WriteLine(I18n.ModAddCompleted, STLogLevel.INFO, allModsInfo.Count, size);
            if (err != null)
                Utils.ShowMessageBox(err, STMessageBoxIcon.Warning);
        }

        private static ModInfo GetModInfo(string jsonPath)
        {
            string datas = File.ReadAllText(jsonPath);
            ModInfo modInfo = new(JsonNode.Parse(Utils.JsonParse(datas))!.AsObject());
            modInfo.Path = Path.GetDirectoryName(jsonPath)!;
            return modInfo;
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
            var result = Utils.ShowMessageBox(I18n.SelectImportMode, MessageBoxButton.YesNoCancel, STMessageBoxIcon.Question);
            if (result == MessageBoxResult.Yes)
                ClearAllEnabledMods();
            else if (result == MessageBoxResult.Cancel)
                return;
        }

        private void GetEnabledMods(string filePath, bool importMode = false)
        {
            string datas = File.ReadAllText(filePath);
            if (datas.Length == 0)
                return;
            try
            {
                string err = null!;
                JsonNode enabledModsJson = JsonNode.Parse(Utils.JsonParse(datas))!;
                if (enabledModsJson.AsObject().Count != 1 || enabledModsJson.AsObject().ElementAt(0).Key != strEnabledMods)
                    throw new();
                if (importMode)
                    ImportMode();
                JsonArray enabledModsJsonArray = enabledModsJson[strEnabledMods]!.AsArray();
                STLog.WriteLine($"{I18n.LoadEnabledModsFile} {I18n.Path}: {filePath}");
                foreach (var modId in enabledModsJsonArray)
                {
                    var id = modId!.GetValue<string>();
                    if (string.IsNullOrEmpty(id))
                        continue;
                    if (allModsInfo.ContainsKey(id))
                    {
                        STLog.WriteLine($"{I18n.EnableMod} {id}", STLogLevel.DEBUG);
                        ChangeModEnabled(id, true);
                    }
                    else
                    {
                        STLog.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
                        err ??= $"{I18n.NotFoundMod}:\n";
                        err += $"{id}\n";
                    }
                }
                STLog.WriteLine($"{I18n.EnableMod} {I18n.Size}: {allEnabledModsId.Count}");
                if (err != null)
                    Utils.ShowMessageBox(err, STMessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.LoadError} {I18n.Path}: {filePath}", ex);
                Utils.ShowMessageBox($"{I18n.LoadError}\n{I18n.Path}: {filePath}", STMessageBoxIcon.Error);
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
            STLog.WriteLine($"{I18n.LoadUserGroup} {I18n.Path}: {filePath}");
            try
            {
                string err = null!;
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
                            if (string.IsNullOrEmpty(id))
                                continue;
                            if (allModsShowInfo.ContainsKey(id))
                            {
                                if (allUserGroups[group].Add(id))
                                    allModShowInfoGroups[group].Add(allModsShowInfo[id]);
                            }
                            else
                            {
                                STLog.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
                                err ??= $"{I18n.NotFoundMod}\n";
                                err += $"{id}\n";
                            }
                        }
                    }
                    else
                    {
                        STLog.WriteLine($"{I18n.DuplicateUserGroupName} {group}");
                        err ??= $"{I18n.DuplicateUserGroupName} {group}";
                    }
                }
                if (err is not null)
                    Utils.ShowMessageBox(err, STMessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.FileError} {filePath}", ex);
                Utils.ShowMessageBox($"{I18n.FileError} {filePath}", STMessageBoxIcon.Error);
            }
        }

        private void GetUserData(string filePath)
        {
            STLog.WriteLine($"{I18n.LoadUserData} {I18n.Path}: {filePath}");
            try
            {
                string err = null!;
                TomlTable toml = TOML.Parse(filePath);
                foreach (string id in toml[ModTypeGroup.Collected].AsTomlArray)
                {
                    if (string.IsNullOrEmpty(id))
                        continue;
                    if (allModsShowInfo.ContainsKey(id))
                    {
                        ChangeModCollected(id, true);
                    }
                    else
                    {
                        STLog.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
                        err ??= $"{I18n.NotFoundMod}\n";
                        err += $"{id}\n";
                    }
                }
                foreach (var dict in toml[strUserCustomData].AsTomlArray)
                {
                    var id = dict[strId].AsString;
                    if (string.IsNullOrEmpty(id))
                        continue;
                    if (allModsShowInfo.ContainsKey(id))
                    {
                        var info = allModsShowInfo[id];
                        info.UserDescription = dict[strUserDescription];
                    }
                    else
                    {
                        STLog.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
                        err ??= $"{I18n.NotFoundMod}\n";
                        err += $"{id}\n";
                    }
                }
                if (!string.IsNullOrEmpty(err))
                    Utils.ShowMessageBox(err, STMessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                STLog.WriteLine($"{I18n.UserDataLoadError} {I18n.Path}: {filePath}", ex);
                Utils.ShowMessageBox($"{I18n.UserDataLoadError}\n{I18n.Path}: {filePath}", STMessageBoxIcon.Error);
            }
        }

        private void GetAllListBoxItems()
        {
            foreach (ListBoxItem item in ListBox_ModsGroupMenu.Items)
            {
                if (item.Content is string str)
                    allListBoxItems.Add(item.Tag.ToString()!, item);
                else if (item.Content is Expander expander && expander.Content is ListBox listBox)
                    foreach (ListBoxItem item1 in listBox.Items)
                        allListBoxItems.Add(item1.Tag.ToString()!, item1);
            }
            STLog.WriteLine(I18n.ListBoxItemsRetrievalCompleted);
        }

        private void GetTypeGroup()
        {
            using StreamReader sr = new(Application.GetResourceStream(modTypeGroupUri).Stream);
            TomlTable toml = TOML.Parse(sr);
            foreach (var kv in toml)
                foreach (string id in kv.Value.AsTomlArray)
                    allModsTypeGroup.Add(id, kv.Key);
            STLog.WriteLine(I18n.TypeGroupRetrievalCompleted);
        }

        private string CheckTypeGroup(string id)
        {
            return allModsTypeGroup.ContainsKey(id) ? allModsTypeGroup[id] : ModTypeGroup.UnknownMods;
        }

        private void GetAllModsShowInfo()
        {
            foreach (var modInfo in allModsInfo.Values)
                AddModShowInfo(modInfo, false);
            STLog.WriteLine($"{I18n.ModShowInfoSetSuccess} {I18n.Size}: {allModsInfo.Count}");
            ListBox_ModsGroupMenu.SelectedIndex = 0;
        }

        private void RefreshDataGrid()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                string text = TextBox_SearchMods.Text;
                string type = ((ComboBoxItem)ComboBox_SearchType.SelectedItem).Tag.ToString()!;
                if (text.Length > 0)
                {
                    DataGrid_ModsShowList.ItemsSource = GetSearchModsShowInfo(text, type);
                    STLog.WriteLine($"{I18n.SearchMod} {text}", STLogLevel.DEBUG);
                }
                else
                {
                    DataGrid_ModsShowList.ItemsSource = allModShowInfoGroups[nowGroupName];
                    STLog.WriteLine($"{I18n.ShowGroup} {nowGroupName}");
                    GC.Collect();
                }
            });
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
                ImageSource = GetIcon($"{info.Path}\\icon.ico"),
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
                    STLog.WriteLine($"{I18n.IconLoadError} {I18n.Path}: {filePath}", ex);
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
                STLog.WriteLine($"{I18n.ModCountInGroupRefresh} {item.Content}", STLogLevel.DEBUG);
            }
            STLog.WriteLine(I18n.ModCountInGroupRefreshCompleted);
        }

        private void RefreshModsContextMenu()
        {
            foreach (var showInfo in allModsShowInfo.Values)
                showInfo.ContextMenu = CreateContextMenu(showInfo);
            STLog.WriteLine($"{I18n.ContextMenuRefreshCompleted} {I18n.Size}: {allModsShowInfo.Values.Count}");
        }

        private ContextMenu CreateContextMenu(ModShowInfo showInfo)
        {
            STLog.WriteLine($"{showInfo.Id} {I18n.AddContextMenu}", STLogLevel.DEBUG);
            ContextMenu contextMenu = new();
            // 标记菜单项是否被创建
            contextMenu.Tag = false;
            // 被点击时才加载菜单,可以降低内存占用
            contextMenu.Loaded += (s, e) =>
            {
                if (contextMenu.Tag is true)
                    return;
                contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
                // 启用或禁用
                MenuItem menuItem = new();
                menuItem.Header = showInfo.IsEnabled ? I18n.DisableSelectedMods : I18n.EnabledSelectedMods;
                menuItem.Click += (s, e) => ChangeSelectedModsEnabled();
                contextMenu.Items.Add(menuItem);
                STLog.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 收藏或取消收藏
                menuItem = new();
                menuItem.Header = showInfo.IsCollected ? I18n.UncollectSelectedMods : I18n.CollectSelectedMods;
                menuItem.Click += (s, e) => ChangeSelectedModsCollected();
                contextMenu.Items.Add(menuItem);
                STLog.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 打开模组文件夹
                menuItem = new();
                menuItem.Header = I18n.OpenModDirectory;
                menuItem.Click += (s, e) =>
                {
                    STLog.WriteLine($"{I18n.OpenModDirectory} {I18n.Path}: {allModsInfo[showInfo.Id].Path}");
                    Utils.OpenLink(allModsInfo[showInfo.Id].Path);
                };
                contextMenu.Items.Add(menuItem);
                STLog.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 删除模组至回收站
                menuItem = new();
                menuItem.Header = I18n.DeleteMod;
                menuItem.Click +=  (s, e) => 
                {
                    string path = allModsInfo[showInfo.Id].Path;
                    if (Utils.ShowMessageBox($"{I18n.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18n.Path}: {path}\n", MessageBoxButton.YesNo, STMessageBoxIcon.Warning) == MessageBoxResult.Yes)
                    {
                        STLog.WriteLine($"{I18n.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18n.Path}: {path}\n");
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
                    menuItem.Header = I18n.AddModToUserGroup;
                    foreach (var group in allUserGroups.Keys)
                    {
                        if (!allUserGroups[group].Contains(showInfo.Id))
                        {
                            MenuItem groupItem = new();
                            groupItem.Header = group;
                            groupItem.Style = (Style)Application.Current.Resources["MenuItem_Style"];
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
                        STLog.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                    }
                }
                // 从用户分组中删除
                var groupWithMod = allUserGroups.Where(g => g.Value.Contains(showInfo.Id));
                if (groupWithMod.Count() > 0)
                {
                    menuItem = new();
                    menuItem.Header = I18n.RemoveFromUserGroup;
                    foreach (var group in groupWithMod)
                    {
                        MenuItem groupItem = new();
                        groupItem.Header = group.Key;
                        groupItem.Style = (Style)Application.Current.Resources["MenuItem_Style"];
                        groupItem.Click += (s, e) =>
                        {
                            ChangeSelectedModsInUserGroup(group.Key, false);
                        };
                        menuItem.Items.Add(groupItem);
                    }
                    if (menuItem.Items.Count > 0)
                    {
                        contextMenu.Items.Add(menuItem);
                        STLog.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
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
                    STLog.WriteLine($"{id} {I18n.AddModToUserGroup} {group}", STLogLevel.DEBUG);
                }
            }
            else
            {
                if (allUserGroups[group].Remove(id))
                {
                    allModShowInfoGroups[group].Remove(allModsShowInfo[id]);
                    STLog.WriteLine($"{id} {I18n.RemoveFromUserGroup} {group}", STLogLevel.DEBUG);
                }
            }
            showInfo.ContextMenu = CreateContextMenu(showInfo);
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
            STLog.WriteLine(I18n.DisableAllEnabledMods);
        }

        private void ChangeModEnabled(string id, bool? enabled = null)
        {
            ModShowInfo showInfo = allModsShowInfo[id];
            showInfo.IsEnabled = (bool)(enabled is null ? !showInfo.IsEnabled : enabled);
            showInfo.ContextMenu = CreateContextMenu(showInfo);
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
            STLog.WriteLine($"{id} {I18n.ChangeEnabledStateTo} {showInfo.IsEnabled}", STLogLevel.DEBUG);
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
                        STLog.WriteLine($"{showInfo.Id} {I18n.NotEnableDependencies} {showInfo.Dependencies}");
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
            showInfo.ContextMenu = CreateContextMenu(showInfo);
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
            STLog.WriteLine($"{id} {I18n.ChangeCollectStateTo} {showInfo.IsCollected}", STLogLevel.DEBUG);
        }

        private void SaveAllData()
        {
            SaveEnabledMods(GameInfo.EnabledModsJsonFile);
            SaveUserData(userDataFile);
            SaveUserGroup(userGroupFile);
        }

        private void SaveEnabledMods(string filePath)
        {
            JsonObject keyValues = new()
            {
                [strEnabledMods] = new JsonArray()
            };
            foreach (var mod in allEnabledModsId)
                ((JsonArray)keyValues[strEnabledMods]!).Add(mod);
            File.WriteAllText(filePath, keyValues.ToJsonString(new() { WriteIndented = true }));
            STLog.WriteLine($"{I18n.EnabledListSaveCompleted} {I18n.Path}: {filePath}");
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
            STLog.WriteLine($"{I18n.SaveUserDataSuccess} {I18n.Path}: {filePath}");
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
            STLog.WriteLine($"{I18n.UserGroupSaveCompleted} {I18n.Path}: {filePath}");
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
            STLog.WriteLine($"{I18n.CloseDetails} {nowSelectedModId}", STLogLevel.DEBUG);
        }

        private void SetModDetails(string id)
        {
            var info = allModsInfo[id];
            var showInfo = allModsShowInfo[id];
            if (showInfo.ImageSource != null)
                Image_ModImage.Source = showInfo.ImageSource;
            else
                Image_ModImage.Source = null;
            Label_ModName.Content = showInfo.Name;
            Label_ModId.Content = showInfo.Id;
            Label_ModVersion.Content = showInfo.Version;
            Label_GameVersion.Content = showInfo.GameVersion;
            Button_ModPath.Content = info.Path;
            TextBlock_ModAuthor.Text = showInfo.Author;
            if (info.Dependencies is not null)
            {
                GroupBox_ModDependencies.Visibility = Visibility.Visible;
                TextBlock_ModDependencies.Text = string.Join("\n", info.Dependencies.Select(i => $"{I18n.Name}: {i.Name} ID: {i.Id} " + (i.Version is not null ? $"{I18n.Version} {i.Version}" : ""))!);
            }
            else
                GroupBox_ModDependencies.Visibility = Visibility.Collapsed;
            TextBlock_ModDescription.Text = info.Description;
            TextBox_UserDescription.Text = showInfo.UserDescription!;
            STLog.WriteLine($"{I18n.ShowDetails} {id}", STLogLevel.DEBUG);
        }

        private void DropFile(string filePath)
        {
            string tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}Temp";
            if (!Utils.UnArchiveFileToDir(filePath, tempPath))
            {
                Utils.ShowMessageBox($"{I18n.UnzipError}\n {I18n.Path}:{filePath}");
                return;
            }
            DirectoryInfo dirs = new(tempPath);
            var filesInfo = dirs.GetFiles(modInfoFile, SearchOption.AllDirectories);
            if (filesInfo.Length > 0 && filesInfo.First() is FileInfo fileInfo && fileInfo.FullName is string jsonPath)
            {
                var newModInfo = GetModInfo(jsonPath);
                string directoryName = Path.GetFileName(fileInfo.DirectoryName)!;
                newModInfo.Path = $"{GameInfo.ModsDirectory}\\{directoryName}";
                if (allModsInfo.ContainsKey(newModInfo.Id))
                {
                    var originalModInfo = allModsInfo[newModInfo.Id];
                    if (Utils.ShowMessageBox($"{newModInfo.Id}\n{string.Format(I18n.DuplicateModExists, originalModInfo.Version, newModInfo.Version)}",
                                                                  MessageBoxButton.YesNo,
                                                                  STMessageBoxIcon.Question,
                                                                  false) == MessageBoxResult.Yes)
                    {
                        Utils.CopyDirectory(originalModInfo.Path, $"{backupModsDirectory}\\Temp");
                        string tempDirectory = $"{backupModsDirectory}\\Temp";
                        Utils.ArchiveDirToDir(tempDirectory, backupModsDirectory, directoryName);
                        Directory.Delete(tempDirectory, true);
                        Directory.Delete(originalModInfo.Path, true);
                        Utils.CopyDirectory(Path.GetDirectoryName(jsonPath)!, GameInfo.ModsDirectory);
                        Dispatcher.BeginInvoke(() =>
                        {
                            RemoveMod(newModInfo.Id);
                            AddMod(newModInfo);
                            RefreshCountOfListBoxItems();
                            StartRemindSaveThread();
                        });
                        STLog.WriteLine($"{I18n.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}");
                    }
                }
                else
                {
                    Utils.CopyDirectory(Path.GetDirectoryName(jsonPath)!, GameInfo.ModsDirectory);
                    Dispatcher.BeginInvoke(() =>
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
                STLog.WriteLine($"{I18n.ZipFileError} {I18n.Path}: {filePath}");
                Utils.ShowMessageBox($"{I18n.ZipFileError}\n{I18n.Path}: {filePath}");
            }
            Dispatcher.BeginInvoke(() => dirs.Delete(true));
        }

        private void RemoveMod(string id)
        {
            var modInfo = allModsInfo[id];
            allModsInfo.Remove(id);
            RemoveModShowInfo(id);
            STLog.WriteLine($"{I18n.RemoveMod} {id} {modInfo.Version}", STLogLevel.DEBUG);
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
            allModsInfo.Add(modInfo.Id, modInfo);
            AddModShowInfo(modInfo);
            STLog.WriteLine($"{I18n.RemoveMod} {modInfo.Id} {modInfo.Version}", STLogLevel.DEBUG);
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
                showInfo.ContextMenu = CreateContextMenu(showInfo);
            STLog.WriteLine($"{I18n.AddMod} {showInfo.Id} {showInfo.Version}", STLogLevel.DEBUG);
        }

        private void ClearDataGridSelected()
        {
            DataGrid_ModsShowList.UnselectAll();
        }

        private void AddUserGroup(string icon, string name)
        {
            ListBoxItem listBoxItem = new();
            // 调用全局资源需要写全
            listBoxItem.Style = (Style)Application.Current.Resources["ListBoxItem_Style"];
            SetListBoxItemData(listBoxItem, name);
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            // 重命名分组
            MenuItem menuItem = new();
            menuItem.Header = I18n.RenameUserGroup;
            menuItem.Click += (s, e) =>
            {
                RenameUserGroup((ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)s)));
            };
            contextMenu.Items.Add(menuItem);
            STLog.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
            // 删除分组
            menuItem = new();
            menuItem.Header = I18n.RemoveUserGroup;
            menuItem.Click += (s, e) =>
            {
                RemoveUserGroup((ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)s)));
            };
            contextMenu.Items.Add(menuItem);
            STLog.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);

            listBoxItem.ContextMenu = contextMenu;
            ListBoxItemHelper.SetIcon(listBoxItem, new Emoji.Wpf.TextBlock() { Text = icon });
            ListBox_UserGroup.Items.Add(listBoxItem);
            allUserGroups.Add(name, new());
            allListBoxItems.Add(name, listBoxItem);
            allModShowInfoGroups.Add(name, new());
            StartRemindSaveThread();
            ComboBox_ExportUserGroup.Items.Add(new ComboBoxItem() { Content = name, Tag = name, Style = (Style)Application.Current.Resources["ComboBoxItem_Style"] });
            STLog.WriteLine($"{I18n.AddUserGroup} {icon} {name}");
        }

        private void RemoveUserGroup(ListBoxItem listBoxItem)
        {
            if (Utils.ShowMessageBox(I18n.ConfirmUserGroupDeletion, MessageBoxButton.YesNo, STMessageBoxIcon.Question) == MessageBoxResult.No)
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
                    Utils.ShowMessageBox(string.Format(I18n.UserGroupCannotNamed, ModTypeGroup.Collected, strUserCustomData), STMessageBoxIcon.Warning, false);
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
                    Utils.ShowMessageBox(I18n.UserGroupNamingFailed);
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
                Dispatcher.BeginInvoke(() => Button_Save.Tag = true);
                Thread.Sleep(1000);
                Dispatcher.BeginInvoke(() => Button_Save.Tag = false);
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

        private void ClearGameLogFile()
        {
            if (Utils.FileExists(GameInfo.LogFile, false))
                Utils.DeleteFileToRecycleBin(GameInfo.LogFile);
            File.Create(GameInfo.LogFile).Close();
            STLog.WriteLine(I18n.GameLogCleanupCompleted);
        }
    }
}