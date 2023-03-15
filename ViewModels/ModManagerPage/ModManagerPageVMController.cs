using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using HKW.Extension;
using HKW.Libs.Log4Cs;
using HKW.Libs.TomlParse;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialogs;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using StarsectorTools.Resources;
using I18nRes = StarsectorTools.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorTools.ViewModels.ModManagerPage
{
    internal partial class ModManagerPageViewModel
    {
        private static readonly string _UserDataFile = $"{ST.CoreDirectory}\\UserData.toml";
        private static readonly string _UserGroupFile = $"{ST.CoreDirectory}\\UserGroup.toml";
        private static readonly string _BackupDirectory = $"{ST.CoreDirectory}\\Backup";
        private static readonly string _BackupModsDirectory = $"{_BackupDirectory}\\Mods";
        private const string _ModInfoFile = "mod_info.json";
        private const string _StrEnabledMods = "enabledMods";
        private const string _StrAll = "All";
        private const string _StrId = "Id";
        private const string _StrIcon = "Icon";
        private const string _StrMods = "Mods";
        private const string _StrUserCustomData = "UserCustomData";

        /// <summary>已启用的模组ID</summary>
        private readonly HashSet<string> _allEnabledModsId = new();

        /// <summary>已收藏的模组ID</summary>
        private readonly HashSet<string> _allCollectedModsId = new();

        /// <summary>
        /// <para>全部分组列表项</para>
        /// <para><see langword="Key"/>: 列表项Tag或ModGroupType</para>
        /// <para><see langword="Value"/>: 列表项</para>
        /// </summary>
        private readonly Dictionary<string, ListBoxItemVM> _allListBoxItems = new();

        /// <summary>
        /// <para>全部模组信息</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 模组信息</para>
        /// </summary>
        private readonly Dictionary<string, ModInfo> _allModInfos = new();

        /// <summary>
        /// <para>全部模组显示信息</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 模组显示信息</para>
        /// </summary>
        private readonly Dictionary<string, ModShowInfo> _allModsShowInfo = new();

        /// <summary>
        /// <para>全部模组所在的类型分组</para>
        /// <para><see langword="Key"/>: 模组ID</para>
        /// <para><see langword="Value"/>: 所在分组</para>
        /// </summary>
        private readonly Dictionary<string, string> _allModsTypeGroup = new();

        /// <summary>
        /// <para>全部用户分组</para>
        /// <para><see langword="Key"/>: 分组名称</para>
        /// <para><see langword="Value"/>: 包含的模组</para>
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _allUserGroups = new();

        /// <summary>
        /// <para>全部分组包含的模组显示信息列表</para>
        /// <para><see langword="Key"/>: 分组名称</para>
        /// <para><see langword="Value"/>: 包含的模组显示信息的列表</para>
        /// </summary>
        private readonly Dictionary<
            string,
            ObservableCollection<ModShowInfo>
        > _allModShowInfoGroups =
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
            // 设置外部API
            ModInfos.AllModInfos = _allModInfos;
            ModInfos.AllEnabledModIds = _allEnabledModsId;
            ModInfos.AllCollectedModIds = _allCollectedModsId;
            ModInfos.AllUserGroups = _allUserGroups.AsReadOnly<
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
            //RefreshModsContextMenu();
            RefreshGroupModCount();
            GC.Collect();
        }

        private void GetAllModsInfo()
        {
            // TODO: 获取模组文件夹的最近更新日期,显示到DataGrid中
            // TODO: 可以保存游戏版本,启用列表以及模组本体的完全备份包
            int errSize = 0;
            StringBuilder errSB = new();
            DirectoryInfo dirInfo = new(GameInfo.ModsDirectory);
            foreach (var dir in dirInfo.GetDirectories())
            {
                if (ModInfo.Parse($"{dir.FullName}\\{_ModInfoFile}") is not ModInfo info)
                {
                    errSize++;
                    errSB.AppendLine(dir.FullName);
                    continue;
                }
                _allModInfos.Add(info.Id, info);
                Logger.Debug($"{I18nRes.ModAddSuccess}: {dir.FullName}");
            }
            Logger.Info(string.Format(I18nRes.ModAddCompleted, _allModInfos.Count, errSize));
            if (errSB.Length > 0)
                MessageBoxVM.Show(
                    new($"{I18nRes.ModAddFailed} {I18nRes.Size}: {errSize}\n{errSB}")
                    {
                        Icon = MessageBoxVM.Icon.Warning
                    }
                );
        }

        private void GetAllModsShowInfo()
        {
            foreach (var modInfo in _allModInfos.Values)
                AddModShowInfo(modInfo, false);
            Logger.Info($"{I18nRes.ModShowInfoSetSuccess} {I18nRes.Size}: {_allModInfos.Count}");
            //ListBox_ModsGroupMenu.SelectedIndex = 0;
        }

        private ModShowInfo CreateModShowInfo(ModInfo info)
        {
            return new ModShowInfo(info)
            {
                IsCollected = _allCollectedModsId.Contains(info.Id),
                IsEnabled = _allEnabledModsId.Contains(info.Id),
                MissDependencies = false,
                ImageSource = GetImage($"{info.ModDirectory}\\icon.ico"),
            };
            static BitmapImage? GetImage(string filePath)
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
                    Logger.Error($"{I18nRes.IconLoadError} {I18nRes.Path}: {filePath}", ex);
                    return null;
                }
            }
        }

        #region AddMod

        private void AddMod(ModInfo modInfo)
        {
            _allModInfos.Add(modInfo.Id, modInfo);
            AddModShowInfo(modInfo);
            Logger.Debug($"{I18nRes.AddMod} {modInfo.Id} {modInfo.Version}");
        }

        private void AddModShowInfo(ModInfo modInfo, bool createContextMenu = true)
        {
            if (_allModsShowInfo.ContainsKey(modInfo.Id))
                return;
            ModShowInfo showInfo = CreateModShowInfo(modInfo);
            // 添加至总分组
            _allModsShowInfo.Add(showInfo.Id, showInfo);
            _allModShowInfoGroups[ModTypeGroup.All].Add(showInfo);
            // 添加至类型分组
            _allModShowInfoGroups[CheckTypeGroup(modInfo.Id)].Add(showInfo);
            // 添加至已启用或已禁用分组
            if (showInfo.IsEnabled)
                _allModShowInfoGroups[ModTypeGroup.Enabled].Add(showInfo);
            else
                _allModShowInfoGroups[ModTypeGroup.Disabled].Add(showInfo);
            // 添加至已收藏分组
            if (showInfo.IsCollected)
                _allModShowInfoGroups[ModTypeGroup.Collected].Add(showInfo);
            // 添加至用户分组
            foreach (var userGroup in _allUserGroups)
            {
                if (userGroup.Value.Contains(modInfo.Id))
                {
                    userGroup.Value.Add(modInfo.Id);
                    _allModShowInfoGroups[userGroup.Key].Add(showInfo);
                }
            }
            if (createContextMenu)
                showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            Logger.Debug($"{I18nRes.AddMod} {showInfo.Id} {showInfo.Version}");
        }

        #endregion AddMod

        #region RemoveMod

        private void RemoveMod(string id)
        {
            var modInfo = _allModInfos[id];
            _allModInfos.Remove(id);
            RemoveModShowInfo(id);
            Logger.Debug($"{I18nRes.RemoveMod} {id} {modInfo.Version}");
        }

        private void RemoveModShowInfo(string id)
        {
            var modShowInfo = _allModsShowInfo[id];
            // 从总分组中删除
            _allModsShowInfo.Remove(id);
            _allModShowInfoGroups[ModTypeGroup.All].Remove(modShowInfo);
            // 从类型分组中删除
            _allModShowInfoGroups[CheckTypeGroup(id)].Remove(modShowInfo);
            // 从已启用或已禁用分组中删除
            if (modShowInfo.IsEnabled)
            {
                _allEnabledModsId.Remove(id);
                _allModShowInfoGroups[ModTypeGroup.Enabled].Remove(modShowInfo);
            }
            else
                _allModShowInfoGroups[ModTypeGroup.Disabled].Remove(modShowInfo);
            // 从已收藏中删除
            if (modShowInfo.IsCollected)
            {
                _allCollectedModsId.Remove(id);
                _allModShowInfoGroups[ModTypeGroup.Collected].Remove(modShowInfo);
            }
            // 从用户分组中删除
            foreach (var userGroup in _allUserGroups)
            {
                if (userGroup.Value.Contains(id))
                {
                    userGroup.Value.Remove(id);
                    _allModShowInfoGroups[userGroup.Key].Remove(modShowInfo);
                }
            }
        }

        #endregion RemoveMod

        #region CreateModShowContextMenu

        private ContextMenuVM CreateModShowContextMenu(ModShowInfo modShowInfo)
        {
            Logger.Debug($"{modShowInfo.Id} {I18nRes.AddContextMenu}");
            ContextMenuVM contextMenu =
                new(
                    (list) =>
                    {
                        list.Add(EnableOrDisableSelectedMods());
                        list.Add(CollectOrUncollectSelectedMods());
                        list.Add(OpenModDirectory());
                        list.Add(DeleteMod());
                        if (AddToUserGroup() is MenuItemVM menuItem1)
                            list.Add(menuItem1);
                        if (RemoveToUserGroup() is MenuItemVM menuItem2)
                            list.Add(menuItem2);
                    }
                );
            return contextMenu;
            MenuItemVM EnableOrDisableSelectedMods()
            {
                // 启用或禁用
                MenuItemVM menuItem = new();
                menuItem.Header = modShowInfo.IsEnabled
                    ? I18nRes.DisableSelectedMods
                    : I18nRes.EnableSelectedMods;
                menuItem.CommandEvent += (p) => ChangeSelectedModsEnabled();
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM CollectOrUncollectSelectedMods()
            {
                // 收藏或取消收藏
                MenuItemVM menuItem = new();
                menuItem.Header = modShowInfo.IsCollected
                    ? I18nRes.UncollectSelectedMods
                    : I18nRes.CollectSelectedMods;
                menuItem.CommandEvent += (p) => ChangeSelectedModsCollected();
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM OpenModDirectory()
            {
                // 打开模组文件夹
                MenuItemVM menuItem = new();
                menuItem.Header = I18nRes.OpenModDirectory;
                menuItem.CommandEvent += (p) =>
                {
                    Logger.Info(
                        $"{I18nRes.OpenModDirectory} {I18nRes.Path}: {_allModInfos[modShowInfo.Id].ModDirectory}"
                    );
                    Utils.OpenLink(_allModInfos[modShowInfo.Id].ModDirectory);
                };
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM DeleteMod()
            {
                // 删除模组至回收站
                MenuItemVM menuItem = new();
                menuItem.Header = I18nRes.DeleteMod;
                menuItem.CommandEvent += (p) =>
                {
                    string path = _allModInfos[modShowInfo.Id].ModDirectory;
                    if (
                        MessageBoxVM.Show(
                            new(
                                $"{I18nRes.ConfirmModDeletion}?\nID: {modShowInfo.Id}\n{I18nRes.Path}: {path}\n"
                            )
                            {
                                Button = MessageBoxVM.Button.YesNo,
                                Icon = MessageBoxVM.Icon.Warning
                            }
                        )
                        is not MessageBoxVM.Result.Yes
                    )
                        return;
                    Logger.Info(
                        $"{I18nRes.ConfirmModDeletion}?\nID: {modShowInfo.Id}\n{I18nRes.Path}: {path}\n"
                    );
                    RemoveMod(modShowInfo.Id);
                    CheckFilterAndRefreshShowMods();
                    RefreshGroupModCount();
                    CloseModDetails();
                    Utils.DeleteDirectoryToRecycleBin(path);
                    IsRemindSave = true;
                };
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM? AddToUserGroup()
            {
                MenuItemVM? menuItem = null;
                // 添加至用户分组
                if (_allUserGroups.Count == 0)
                    return menuItem;
                menuItem = new();
                menuItem.Header = I18nRes.AddModToUserGroup;
                menuItem.ItemsSource = new();
                foreach (var group in _allUserGroups.Keys)
                {
                    if (_allUserGroups[group].Contains(modShowInfo.Id))
                        continue;
                    MenuItemVM groupItem = new();
                    groupItem.Header = group;
                    groupItem.CommandEvent += (p) =>
                        ChangeUserGroupContainsSelectedMods(group, true);
                    menuItem.Add(groupItem);
                }
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem.Any() ? menuItem : null;
                ;
            }
            MenuItemVM? RemoveToUserGroup()
            {
                MenuItemVM? menuItem = null;
                // 从用户分组中删除
                var groupContainsMod = _allUserGroups.Where(g => g.Value.Contains(modShowInfo.Id));
                if (!groupContainsMod.Any())
                    return menuItem;
                menuItem = new();
                menuItem.Header = I18nRes.RemoveFromUserGroup;
                menuItem.ItemsSource = new();
                foreach (var group in groupContainsMod)
                {
                    MenuItemVM groupItem = new();
                    groupItem.Header = group.Key;
                    groupItem.CommandEvent += (p) =>
                        ChangeUserGroupContainsSelectedMods(group.Key, false);
                    menuItem.Add(groupItem);
                }
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem.Any() ? menuItem : null;
            }
        }

        #endregion CreateModShowContextMenu

        #region CheckEnabledMods

        private void CheckEnabledMods()
        {
            if (Utils.FileExists(GameInfo.EnabledModsJsonFile))
                TryGetEnabledMods(GameInfo.EnabledModsJsonFile);
            else
                SaveEnabledMods(GameInfo.EnabledModsJsonFile);
        }

        private void TryGetEnabledMods(string filePath, bool importMode = false)
        {
            try
            {
                StringBuilder errSB = new();
                if (Utils.JsonParse2Object(filePath) is not JsonObject enabledModsJson)
                    throw new();
                if (enabledModsJson.Count != 1 || !enabledModsJson.ContainsKey(_StrEnabledMods))
                    throw new();
                if (importMode && EnabledModListImportMode() is false)
                    return;
                if (
                    enabledModsJson[_StrEnabledMods]?.AsArray()
                    is not JsonArray enabledModsJsonArray
                )
                    throw new();
                Logger.Info($"{I18nRes.LoadEnabledModsFile} {I18nRes.Path}: {filePath}");
                if (GetEnabledMods(enabledModsJsonArray) is StringBuilder err)
                {
                    MessageBoxVM.Show(
                        new($"{I18nRes.EnabledModsFile}: {filePath} {I18nRes.NotFoundMod}\n{err}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                }
                Logger.Info($"{I18nRes.EnableMod} {I18nRes.Size}: {_allEnabledModsId.Count}");
            }
            catch
            {
                Logger.Error($"{I18nRes.LoadError} {I18nRes.Path}: {filePath}");
                MessageBoxVM.Show(
                    new($"{I18nRes.LoadError}\n{I18nRes.Path}: {filePath}")
                    {
                        Icon = MessageBoxVM.Icon.Error
                    }
                );
            }
        }

        private StringBuilder? GetEnabledMods(JsonArray array)
        {
            StringBuilder err = new();
            foreach (var modId in array)
            {
                var id = modId!.GetValue<string>();
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (!_allModInfos.ContainsKey(id))
                {
                    Logger.Warring($"{I18nRes.NotFoundMod} {id}");
                    err.AppendLine(id);
                    continue;
                }
                ChangeModEnabled(id, true);
                Logger.Debug($"{I18nRes.EnableMod} {id}");
            }
            return err.Length > 0 ? err : null;
        }

        private bool EnabledModListImportMode()
        {
            var result = MessageBoxVM.Show(
                new(I18nRes.SelectImportMode)
                {
                    Button = MessageBoxVM.Button.YesNoCancel,
                    Icon = MessageBoxVM.Icon.Question
                }
            );
            if (result is MessageBoxVM.Result.Yes)
                ClearAllEnabledMods();
            else if (result is MessageBoxVM.Result.Cancel)
                return false;
            return true;
        }

        #endregion CheckEnabledMods

        #region GetUserData

        private void CheckUserData()
        {
            if (Utils.FileExists(_UserDataFile))
                GetUserData(_UserDataFile);
            else
                SaveUserData(_UserDataFile);
            if (Utils.FileExists(_UserGroupFile))
                GetAllUserGroup(_UserGroupFile);
            else
                SaveAllUserGroup(_UserGroupFile);
        }

        private void GetAllUserGroup(string filePath)
        {
            Logger.Info($"{I18nRes.LoadUserGroup} {I18nRes.Path}: {filePath}");
            try
            {
                StringBuilder errSB = new();
                TomlTable toml = TOML.Parse(filePath);
                foreach (var kv in toml)
                {
                    if (kv.Key == ModTypeGroup.Collected || kv.Key == _StrUserCustomData)
                        continue;
                    string group = kv.Key;
                    if (_allUserGroups.ContainsKey(group))
                    {
                        Logger.Info($"{I18nRes.DuplicateUserGroupName} {group}");
                        errSB.AppendLine($"{I18nRes.DuplicateUserGroupName} {group}");
                        continue;
                    }
                    AddUserGroup(kv.Value[_StrIcon]!, group, false);
                    if (
                        GetModsInUserGroup(group, kv.Value[_StrMods].AsTomlArray)
                        is StringBuilder err
                    )
                        errSB.Append($"{I18nRes.UserGroup}: {group} {I18nRes.NotFoundMod}:\n{err}");
                }
                if (errSB.Length > 0)
                    MessageBoxVM.Show(new(errSB.ToString()) { Icon = MessageBoxVM.Icon.Warning });
            }
            catch (Exception ex)
            {
                Logger.Error($"{I18nRes.FileError} {filePath}", ex);
                MessageBoxVM.Show(
                    new($"{I18nRes.FileError} {filePath}") { Icon = MessageBoxVM.Icon.Error }
                );
            }
        }

        private StringBuilder? GetModsInUserGroup(string group, TomlArray array)
        {
            StringBuilder err = new();
            foreach (string id in array)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (!_allModsShowInfo.ContainsKey(id))
                {
                    Logger.Warring($"{I18nRes.NotFoundMod} {id}");
                    err.AppendLine(id);
                    continue;
                }
                if (_allUserGroups[group].Add(id))
                    _allModShowInfoGroups[group].Add(_allModsShowInfo[id]);
            }
            return err.Length > 0 ? err : null;
        }

        private void GetUserData(string filePath)
        {
            Logger.Info($"{I18nRes.LoadUserData} {I18nRes.Path}: {filePath}");
            try
            {
                TomlTable toml = TOML.Parse(filePath);
                if (
                    GetUserCollectedMods(toml[ModTypeGroup.Collected].AsTomlArray)
                    is StringBuilder err
                )
                {
                    MessageBoxVM.Show(
                        new($"{I18nRes.CollectedModList} {I18nRes.NotFoundMod}\n{err}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                }
                if (GetUserCustomData(toml[_StrUserCustomData].AsTomlArray) is StringBuilder err1)
                {
                    MessageBoxVM.Show(
                        new($"{I18nRes.UserCustomData} {I18nRes.NotFoundMod}\n{err1}")
                        {
                            Icon = MessageBoxVM.Icon.Warning
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{I18nRes.UserDataLoadError} {I18nRes.Path}: {filePath}", ex);
                MessageBoxVM.Show(
                    new($"{I18nRes.UserDataLoadError}\n{I18nRes.Path}: {filePath}")
                    {
                        Icon = MessageBoxVM.Icon.Error
                    }
                );
            }
        }

        private StringBuilder? GetUserCollectedMods(TomlArray array)
        {
            Logger.Info(I18nRes.LoadCollectedModList);
            StringBuilder errSB = new();
            foreach (string id in array)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (!_allModsShowInfo.ContainsKey(id))
                {
                    Logger.Warring($"{I18nRes.NotFoundMod} {id}");
                    errSB.AppendLine(id);
                    continue;
                }
                ChangeModCollected(id, true);
            }
            return errSB.Length > 0 ? errSB : null;
        }

        private StringBuilder? GetUserCustomData(TomlArray array)
        {
            Logger.Info(I18nRes.LoadUserCustomData);
            StringBuilder err = new();
            foreach (var dict in array)
            {
                var id = dict[_StrId].AsString;
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (!_allModsShowInfo.ContainsKey(id))
                {
                    Logger.Warring($"{I18nRes.NotFoundMod} {id}");
                    err.AppendLine(id);
                    continue;
                }
                var info = _allModsShowInfo[id];
                info.UserDescription = dict[nameof(ModShowInfo.UserDescription)];
            }
            return err.Length > 0 ? err : null;
        }

        #endregion GetUserData

        private void GetAllListBoxItems()
        {
            foreach (var item in ListBox_MainMenu)
                _allListBoxItems.Add(item.Tag!.ToString()!, item);
            foreach (var item in ListBox_TypeGroupMenu)
                _allListBoxItems.Add(item.Tag!.ToString()!, item);
            foreach (var item in ListBox_UserGroupMenu)
                _allListBoxItems.Add(item.Tag!.ToString()!, item);
            Logger.Info(I18nRes.ListBoxItemsRetrievalCompleted);
        }

        #region TypeGroup

        private void GetTypeGroup()
        {
            using StreamReader sr = ResourceDictionary.GetResourceStream(
                ResourceDictionary.ModTypeGroup_toml
            );
            ;
            TomlTable toml = TOML.Parse(sr);
            foreach (var kv in toml)
                foreach (string id in kv.Value.AsTomlArray)
                    _allModsTypeGroup.Add(id, kv.Key);
            Logger.Info(I18nRes.TypeGroupRetrievalCompleted);
        }

        private string CheckTypeGroup(string id)
        {
            return _allModsTypeGroup.ContainsKey(id)
                ? _allModsTypeGroup[id]
                : ModTypeGroup.UnknownMods;
        }

        #endregion TypeGroup

        #region RefreshDisplayData

        private void CheckAndRefreshDisplayData(string group = "")
        {
            if (!string.IsNullOrWhiteSpace(group))
            {
                if (NowSelectedGroupName == group)
                {
                    CheckFilterAndRefreshShowMods();
                    ShowSpin = false;
                }
                RefreshGroupModCount();
            }
            else
            {
                CheckFilterAndRefreshShowMods();
                RefreshGroupModCount();
            }
        }

        private void CheckFilterAndRefreshShowMods()
        {
            var text = ModFilterText;
            var type = ComboBox_ModFilterType.SelectedItem!.Tag!.ToString()!;
            if (!string.IsNullOrWhiteSpace(text))
            {
                RefreshNowShowMods(GetFilterModsShowInfo(text, type));
                Logger.Info($"{I18nRes.SearchMod} {text}");
            }
            else
            {
                RefreshNowShowMods(_allModShowInfoGroups[NowSelectedGroupName]);
                Logger.Info($"{I18nRes.ShowGroup} {NowSelectedGroupName}");
            }
        }

        private void RefreshNowShowMods(ObservableCollection<ModShowInfo> infos)
        {
            NowShowMods = infos;
        }

        private ObservableCollection<ModShowInfo> GetFilterModsShowInfo(string text, string type) =>
            new(
                type switch
                {
                    nameof(ModShowInfo.Name)
                        => _allModShowInfoGroups[NowSelectedGroupName].Where(
                            i => i.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    nameof(ModShowInfo.Id)
                        => _allModShowInfoGroups[NowSelectedGroupName].Where(
                            i => i.Id.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    nameof(ModShowInfo.Author)
                        => _allModShowInfoGroups[NowSelectedGroupName].Where(
                            i => i.Author.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    nameof(ModShowInfo.UserDescription)
                        => _allModShowInfoGroups[NowSelectedGroupName].Where(
                            i =>
                                i.UserDescription.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    _ => null!
                }
            );

        private void RefreshGroupModCount()
        {
            foreach (var item in _allListBoxItems.Values)
            {
                int size = _allModShowInfoGroups[item!.Tag!.ToString()!].Count;
                item.Content = $"{item.ToolTip} ({size})";
                Logger.Debug($"{I18nRes.ModCountInGroupRefresh} {item.Content}");
            }
            Logger.Debug(I18nRes.ModCountInGroupRefreshCompleted);
        }

        #endregion RefreshDisplayData

        private void RefreshModsContextMenu()
        {
            foreach (var showInfo in _allModsShowInfo.Values)
                showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            Logger.Info(
                $"{I18nRes.ContextMenuRefreshCompleted} {I18nRes.Size}: {_allModsShowInfo.Values.Count}"
            );
        }

        #region ChangeUserGroupContainsSelectedMods

        private void ChangeUserGroupContainsSelectedMods(string group, bool isInGroup)
        {
            int count = _nowSelectedMods.Count;
            for (int i = 0; i < _nowSelectedMods.Count; )
            {
                ChangeUserGroupContainsSelectedMod(group, _nowSelectedMods[i].Id, isInGroup);
                // 如果已选择数量没有变化,则继续下一个选项
                if (count == _nowSelectedMods.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (count != _nowSelectedMods.Count)
                CloseModDetails();
            CheckAndRefreshDisplayData(group);
            IsRemindSave = true;
        }

        private void ChangeUserGroupContainsSelectedMod(string group, string id, bool isInGroup)
        {
            ModShowInfo showInfo = _allModsShowInfo[id];
            if (isInGroup)
            {
                if (_allUserGroups[group].Add(id))
                {
                    _allModShowInfoGroups[group].Add(_allModsShowInfo[id]);
                    Logger.Debug($"{id} {I18nRes.AddModToUserGroup} {group}");
                }
            }
            else
            {
                if (_allUserGroups[group].Remove(id))
                {
                    _allModShowInfoGroups[group].Remove(_allModsShowInfo[id]);
                    Logger.Debug($"{id} {I18nRes.RemoveFromUserGroup} {group}");
                }
            }
            showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
        }

        #endregion ChangeUserGroupContainsSelectedMods

        #region ChangeModEnabled

        private void ChangeSelectedModsEnabled(bool? enabled = null)
        {
            int count = _nowSelectedMods.Count;
            for (int i = 0; i < _nowSelectedMods.Count; )
            {
                ChangeModEnabled(_nowSelectedMods[i].Id, enabled);
                // 如果已选择数量没有变化,则继续下一个选项
                if (count == _nowSelectedMods.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (count != _nowSelectedMods.Count)
                CloseModDetails();
            CheckAndRefreshDisplayData(nameof(ModTypeGroup.Enabled));
            CheckEnabledModsDependencies();
            IsRemindSave = true;
        }

        private void ClearAllEnabledMods()
        {
            while (_allEnabledModsId.Count > 0)
                ChangeModEnabled(_allEnabledModsId.ElementAt(0), false);
            Logger.Info(I18nRes.DisableAllEnabledMods);
        }

        private void ChangeModEnabled(string id, bool? enabled = null)
        {
            ModShowInfo showInfo = _allModsShowInfo[id];
            showInfo.IsEnabled = (bool)(enabled is null ? !showInfo.IsEnabled : enabled);
            showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            if (showInfo.IsEnabled is true)
            {
                if (_allEnabledModsId.Add(showInfo.Id))
                {
                    _allModShowInfoGroups[ModTypeGroup.Enabled].Add(showInfo);
                    _allModShowInfoGroups[ModTypeGroup.Disabled].Remove(showInfo);
                }
            }
            else
            {
                if (_allEnabledModsId.Remove(showInfo.Id))
                {
                    _allModShowInfoGroups[ModTypeGroup.Enabled].Remove(showInfo);
                    _allModShowInfoGroups[ModTypeGroup.Disabled].Add(showInfo);
                    showInfo.MissDependencies = false;
                }
            }
            Logger.Debug($"{id} {I18nRes.ChangeEnabledStateTo} {showInfo.IsEnabled}");
        }

        #endregion ChangeModEnabled

        private void CheckEnabledModsDependencies()
        {
            foreach (var showInfo in _allModShowInfoGroups[ModTypeGroup.Enabled])
            {
                if (showInfo.DependenciesSet is null)
                    continue;
                showInfo.MissDependenciesMessage = string.Join(
                    " , ",
                    showInfo.DependenciesSet
                        .Where(s => !_allEnabledModsId.Contains(s.Id))
                        .Select(s => s.Name)
                );
                if (string.IsNullOrWhiteSpace(showInfo.MissDependenciesMessage))
                    showInfo.MissDependencies = false;
                else
                {
                    Logger.Info(
                        $"{showInfo.Id} {I18nRes.NotEnableDependencies} {showInfo.DependenciesSet}"
                    );
                    showInfo.MissDependencies = true;
                }
            }
        }

        #region ChangeModCollected

        private void ChangeSelectedModsCollected(bool? collected = null)
        {
            int count = _nowSelectedMods.Count;
            for (int i = 0; i < _nowSelectedMods.Count; )
            {
                ChangeModCollected(_nowSelectedMods[i].Id, collected);
                if (count == _nowSelectedMods.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (count != _nowSelectedMods.Count)
                CloseModDetails();
            CheckAndRefreshDisplayData(nameof(ModTypeGroup.Collected));
            IsRemindSave = true;
        }

        private void ChangeModCollected(string id, bool? collected = null)
        {
            ModShowInfo showInfo = _allModsShowInfo[id];
            showInfo.IsCollected = (bool)(collected is null ? !showInfo.IsCollected : collected);
            showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            if (showInfo.IsCollected is true)
            {
                if (_allCollectedModsId.Add(showInfo.Id))
                    _allModShowInfoGroups[ModTypeGroup.Collected].Add(showInfo);
            }
            else
            {
                if (_allCollectedModsId.Remove(showInfo.Id))
                    _allModShowInfoGroups[ModTypeGroup.Collected].Remove(showInfo);
            }
            Logger.Debug($"{id} {I18nRes.ChangeCollectStateTo} {showInfo.IsCollected}");
        }

        #endregion ChangeModCollected

        #region SaveAllData

        private void SaveAllData()
        {
            SaveEnabledMods(GameInfo.EnabledModsJsonFile);
            SaveUserData(_UserDataFile);
            SaveAllUserGroup(_UserGroupFile);
        }

        private void SaveEnabledMods(string filePath)
        {
            JsonObject jsonObject = new() { [_StrEnabledMods] = new JsonArray() };
            foreach (var mod in _allEnabledModsId)
                jsonObject[_StrEnabledMods]!.AsArray().Add(mod);
            jsonObject.SaveTo(filePath);
            Logger.Info($"{I18nRes.EnabledListSaveCompleted} {I18nRes.Path}: {filePath}");
        }

        private void SaveUserData(string filePath)
        {
            TomlTable toml =
                new()
                {
                    [ModTypeGroup.Collected] = new TomlArray(),
                    [_StrUserCustomData] = new TomlArray(),
                };
            foreach (var info in _allModsShowInfo.Values)
            {
                if (info.IsCollected is true)
                    toml[ModTypeGroup.Collected].Add(info.Id);
                if (info.UserDescription!.Length > 0)
                {
                    toml[_StrUserCustomData].Add(
                        new TomlTable()
                        {
                            [nameof(ModShowInfo.Id)] = info.Id,
                            [nameof(ModShowInfo.UserDescription)] =
                                info.UserDescription!.Length > 0 ? info.UserDescription : "",
                        }
                    );
                }
            }
            toml.SaveTo(filePath);
            Logger.Info($"{I18nRes.SaveUserDataSuccess} {I18nRes.Path}: {filePath}");
        }

        private void SaveAllUserGroup(string filePath, string group = _StrAll)
        {
            TomlTable toml = new();
            if (group == _StrAll)
            {
                foreach (var groupData in _allUserGroups)
                    Save(groupData.Key);
            }
            else
            {
                Save(group);
            }
            toml.SaveTo(filePath);
            Logger.Info($"{I18nRes.UserGroupSaveCompleted} {I18nRes.Path}: {filePath}");
            void Save(string name)
            {
                var mods = _allUserGroups[name];
                toml.Add(
                    name,
                    new TomlTable()
                    {
                        [_StrIcon] = _allListBoxItems[name].Icon!.ToString()!,
                        [_StrMods] = new TomlArray(),
                    }
                );
                foreach (var id in mods)
                    toml[name][_StrMods].Add(id);
            }
        }

        #endregion SaveAllData

        #region ModDetails

        private void ChangeShowModDetails(ModShowInfo? info)
        {
            if (info is null)
                CloseModDetails();
            else if (IsShowModDetails && _nowSelectedMod?.Id == info.Id)
                CloseModDetails();
            else
                ShowModDetails(info.Id);
            _nowSelectedMod = info;
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
            Logger.Debug($"{I18nRes.CloseDetails}");
        }

        private void SetModDetails(string id)
        {
            var info = _allModInfos[id];
            var showInfo = _allModsShowInfo[id];
            ModDetailImage = showInfo.ImageSource;
            ModDetailName = showInfo.Name;
            ModDetailId = showInfo.Id;
            ModDetailModVersion = showInfo.Version;
            ModDetailGameVersion = showInfo.GameVersion;
            ModDetailPath = info.ModDirectory;
            ModDetailAuthor = showInfo.Author;
            if (info.DependenciesSet is not null)
            {
                ModDetailDependencies = string.Join(
                    "\n",
                    info.DependenciesSet.Select(
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
            Logger.Debug($"{I18nRes.ShowDetails} {id}");
        }

        #endregion ModDetails

        #region AddUserGroup

        private void InitializeAddUserGroupWindowViewMode(AddUserGroupWindowViewMode viewModel)
        {
            viewModel.OKEvent += () =>
            {
                var icon = viewModel.UserGroupIcon;
                var name = viewModel.UserGroupName;
                if (
                    viewModel.BaseListBoxItem is not null
                    && TryRenameUserGroup(viewModel.BaseListBoxItem!, icon, name)
                )
                    viewModel.Hide();
                else if (TryAddUserGroup(icon, name))
                    viewModel.Hide();
            };
            viewModel.CancelEvent += () =>
            {
                viewModel.Hide();
            };
        }

        private bool TryAddUserGroup(string icon, string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && !_allUserGroups.ContainsKey(name))
            {
                if (name == ModTypeGroup.Collected || name == _StrUserCustomData)
                    MessageBoxVM.Show(
                        new(
                            string.Format(
                                I18nRes.UserGroupCannotNamed,
                                ModTypeGroup.Collected,
                                _StrUserCustomData
                            )
                        )
                        {
                            ShowMainWindowBlurEffect = false
                        }
                    );
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

        private void AddUserGroup(string icon, string name, bool remindSave = true)
        {
            ListBoxItemVM listBoxItem = new();
            SetListBoxItemData(listBoxItem, name);
            listBoxItem.ContextMenu = CreateuserGroupItemContextMenu(listBoxItem);
            listBoxItem.Icon = icon;
            ListBox_UserGroupMenu.Add(listBoxItem);
            _allUserGroups.Add(name, new());
            _allListBoxItems.Add(name, listBoxItem);
            _allModShowInfoGroups.Add(name, new());
            ComboBox_ExportUserGroup.Add(new() { Content = name, Tag = name });
            Logger.Info($"{I18nRes.AddUserGroup} {icon} {name}");
            RefreshGroupModCount();
            RefreshModsContextMenu();
            if (remindSave)
                IsRemindSave = true;
        }

        private ContextMenuVM CreateuserGroupItemContextMenu(ListBoxItemVM listBoxItem)
        {
            return new(
                (list) =>
                {
                    list.Add(EnableAllUserGroupModsMenuItem(listBoxItem));
                    list.Add(DisableAllUserGroupModsMenuItem(listBoxItem));
                    list.Add(CleanAllModsMenuItem(listBoxItem));
                    list.Add(RenameUserGroupMenuItem(listBoxItem));
                    list.Add(RemoveUserGroupMenuItem(listBoxItem));
                }
            );

            MenuItemVM EnableAllUserGroupModsMenuItem(ListBoxItemVM listBoxItem)
            {
                // 启用所有用户分组内模组
                MenuItemVM menuItem = new();
                menuItem.Header = I18nRes.EnableAllMods;
                menuItem.Icon = "✅";
                menuItem.CommandEvent += (p) =>
                {
                    var modIds = _allUserGroups[listBoxItem.ToolTip!.ToString()!];
                    foreach (var id in modIds)
                        ChangeModEnabled(id, true);
                    CheckAndRefreshDisplayData();
                    IsRemindSave = true;
                };
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM DisableAllUserGroupModsMenuItem(ListBoxItemVM listBoxItem)
            {
                // 禁用所有用户分组内模组
                MenuItemVM menuItem = new();
                menuItem.Header = I18nRes.DisableAllMods;
                menuItem.Icon = "❎";
                menuItem.CommandEvent += (p) =>
                {
                    var modIds = _allUserGroups[listBoxItem.ToolTip!.ToString()!];
                    foreach (var id in modIds)
                        ChangeModEnabled(id, false);
                    CheckAndRefreshDisplayData();
                    IsRemindSave = true;
                };
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM CleanAllModsMenuItem(ListBoxItemVM listBoxItem)
            {
                // 清空所有模组
                MenuItemVM menuItem = new();
                menuItem.Header = I18nRes.CleanAllMods;
                menuItem.Icon = "🗑";
                menuItem.CommandEvent += (p) =>
                {
                    var name = listBoxItem.ToolTip!.ToString()!;
                    _allUserGroups[name].Clear();
                    _allModShowInfoGroups[name].Clear();
                    CheckAndRefreshDisplayData();
                    IsRemindSave = true;
                };
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM RenameUserGroupMenuItem(ListBoxItemVM listBoxItem)
            {
                // 重命名分组
                MenuItemVM menuItem = new();
                menuItem.Header = I18nRes.RenameUserGroup;
                menuItem.CommandEvent += (p) => PrepareRenameUserGroup(listBoxItem);
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM RemoveUserGroupMenuItem(ListBoxItemVM listBoxItem)
            {
                // 删除分组
                MenuItemVM menuItem = new();
                menuItem = new();
                menuItem.Header = I18nRes.RemoveUserGroup;
                menuItem.CommandEvent += (p) => RemoveUserGroup(listBoxItem);
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
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
            if (_nowSelectedGroup == listBoxItem)
                ListBox_MainMenu.SelectedIndex = 0;
            ListBox_UserGroupMenu.Remove(listBoxItem);
            _allUserGroups.Remove(name);
            _allListBoxItems.Remove(name);
            _allModShowInfoGroups.Remove(name);
            RefreshModsContextMenu();
            IsRemindSave = true;
            // 删除导出用户分组下拉列表的此分组选择
            if (ComboBox_ExportUserGroup.SelectedItem!.Tag!.ToString() == name)
            {
                ComboBox_ExportUserGroup.Remove(ComboBox_ExportUserGroup.SelectedItem);
                ComboBox_ExportUserGroup.SelectedIndex = 0;
            }
        }

        private void PrepareRenameUserGroup(ListBoxItemVM listBoxItem)
        {
            string icon = listBoxItem.Icon!.ToString()!;
            string name = listBoxItem.ToolTip!.ToString()!;
            AddUserGroupWindow.UserGroupIcon = icon;
            AddUserGroupWindow.UserGroupName = name;
            AddUserGroupWindow.BaseListBoxItem = listBoxItem;
            AddUserGroupWindow.ShowDialog();
        }

        private bool TryRenameUserGroup(ListBoxItemVM listBoxItem, string newIcon, string newName)
        {
            if (newName == ModTypeGroup.Collected || newName == _StrUserCustomData)
            {
                MessageBoxVM.Show(
                    new(
                        string.Format(
                            I18nRes.UserGroupCannotNamed,
                            ModTypeGroup.Collected,
                            _StrUserCustomData
                        )
                    )
                    {
                        Icon = MessageBoxVM.Icon.Warning,
                        ShowMainWindowBlurEffect = false
                    }
                );
                return false;
            }
            if (_allUserGroups.ContainsKey(newName))
            {
                MessageBoxVM.Show(new(I18nRes.UserGroupNamingFailed));
                return false;
            }
            RenameUserGroup(listBoxItem, newIcon, newName);
            return true;
        }

        private void RenameUserGroup(ListBoxItemVM listBoxItem, string newIcon, string newName)
        {
            string name = listBoxItem.ToolTip!.ToString()!;
            // 重命名图标
            listBoxItem.Icon = newIcon;
            SetListBoxItemData(listBoxItem, newName);
            // 重命名组名称
            var tempUserGroup = _allUserGroups[name];
            _allUserGroups.Remove(name);
            _allUserGroups.Add(newName, tempUserGroup);
            // 重命名组名称
            var tempShowInfos = _allModShowInfoGroups[name];
            _allModShowInfoGroups.Remove(name);
            _allModShowInfoGroups.Add(newName, tempShowInfos);
            // 重命名列表项
            _allListBoxItems.Remove(name);
            _allListBoxItems.Add(newName, listBoxItem);
            RefreshGroupModCount();
            RefreshModsContextMenu();
            IsRemindSave = true;
        }

        private static void SetListBoxItemData(ListBoxItemVM item, string name)
        {
            item.Content = name;
            item.ToolTip = name;
            item.Tag = name;
        }

        #endregion AddUserGroup

        #region DropFile

        internal async Task DropFiles(Array array)
        {
            var count = array.Length;
            var tempPath = "Temp";
            var tempDirectoryInfo = new DirectoryInfo(tempPath);
            var completed = 0;
            Logger.Info($"{I18nRes.ConfirmDragFiles} {I18nRes.Size}: {count}");
            using var pendingHandler = PendingBoxVM.Show(
                string.Format(I18nRes.UnArchiveFileMessage, count, completed, count - completed, "")
            );
            foreach (string path in array)
            {
                await Task.Delay(1);
                if (Directory.Exists(path))
                {
                    Logger.Info($"{I18nRes.ParseDirectory} {path}");
                    var files = Utils.GetAllSubFiles(path);
                    count += files.Count;
                    foreach (var subFile in files)
                    {
                        pendingHandler.UpdateMessage(
                            string.Format(
                                I18nRes.UnArchiveFileMessage,
                                count,
                                completed,
                                count - completed,
                                subFile
                            )
                        );
                        await AddModFromFile(subFile.FullName, tempDirectoryInfo);
                        completed++;
                    }
                }
                else
                {
                    pendingHandler.UpdateMessage(
                        string.Format(
                            I18nRes.UnArchiveFileMessage,
                            count,
                            completed,
                            count - completed,
                            path
                        )
                    );
                    await AddModFromFile(path, tempDirectoryInfo);
                    completed++;
                }
            }
            CheckAndRefreshDisplayData();
            tempDirectoryInfo.Delete(true);
        }

        private async Task AddModFromFile(string file, DirectoryInfo tempDirectoryInfo)
        {
            Logger.Info($"{I18nRes.ParseFile} {file}");
            if (
                await TryGetModInfoPath(file, tempDirectoryInfo.Name, tempDirectoryInfo)
                is not string jsonFile
            )
                return;
            var jsonFileName = Path.GetFileName(jsonFile);
            var directoryName = Path.GetFileName(Path.GetDirectoryName(jsonFile)!);
            var modDirectory = Path.Combine(GameInfo.ModsDirectory, directoryName);
            if (
                ModInfo.Parse(
                    Utils.JsonParse2Object(jsonFile)!,
                    Path.Combine(modDirectory, jsonFileName)
                )
                is not ModInfo newModInfo
            )
            {
                MessageBoxVM.Show(
                    new($"{I18nRes.FileError}\n{I18nRes.Path}: {file}")
                    {
                        ShowMainWindowBlurEffect = false
                    }
                );
                return;
            }
            if (!_allModInfos.ContainsKey(newModInfo.Id))
            {
                Utils.CopyDirectory(Path.GetDirectoryName(jsonFile)!, GameInfo.ModsDirectory);
                AddMod(newModInfo);
                return;
            }
            await TryOverwriteMod(
                jsonFile,
                modDirectory,
                _allModInfos[newModInfo.Id],
                newModInfo,
                tempDirectoryInfo
            );
            Directory.Delete(Path.Combine(tempDirectoryInfo.FullName, directoryName), true);

            static async Task<string?> TryGetModInfoPath(
                string file,
                string tempDirectory,
                DirectoryInfo tempDirectoryInfo
            )
            {
                if (!await Utils.UnArchiveFileToDirectory(file, tempDirectory))
                {
                    MessageBoxVM.Show(
                        new($"{I18nRes.UnzipError}\n {I18nRes.Path}:{file}")
                        {
                            ShowMainWindowBlurEffect = false
                        }
                    );
                    return null;
                }
                var filesInfo = tempDirectoryInfo.GetFiles(
                    _ModInfoFile,
                    SearchOption.AllDirectories
                );
                if (
                    !(
                        filesInfo.FirstOrDefault(defaultValue: null) is FileInfo fileInfo
                        && fileInfo.FullName is string jsonFile
                    )
                )
                {
                    Logger.Info($"{I18nRes.ZipFileError} {I18nRes.Path}: {file}");
                    MessageBoxVM.Show(
                        new($"{I18nRes.ZipFileError}\n{I18nRes.Path}: {file}")
                        {
                            ShowMainWindowBlurEffect = false
                        }
                    );
                    return null;
                }
                return jsonFile;
            }
            async Task TryOverwriteMod(
                string jsonFile,
                string directoryName,
                ModInfo modInfo,
                ModInfo newModInfo,
                DirectoryInfo tempDirectoryInfo
            )
            {
                var result = MessageBoxVM.Show(
                    new(
                        $"{newModInfo.Id}\n{string.Format(I18nRes.DuplicateModExists, modInfo.Version, newModInfo.Version)}"
                    )
                    {
                        Button = MessageBoxVM.Button.YesNoCancel,
                        Icon = MessageBoxVM.Icon.Question,
                        ShowMainWindowBlurEffect = false,
                    }
                );
                var showInfo = _allModsShowInfo[modInfo.Id];
                var isCollected = showInfo.IsCollected;
                var isEnabled = showInfo.IsEnabled;
                if (result is MessageBoxVM.Result.Yes)
                {
                    Utils.CopyDirectory(
                        modInfo.ModDirectory,
                        $"{_BackupModsDirectory}\\{tempDirectoryInfo.Name}"
                    );
                    string tempDirectory = $"{_BackupModsDirectory}\\{tempDirectoryInfo.Name}";
                    await Utils.ArchiveDirectoryToFile(
                        tempDirectory,
                        _BackupModsDirectory,
                        directoryName
                    );
                    Directory.Delete(tempDirectory, true);
                    Directory.Delete(modInfo.ModDirectory, true);
                    Utils.CopyDirectory(Path.GetDirectoryName(jsonFile)!, GameInfo.ModsDirectory);
                    RemoveMod(newModInfo.Id);
                    AddMod(newModInfo);
                    IsRemindSave = true;
                    Logger.Info(
                        $"{I18nRes.ReplaceMod} {newModInfo.Id} {modInfo.Version} => {newModInfo.Version}"
                    );
                }
                else if (result is MessageBoxVM.Result.No)
                {
                    Utils.DeleteDirectoryToRecycleBin(modInfo.ModDirectory);
                    Utils.CopyDirectory(Path.GetDirectoryName(jsonFile)!, GameInfo.ModsDirectory);
                    RemoveMod(newModInfo.Id);
                    AddMod(newModInfo);
                    IsRemindSave = true;
                    Logger.Info(
                        $"{I18nRes.ReplaceMod} {newModInfo.Id} {modInfo.Version} => {newModInfo.Version}"
                    );
                }
                ChangeModEnabled(newModInfo.Id, isEnabled);
                ChangeModCollected(newModInfo.Id, isCollected);
            }
        }

        #endregion DropFile

        internal void Close()
        {
            AddUserGroupWindow.Close();
        }
    }
}
