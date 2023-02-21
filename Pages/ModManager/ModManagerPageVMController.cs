using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.Extension;
using HKW.Libs.Log4Cs;
using HKW.Libs.TomlParse;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialogs;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Resources;
using I18nRes = StarsectorTools.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorTools.Pages.ModManager
{
    internal partial class ModManagerPageViewModel
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

        public bool NeedSave { get; private set; } = false;

        /// <summary>记录了模组类型的嵌入资源链接</summary>
        private static readonly Uri modTypeGroupUri =
            new("/Resources/ModTypeGroup.toml", UriKind.Relative);

        /// <summary>提醒保存配置的动画线程</summary>
        private Thread remindSaveThread = null!;

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
            public HashSet<string>? DependenciesSet { get; set; }

            /// <summary>展开启用前置的按钮</summary>
            [ObservableProperty]
            private bool missDependencies;

            /// <summary>用户描述</summary>
            [ObservableProperty]
            private string userDescription = string.Empty;

            /// <summary>右键菜单</summary>
            [ObservableProperty]
            private ContextMenuVM contextMenu = null!;
        }

        /// <summary>已启用的模组ID</summary>
        private HashSet<string> allEnabledModsId = new();

        /// <summary>已收藏的模组ID</summary>
        private HashSet<string> allCollectedModsId = new();

        /// <summary>
        /// <para>全部分组列表项</para>
        /// <para><see langword="Key"/>: 列表项Tag或ModGroupType</para>
        /// <para><see langword="Value"/>: 列表项</para>
        /// </summary>
        private Dictionary<string, ListBoxItemVM> allListBoxItems = new();

        /// <summary>
        /// <para>全部模组信息</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 模组信息</para>
        /// </summary>
        private Dictionary<string, ModInfo> allModInfos = new();

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
        private Dictionary<string, HashSet<ModShowInfo>> allModShowInfoGroups =
            new()
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

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            remindSaveThread = new(RemindSave);
            ModsInfo.AllModsInfo = new(allModInfos);
            ModsInfo.AllEnabledModsId = new(allEnabledModsId);
            ModsInfo.AllCollectedModsId = new(allCollectedModsId);
            ModsInfo.AllUserGroups = allUserGroups.AsReadOnly<
                string,
                HashSet<string>,
                IReadOnlySet<string>
            >();
            GetAllModsInfo();
            GetAllListBoxItems();
            GetTypeGroup();
            GetAllModsShowInfo();
            CheckEnabledMods();
            CheckEnabledModsDependencies();
            CheckUserData();
            RefreshModsContextMenu();
            RefreshGroupModCount();
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
                    Logger.Record($"{I18nRes.ModAddSuccess}: {dir.FullName}", LogLevel.DEBUG);
                }
                else
                {
                    err += $"{dir.FullName}\n";
                    errSize++;
                }
            }
            Logger.Record(
                string.Format(I18nRes.ModAddCompleted, allModInfos.Count, errSize),
                LogLevel.INFO
            );
            if (!string.IsNullOrWhiteSpace(err))
                MessageBoxVM.Show(
                    new($"{I18nRes.ModAddFailed} {I18nRes.Size}: {errSize}\n{err}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
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
            var result = MessageBoxVM.Show(
                new(I18nRes.SelectImportMode)
                {
                    Button = MessageBoxVM.Button.YesNoCancel,
                    Icon = MessageBoxVM.Icon.Question
                }
            );
            if (result == MessageBoxVM.Result.Yes)
                ClearAllEnabledMods();
            else if (result == MessageBoxVM.Result.Cancel)
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
                if (
                    enabledModsJson.AsObject().Count != 1
                    || enabledModsJson.AsObject().ElementAt(0).Key != strEnabledMods
                )
                    throw new ArgumentNullException();
                if (importMode)
                    ImportMode();
                JsonArray enabledModsJsonArray = enabledModsJson[strEnabledMods]!.AsArray();
                Logger.Record($"{I18nRes.LoadEnabledModsFile} {I18nRes.Path}: {filePath}");
                foreach (var modId in enabledModsJsonArray)
                {
                    var id = modId!.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    if (allModInfos.ContainsKey(id))
                    {
                        Logger.Record($"{I18nRes.EnableMod} {id}", LogLevel.DEBUG);
                        ChangeModEnabled(id, true);
                    }
                    else
                    {
                        Logger.Record($"{I18nRes.NotFoundMod} {id}", LogLevel.WARN);
                        err += $"{id}\n";
                        errSize++;
                    }
                }
                Logger.Record($"{I18nRes.EnableMod} {I18nRes.Size}: {allEnabledModsId.Count}");
                if (!string.IsNullOrWhiteSpace(err))
                    MessageBoxVM.Show(
                        new($"{I18nRes.NotFoundMod} {I18nRes.Size}: {errSize}\n{err}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18nRes.LoadError} {I18nRes.Path}: {filePath}", ex);
                MessageBoxVM.Show(
                    new($"{I18nRes.LoadError}\n{I18nRes.Path}: {filePath}")
                    {
                        Icon = MessageBoxVM.Icon.Error
                    }
                );
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
            Logger.Record($"{I18nRes.LoadUserGroup} {I18nRes.Path}: {filePath}");
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
                                Logger.Record($"{I18nRes.NotFoundMod} {id}", LogLevel.WARN);
                                err += $"{id}\n";
                                errSize++;
                            }
                        }
                    }
                    else
                    {
                        Logger.Record($"{I18nRes.DuplicateUserGroupName} {group}");
                        err ??= $"{I18nRes.DuplicateUserGroupName} {group}";
                    }
                }
                if (!string.IsNullOrWhiteSpace(err))
                    MessageBoxVM.Show(
                        new($"{I18nRes.NotFoundMod} {I18nRes.Size}: {errSize}\n{err}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18nRes.FileError} {filePath}", ex);
                MessageBoxVM.Show(
                    new($"{I18nRes.FileError} {filePath}") { Icon = MessageBoxVM.Icon.Error }
                );
            }
        }

        private void GetUserData(string filePath)
        {
            Logger.Record($"{I18nRes.LoadUserData} {I18nRes.Path}: {filePath}");
            try
            {
                string err = string.Empty;
                int errSize = 0;
                var toml = TOML.Parse(filePath);
                Logger.Record(I18nRes.LoadCollectedList);
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
                        Logger.Record($"{I18nRes.NotFoundMod} {id}", LogLevel.WARN);
                        err += $"{id}\n";
                    }
                }
                if (!string.IsNullOrWhiteSpace(err))
                    MessageBoxVM.Show(
                        new(
                            $"{I18nRes.LoadCollectedList} {I18nRes.NotFoundMod} {I18nRes.Size}: {errSize}\n{err}"
                        )
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                err = string.Empty;
                errSize = 0;
                Logger.Record(I18nRes.LoadUserCustomData);
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
                        Logger.Record($"{I18nRes.NotFoundMod} {id}", LogLevel.WARN);
                        err ??= $"{I18nRes.NotFoundMod}\n";
                        err += $"{id}\n";
                    }
                }
                if (!string.IsNullOrWhiteSpace(err))
                    MessageBoxVM.Show(
                        new(
                            $"{I18nRes.LoadUserCustomData} {I18nRes.NotFoundMod} {I18nRes.Size}: {errSize}\n{err}"
                        )
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18nRes.UserDataLoadError} {I18nRes.Path}: {filePath}", ex);
                MessageBoxVM.Show(
                    new($"{I18nRes.UserDataLoadError}\n{I18nRes.Path}: {filePath}")
                    {
                        Icon = MessageBoxVM.Icon.Error
                    }
                );
            }
        }

        private void GetAllListBoxItems()
        {
            foreach (var item in ListBox_MainMenu)
                allListBoxItems.Add(item.Tag!.ToString()!, item);
            foreach (var item in ListBox_TypeGroupMenu)
                allListBoxItems.Add(item.Tag!.ToString()!, item);
            foreach (var item in ListBox_UserGroupMenu)
                allListBoxItems.Add(item.Tag!.ToString()!, item);
            Logger.Record(I18nRes.ListBoxItemsRetrievalCompleted);
        }

        private void GetTypeGroup()
        {
            using StreamReader sr = ResourceDictionary.GetResourceStream(ResourceDictionary.ModTypeGroup_toml); ;
            TomlTable toml = TOML.Parse(sr);
            foreach (var kv in toml)
                foreach (string id in kv.Value.AsTomlArray)
                    allModsTypeGroup.Add(id, kv.Key);
            Logger.Record(I18nRes.TypeGroupRetrievalCompleted);
        }

        private string CheckTypeGroup(string id)
        {
            return allModsTypeGroup.ContainsKey(id)
                ? allModsTypeGroup[id]
                : ModTypeGroup.UnknownMods;
        }

        private void GetAllModsShowInfo()
        {
            foreach (var modInfo in allModInfos.Values)
                AddModShowInfo(modInfo, false);
            Logger.Record($"{I18nRes.ModShowInfoSetSuccess} {I18nRes.Size}: {allModInfos.Count}");
            //ListBox_ModsGroupMenu.SelectedIndex = 0;
        }

        private void CheckRefreshGroupAndMods(string group)
        {
            if (nowSelectedGroupName == group)
                RefreshShowMods();
            RefreshGroupModCount();
        }

        private void RefreshShowMods()
        {
            var text = ModFilterText;
            var type = ComboBox_ModFilterType.SelectedItem!.Tag!.ToString()!;
            if (!string.IsNullOrEmpty(text))
            {
                ShowDataGridItems(GetSearchModsShowInfo(text, type));
                Logger.Record($"{I18nRes.SearchMod} {text}");
            }
            else
            {
                ShowDataGridItems(allModShowInfoGroups[nowSelectedGroupName]);
                Logger.Record($"{I18nRes.ShowGroup} {nowSelectedGroupName}");
            }
        }

        private void ShowDataGridItems(IEnumerable<ModShowInfo> infos)
        {
            NowShowMods.Clear();
            foreach (var info in infos)
            {
                // TODO: 需优化显示方式,DataGrid效率太差
                //await Task.Delay(10);
                NowShowMods.Add(info);
            }
        }

        private List<ModShowInfo> GetSearchModsShowInfo(string text, string type) =>
            new List<ModShowInfo>(
                type switch
                {
                    strName
                        => allModShowInfoGroups[nowSelectedGroupName].Where(
                            i => i.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    strId
                        => allModShowInfoGroups[nowSelectedGroupName].Where(
                            i => i.Id.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    strAuthor
                        => allModShowInfoGroups[nowSelectedGroupName].Where(
                            i => i.Author.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    strUserDescription
                        => allModShowInfoGroups[nowSelectedGroupName].Where(
                            i =>
                                i.UserDescription.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    _ => null!
                }
            );

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
                DependenciesSet = info.Dependencies is not null
                    ? new(info.Dependencies.Select(i => i.Id))
                    : null!,
                ImageSource = GetImage($"{info.ModDirectory}\\icon.ico"),
            };
            BitmapImage? GetImage(string filePath)
            {
                if (!File.Exists(filePath))
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
                    Logger.Record($"{I18nRes.IconLoadError} {I18nRes.Path}: {filePath}", ex);
                    return null;
                }
            }
        }

        private void RefreshGroupModCount()
        {
            foreach (var item in allListBoxItems.Values)
            {
                int size = allModShowInfoGroups[item!.Tag!.ToString()!].Count;
                item.Content = $"{item.ToolTip} ({size})";
                Logger.Record($"{I18nRes.ModCountInGroupRefresh} {item.Content}", LogLevel.DEBUG);
            }
            Logger.Record(I18nRes.ModCountInGroupRefreshCompleted);
        }

        private void RefreshModsContextMenu()
        {
            foreach (var showInfo in allModsShowInfo.Values)
                showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            Logger.Record(
                $"{I18nRes.ContextMenuRefreshCompleted} {I18nRes.Size}: {allModsShowInfo.Values.Count}"
            );
        }

        private ContextMenuVM CreateModShowContextMenu(ModShowInfo showInfo)
        {
            Logger.Record($"{showInfo.Id} {I18nRes.AddContextMenu}", LogLevel.DEBUG);
            ContextMenuVM contextMenu =
                new(
                    (list) =>
                    {
                        // 启用或禁用
                        MenuItemVM menuItem = new();
                        menuItem.Header = showInfo.IsEnabled
                            ? I18nRes.DisableSelectedMods
                            : I18nRes.EnabledSelectedMods;
                        menuItem.CommandEvent += (p) => ChangeSelectedModsEnabled();
                        list.Add(menuItem);
                        Logger.Record($"{I18nRes.AddMenuItem} {menuItem.Header}", LogLevel.DEBUG);
                        // 收藏或取消收藏
                        menuItem = new();
                        menuItem.Header = showInfo.IsCollected
                            ? I18nRes.UncollectSelectedMods
                            : I18nRes.CollectSelectedMods;
                        menuItem.CommandEvent += (p) => ChangeSelectedModsCollected();
                        list.Add(menuItem);
                        Logger.Record($"{I18nRes.AddMenuItem} {menuItem.Header}", LogLevel.DEBUG);
                        // 打开模组文件夹
                        menuItem = new();
                        menuItem.Header = I18nRes.OpenModDirectory;
                        menuItem.CommandEvent += (p) =>
                        {
                            Logger.Record(
                                $"{I18nRes.OpenModDirectory} {I18nRes.Path}: {allModInfos[showInfo.Id].ModDirectory}"
                            );
                            Utils.OpenLink(allModInfos[showInfo.Id].ModDirectory);
                        };
                        list.Add(menuItem);
                        Logger.Record($"{I18nRes.AddMenuItem} {menuItem.Header}", LogLevel.DEBUG);
                        // 删除模组至回收站
                        menuItem = new();
                        menuItem.Header = I18nRes.DeleteMod;
                        menuItem.CommandEvent += (p) =>
                        {
                            string path = allModInfos[showInfo.Id].ModDirectory;
                            if (
                                MessageBoxVM.Show(
                                    new(
                                        $"{I18nRes.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18nRes.Path}: {path}\n"
                                    )
                                    {
                                        Button = MessageBoxVM.Button.YesNo,
                                        Icon = MessageBoxVM.Icon.Warning
                                    }
                                ) is MessageBoxVM.Result.Yes
                            )
                            {
                                Logger.Record(
                                    $"{I18nRes.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18nRes.Path}: {path}\n"
                                );
                                RemoveMod(showInfo.Id);
                                RefreshShowMods();
                                RefreshGroupModCount();
                                CloseModDetails();
                                Utils.DeleteDirToRecycleBin(path);
                                StartRemindSaveThread();
                            }
                        };
                        list.Add(menuItem);
                        // 添加至用户分组
                        if (allUserGroups.Count > 0)
                        {
                            menuItem = new();
                            menuItem.Header = I18nRes.AddModToUserGroup;
                            foreach (var group in allUserGroups.Keys)
                            {
                                if (!allUserGroups[group].Contains(showInfo.Id))
                                {
                                    MenuItemVM groupItem = new();
                                    groupItem.Header = group;
                                    groupItem.CommandEvent += (p) =>
                                    {
                                        ChangeSelectedModsInUserGroup(group, true);
                                    };
                                    menuItem.Add(groupItem);
                                }
                            }
                            if (menuItem.Count > 0)
                            {
                                list.Add(menuItem);
                                Logger.Record(
                                    $"{I18nRes.AddMenuItem} {menuItem.Header}",
                                    LogLevel.DEBUG
                                );
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
                                MenuItemVM groupItem = new();
                                groupItem.Header = group.Key;
                                groupItem.CommandEvent += (p) =>
                                {
                                    ChangeSelectedModsInUserGroup(group.Key, false);
                                };
                                menuItem.Add(groupItem);
                            }
                            if (menuItem.Count > 0)
                            {
                                list.Add(menuItem);
                                Logger.Record(
                                    $"{I18nRes.AddMenuItem} {menuItem.Header}",
                                    LogLevel.DEBUG
                                );
                            }
                        }
                    }
                );
            return contextMenu;
        }

        private void ChangeSelectedModsInUserGroup(string group, bool isInGroup)
        {
            int conut = nowSelectedMods.Count;
            for (int i = 0; i < nowSelectedMods.Count;)
            {
                ChangeModInUserGroup(group, nowSelectedMods[i].Id, isInGroup);
                // 如果已选择数量没有变化,则继续下一个选项
                if (conut == nowSelectedMods.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (conut != nowSelectedMods.Count)
                CloseModDetails();
            CheckRefreshGroupAndMods(group);
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
                    Logger.Record($"{id} {I18nRes.AddModToUserGroup} {group}", LogLevel.DEBUG);
                }
            }
            else
            {
                if (allUserGroups[group].Remove(id))
                {
                    allModShowInfoGroups[group].Remove(allModsShowInfo[id]);
                    Logger.Record($"{id} {I18nRes.RemoveFromUserGroup} {group}", LogLevel.DEBUG);
                }
            }
            showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
        }

        private void ChangeSelectedModsEnabled(bool? enabled = null)
        {
            int conut = nowSelectedMods.Count;
            for (int i = 0; i < nowSelectedMods.Count;)
            {
                ChangeModEnabled(nowSelectedMods[i].Id, enabled);
                // 如果已选择数量没有变化,则继续下一个选项
                if (conut == nowSelectedMods.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (conut != nowSelectedMods.Count)
                CloseModDetails();
            CheckRefreshGroupAndMods(nameof(ModTypeGroup.Enabled));
            CheckEnabledModsDependencies();
            StartRemindSaveThread();
        }

        private void ClearAllEnabledMods()
        {
            while (allEnabledModsId.Count > 0)
                ChangeModEnabled(allEnabledModsId.ElementAt(0), false);
            Logger.Record(I18nRes.DisableAllEnabledMods);
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
            Logger.Record(
                $"{id} {I18nRes.ChangeEnabledStateTo} {showInfo.IsEnabled}",
                LogLevel.DEBUG
            );
        }

        private void CheckEnabledModsDependencies()
        {
            foreach (var showInfo in allModShowInfoGroups[ModTypeGroup.Enabled])
            {
                if (showInfo.DependenciesSet != null)
                {
                    showInfo.Dependencies = string.Join(
                        " , ",
                        showInfo.DependenciesSet.Where(s => !allEnabledModsId.Contains(s))
                    );
                    if (showInfo.Dependencies.Length > 0)
                    {
                        Logger.Record(
                            $"{showInfo.Id} {I18nRes.NotEnableDependencies} {showInfo.Dependencies}"
                        );
                        showInfo.MissDependencies = true;
                    }
                    else
                        showInfo.MissDependencies = false;
                }
            }
        }

        private void ChangeSelectedModsCollected(bool? collected = null)
        {
            int conut = nowSelectedMods.Count;
            for (int i = 0; i < nowSelectedMods.Count;)
            {
                ChangeModCollected(nowSelectedMods[i].Id, collected);
                if (conut == nowSelectedMods.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (conut != nowSelectedMods.Count)
                CloseModDetails();
            CheckRefreshGroupAndMods(nameof(ModTypeGroup.Collected));
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
            Logger.Record(
                $"{id} {I18nRes.ChangeCollectStateTo} {showInfo.IsCollected}",
                LogLevel.DEBUG
            );
        }

        private void SaveAllData()
        {
            SaveEnabledMods(GameInfo.EnabledModsJsonFile);
            SaveUserData(userDataFile);
            SaveUserGroup(userGroupFile);
        }

        private void SaveEnabledMods(string filePath)
        {
            JsonObject jsonObject = new() { [strEnabledMods] = new JsonArray() };
            foreach (var mod in allEnabledModsId)
                jsonObject[strEnabledMods]!.AsArray().Add(mod);
            jsonObject.SaveTo(filePath);
            Logger.Record($"{I18nRes.EnabledListSaveCompleted} {I18nRes.Path}: {filePath}");
        }

        private void SaveUserData(string filePath)
        {
            TomlTable toml =
                new()
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
                    toml[strUserCustomData].Add(
                        new TomlTable()
                        {
                            [strId] = info.Id,
                            [strUserDescription] =
                                info.UserDescription!.Length > 0 ? info.UserDescription : "",
                        }
                    );
                }
            }
            toml.SaveTo(filePath);
            Logger.Record($"{I18nRes.SaveUserDataSuccess} {I18nRes.Path}: {filePath}");
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
            Logger.Record($"{I18nRes.UserGroupSaveCompleted} {I18nRes.Path}: {filePath}");
            void Save(string name)
            {
                var mods = allUserGroups[name];
                toml.Add(
                    name,
                    new TomlTable()
                    {
                        [strIcon] = allListBoxItems[name].Icon!.ToString()!,
                        [strMods] = new TomlArray(),
                    }
                );
                foreach (var id in mods)
                    toml[name][strMods].Add(id);
            }
        }

        private void ChangeShowModDetails(ModShowInfo? info)
        {
            if (info is null)
                CloseModDetails();
            else if (IsShowModDetails && nowSelectedMod?.Id == info.Id)
                CloseModDetails();
            else
                ShowModDetails(info.Id);
            nowSelectedMod = info;
        }

        private void ShowModDetails(string id)
        {
            IsShowModDetails = true;
            SetModDetails(id);
        }

        private void CloseModDetails()
        {
            IsShowModDetails = false;
            ModDetailUserDescription = string.Empty;
            Logger.Record($"{I18nRes.CloseDetails}", LogLevel.DEBUG);
        }

        private void SetModDetails(string id)
        {
            var info = allModInfos[id];
            var showInfo = allModsShowInfo[id];
            ModDetailImage = showInfo.ImageSource;
            ModDetailName = showInfo.Name;
            ModDetailId = showInfo.Id;
            ModDetailModVersion = showInfo.Version;
            ModDetailGameVersion = showInfo.GameVersion;
            ModDetailPath = info.ModDirectory;
            ModDetailAuthor = showInfo.Author;
            if (info.Dependencies is not null)
            {
                ModDetailDependencies = string.Join(
                    "\n",
                    info.Dependencies.Select(
                        i =>
                            $"{I18nRes.Name}: {i.Name} ID: {i.Id} "
                            + (i.Version is not null ? $"{I18nRes.Version} {i.Version}" : "")
                    )!
                );
            }
            else
                ModDetailDependencies = string.Empty;
            ModDetailDescription = info.Description;
            ModDetailUserDescription = showInfo.UserDescription!;
            Logger.Record($"{I18nRes.ShowDetails} {id}", LogLevel.DEBUG);
        }

        internal async Task DropFile(string filePath)
        {
            string tempPath = "Temp";
            if (!await Utils.UnArchiveFileToDir(filePath, tempPath))
            {
                MessageBoxVM.Show(new($"{I18nRes.UnzipError}\n {I18nRes.Path}:{filePath}"));
                return;
            }
            DirectoryInfo dirs = new(tempPath);
            var filesInfo = dirs.GetFiles(modInfoFile, SearchOption.AllDirectories);
            if (
                filesInfo.FirstOrDefault(defaultValue: null) is FileInfo fileInfo
                && fileInfo.FullName is string jsonPath
            )
            {
                string directoryName = Path.GetFileName(fileInfo.DirectoryName)!;
                if (
                    ModInfo.Parse(
                        Utils.JsonParse(jsonPath)!,
                        $"{GameInfo.ModsDirectory}\\{directoryName}"
                    )
                    is not ModInfo newModInfo
                )
                {
                    MessageBoxVM.Show(new($"{I18nRes.FileError}\n{I18nRes.Path}: {filePath}"));
                    return;
                }
                if (allModInfos.ContainsKey(newModInfo.Id))
                {
                    var originalModInfo = allModInfos[newModInfo.Id];
                    var result = MessageBoxVM.Show(
                        new(
                            $"{newModInfo.Id}\n{string.Format(I18nRes.DuplicateModExists, originalModInfo.Version, newModInfo.Version)}"
                        )
                        {
                            Button = MessageBoxVM.Button.YesNoCancel,
                            Icon = MessageBoxVM.Icon.Question,
                        }
                    );
                    if (result == MessageBoxVM.Result.Yes)
                    {
                        Utils.CopyDirectory(
                            originalModInfo.ModDirectory,
                            $"{backupModsDirectory}\\{tempPath}"
                        );
                        string tempDirectory = $"{backupModsDirectory}\\{tempPath}";
                        Utils.ArchiveDirToDir(tempDirectory, backupModsDirectory, directoryName);
                        Directory.Delete(tempDirectory, true);
                        Directory.Delete(originalModInfo.ModDirectory, true);
                        Utils.CopyDirectory(
                            Path.GetDirectoryName(jsonPath)!,
                            GameInfo.ModsDirectory
                        );
                        RemoveMod(newModInfo.Id);
                        AddMod(newModInfo);
                        RefreshGroupModCount();
                        StartRemindSaveThread();
                        Logger.Record(
                            $"{I18nRes.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}"
                        );
                    }
                    else if (result == MessageBoxVM.Result.No)
                    {
                        Utils.DeleteDirToRecycleBin(originalModInfo.ModDirectory);
                        Utils.CopyDirectory(
                            Path.GetDirectoryName(jsonPath)!,
                            GameInfo.ModsDirectory
                        );
                        RemoveMod(newModInfo.Id);
                        AddMod(newModInfo);
                        RefreshGroupModCount();
                        StartRemindSaveThread();
                        Logger.Record(
                            $"{I18nRes.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}"
                        );
                    }
                }
                else
                {
                    Utils.CopyDirectory(Path.GetDirectoryName(jsonPath)!, GameInfo.ModsDirectory);
                    AddMod(newModInfo);
                    RefreshGroupModCount();
                }
                RefreshShowMods();
            }
            else
            {
                Logger.Record($"{I18nRes.ZipFileError} {I18nRes.Path}: {filePath}");
                MessageBoxVM.Show(new($"{I18nRes.ZipFileError}\n{I18nRes.Path}: {filePath}"));
            }
            dirs.Delete(true);
        }

        private void RemoveMod(string id)
        {
            var modInfo = allModInfos[id];
            allModInfos.Remove(id);
            RemoveModShowInfo(id);
            Logger.Record($"{I18nRes.RemoveMod} {id} {modInfo.Version}", LogLevel.DEBUG);
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
            Logger.Record($"{I18nRes.RemoveMod} {modInfo.Id} {modInfo.Version}", LogLevel.DEBUG);
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
            Logger.Record($"{I18nRes.AddMod} {showInfo.Id} {showInfo.Version}", LogLevel.DEBUG);
        }

        private void ClearDataGridSelected()
        {
            //NowShowModes;
        }

        internal bool TryAddUserGroup(string icon, string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && !allUserGroups.ContainsKey(name))
            {
                if (name == ModTypeGroup.Collected || name == strUserCustomData)
                    MessageBoxVM.Show(new(string.Format(I18nRes.UserGroupCannotNamed, ModTypeGroup.Collected, strUserCustomData)) { Tag = false });
                else
                {
                    AddUserGroup(icon, name);
                    return true;
                }
            }
            else
                MessageBoxVM.Show(new(I18nRes.UserGroupNamingFailed));
            return false;
        }

        private void AddUserGroup(string icon, string name)
        {
            ListBoxItemVM listBoxItem = new();
            // 调用全局资源需要写全
            SetListBoxItemData(ref listBoxItem, name);
            ContextMenuVM contextMenu = new();
            // 重命名分组
            MenuItemVM menuItem = new();
            menuItem.Header = I18nRes.RenameUserGroup;
            menuItem.CommandEvent += (p) =>
            {
                RenameUserGroup(listBoxItem);
            };
            contextMenu.Add(menuItem);
            Logger.Record($"{I18nRes.AddMenuItem} {menuItem.Header}", LogLevel.DEBUG);
            // 删除分组
            menuItem = new();
            menuItem.Header = I18nRes.RemoveUserGroup;
            menuItem.CommandEvent += (p) =>
            {
                RemoveUserGroup(listBoxItem);
            };
            contextMenu.Add(menuItem);
            Logger.Record($"{I18nRes.AddMenuItem} {menuItem.Header}", LogLevel.DEBUG);

            listBoxItem.ContextMenu = contextMenu;
            listBoxItem.Icon = icon;
            ListBox_UserGroupMenu.Add(listBoxItem);
            allUserGroups.Add(name, new());
            allListBoxItems.Add(name, listBoxItem);
            allModShowInfoGroups.Add(name, new());
            ComboBox_ExportUserGroup.Add(new() { Content = name, Tag = name });
            RefreshGroupModCount();
            RefreshModsContextMenu();
            StartRemindSaveThread();
            Logger.Record($"{I18nRes.AddUserGroup} {icon} {name}");
        }

        private void RemoveUserGroup(ListBoxItemVM listBoxItem)
        {
            if (
                MessageBoxVM.Show(
                    new(I18nRes.ConfirmUserGroupDeletion)
                    {
                        Button = MessageBoxVM.Button.YesNo,
                        Icon = MessageBoxVM.Icon.Question
                    }
                ) is MessageBoxVM.Result.No
            )
                return;
            var name = listBoxItem!.Tag!.ToString()!;
            if (nowSelectedGroup == listBoxItem)
                ListBox_TypeGroupMenu.SelectedIndex = 0;
            ListBox_UserGroupMenu.Remove(listBoxItem);
            allUserGroups.Remove(name);
            allListBoxItems.Remove(name);
            allModShowInfoGroups.Remove(name);
            RefreshModsContextMenu();
            StartRemindSaveThread();
            // 删除导出用户分组下拉列表的此分组选择
            if (ComboBox_ExportUserGroup.SelectedItem!.Tag!.ToString() == name)
            {
                ComboBox_ExportUserGroup.Remove(ComboBox_ExportUserGroup.SelectedItem);
                ComboBox_ExportUserGroup.SelectedIndex = 0;
            }
        }

        private void RenameUserGroup(ListBoxItemVM listBoxItem)
        {
            string icon = listBoxItem.Icon.ToString()!;
            string name = listBoxItem.ToolTip.ToString()!;
            // TODO: 使用了window 需移至外部
            AddUserGroup window = new();
            window.TextBox_Icon.Text = icon;
            window.TextBox_Name.Text = name;
            window.Button_Yes.Click += (s, e) =>
            {
                string _icon = window.TextBox_Icon.Text;
                string _name = window.TextBox_Name.Text;
                if (_name == ModTypeGroup.Collected || _name == strUserCustomData)
                {
                    MessageBoxVM.Show(
                        new(
                            string.Format(
                                I18nRes.UserGroupCannotNamed,
                                ModTypeGroup.Collected,
                                strUserCustomData
                            )
                        )
                        {
                            Icon = MessageBoxVM.Icon.Warning,
                            Tag = true
                        }
                    );
                    return;
                }
                if (name == _name || !allUserGroups.ContainsKey(_name))
                {
                    listBoxItem.Icon = _icon;
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
                    SetListBoxItemData(ref listBoxItem, _name);
                    RefreshGroupModCount();
                    RefreshModsContextMenu();
                    StartRemindSaveThread();
                }
                else
                    MessageBoxVM.Show(new(I18nRes.UserGroupNamingFailed));
            };
            window.Button_Cancel.Click += (s, e) => window.Close();
            window.ShowDialog();
        }

        private void SetListBoxItemData(ref ListBoxItemVM item, string name)
        {
            item.Content = name;
            item.ToolTip = name;
            item.Tag = name;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private async void RemindSave()
        {
            while (remindSaveThread.ThreadState is not ThreadState.Unstarted)
            {
                IsRemindSave = true;
                await Task.Delay(1000);
                IsRemindSave = false;
                await Task.Delay(1000);
            }
        }

        private void StartRemindSaveThread()
        {
            if (remindSaveThread.ThreadState is ThreadState.Unstarted)
                remindSaveThread.Start();
        }

        private void ResetRemindSaveThread()
        {
            if (remindSaveThread.ThreadState is not ThreadState.Unstarted)
                remindSaveThread.Join(1);
            remindSaveThread = new(RemindSave);
        }
    }
}