using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
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

        /// <summary>记录了模组类型的嵌入资源链接</summary>
        private static readonly Uri modTypeGroupUri =
            new("/Resources/ModTypeGroup.toml", UriKind.Relative);

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
        private Dictionary<string, ObservableCollection<ModShowInfo>> allModShowInfoGroups =
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
            ModsInfo.AllModsInfo = allModInfos;
            ModsInfo.AllEnabledModsId = allEnabledModsId;
            ModsInfo.AllCollectedModsId = allCollectedModsId;
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
            //RefreshModsContextMenu();
            RefreshGroupModCount();
            GC.Collect();
        }

        private void GetAllModsInfo()
        {
            int errSize = 0;
            StringBuilder errSB = new();
            DirectoryInfo dirInfo = new(GameInfo.ModsDirectory);
            foreach (var dir in dirInfo.GetDirectories())
            {
                if (ModInfo.Parse($"{dir.FullName}\\{modInfoFile}") is not ModInfo info)
                {
                    errSize++;
                    errSB.AppendLine(dir.FullName);
                    continue;
                }
                allModInfos.Add(info.Id, info);
                Logger.Debug($"{I18nRes.ModAddSuccess}: {dir.FullName}");
            }
            Logger.Info(
                string.Format(I18nRes.ModAddCompleted, allModInfos.Count, errSize)
            );
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
            foreach (var modInfo in allModInfos.Values)
                AddModShowInfo(modInfo, false);
            Logger.Info($"{I18nRes.ModShowInfoSetSuccess} {I18nRes.Size}: {allModInfos.Count}");
            //ListBox_ModsGroupMenu.SelectedIndex = 0;
        }

        private ModShowInfo CreateModShowInfo(ModInfo info)
        {
            return new ModShowInfo(info)
            {
                IsCollected = allCollectedModsId.Contains(info.Id),
                IsEnabled = allEnabledModsId.Contains(info.Id),
                MissDependencies = false,
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
                    Logger.Error($"{I18nRes.IconLoadError} {I18nRes.Path}: {filePath}", ex);
                    return null;
                }
            }
        }

        #region AddMod


        private void AddMod(ModInfo modInfo)
        {
            allModInfos.Add(modInfo.Id, modInfo);
            AddModShowInfo(modInfo);
            Logger.Debug($"{I18nRes.RemoveMod} {modInfo.Id} {modInfo.Version}");
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
            Logger.Debug($"{I18nRes.AddMod} {showInfo.Id} {showInfo.Version}");
        }
        #endregion
        #region RemoveMod


        private void RemoveMod(string id)
        {
            var modInfo = allModInfos[id];
            allModInfos.Remove(id);
            RemoveModShowInfo(id);
            Logger.Debug($"{I18nRes.RemoveMod} {id} {modInfo.Version}");
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
        #endregion
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
                        $"{I18nRes.OpenModDirectory} {I18nRes.Path}: {allModInfos[modShowInfo.Id].ModDirectory}"
                    );
                    Utils.OpenLink(allModInfos[modShowInfo.Id].ModDirectory);
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
                    string path = allModInfos[modShowInfo.Id].ModDirectory;
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
                    Utils.DeleteDirToRecycleBin(path);
                    IsRemindSave = true;
                };
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM? AddToUserGroup()
            {
                MenuItemVM? menuItem = null;
                // 添加至用户分组
                if (allUserGroups.Count == 0)
                    return menuItem;
                menuItem = new();
                menuItem.Header = I18nRes.AddModToUserGroup;
                menuItem.ItemsSource = new();
                foreach (var group in allUserGroups.Keys)
                {
                    if (allUserGroups[group].Contains(modShowInfo.Id))
                        continue;
                    MenuItemVM groupItem = new();
                    groupItem.Header = group;
                    groupItem.CommandEvent += (p) =>
                        ChangeUserGroupContainsSelectedMods(group, true);
                    menuItem.Add(groupItem);
                }
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem.Any() ? menuItem : null; ;
            }
            MenuItemVM? RemoveToUserGroup()
            {
                MenuItemVM? menuItem = null;
                // 从用户分组中删除
                var groupContainsMod = allUserGroups.Where(g => g.Value.Contains(modShowInfo.Id));
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

        #endregion
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
                    throw new ArgumentNullException();
                if (enabledModsJson.Count != 1 || !enabledModsJson.ContainsKey(strEnabledMods))
                    throw new ArgumentNullException();
                if (importMode)
                    EnabledModListImportMode();
                if (
                    enabledModsJson[strEnabledMods]?.AsArray() is not JsonArray enabledModsJsonArray
                )
                    throw new ArgumentNullException();
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
                Logger.Info($"{I18nRes.EnableMod} {I18nRes.Size}: {allEnabledModsId.Count}");
            }
            catch (Exception ex)
            {
                Logger.Error($"{I18nRes.LoadError} {I18nRes.Path}: {filePath}", ex);
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
                if (!allModInfos.ContainsKey(id))
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

        private void EnabledModListImportMode()
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
        #endregion
        #region GetUserData
        private void CheckUserData()
        {
            if (Utils.FileExists(userDataFile))
                GetUserData(userDataFile);
            else
                SaveUserData(userDataFile);
            if (Utils.FileExists(userGroupFile))
                GetAllUserGroup(userGroupFile);
            else
                SaveAllUserGroup(userGroupFile);
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
                    if (kv.Key == ModTypeGroup.Collected || kv.Key == strUserCustomData)
                        continue;
                    string group = kv.Key;
                    if (allUserGroups.ContainsKey(group))
                    {
                        Logger.Info($"{I18nRes.DuplicateUserGroupName} {group}");
                        errSB.AppendLine($"{I18nRes.DuplicateUserGroupName} {group}");
                        continue;
                    }
                    AddUserGroup(kv.Value[strIcon]!, group, false);
                    if (
                        GetModsInUserGroup(group, kv.Value[strMods].AsTomlArray)
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
                if (!allModsShowInfo.ContainsKey(id))
                {
                    Logger.Warring($"{I18nRes.NotFoundMod} {id}");
                    err.AppendLine(id);
                    continue;
                }
                if (allUserGroups[group].Add(id))
                    allModShowInfoGroups[group].Add(allModsShowInfo[id]);
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
                if (GetUserCustomData(toml[strUserCustomData].AsTomlArray) is StringBuilder err1)
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
                if (!allModsShowInfo.ContainsKey(id))
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
                var id = dict[strId].AsString;
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (!allModsShowInfo.ContainsKey(id))
                {
                    Logger.Warring($"{I18nRes.NotFoundMod} {id}");
                    err.AppendLine(id);
                    continue;
                }
                var info = allModsShowInfo[id];
                info.UserDescription = dict[nameof(ModShowInfo.UserDescription)];
            }
            return err.Length > 0 ? err : null;
        }
        #endregion
        private void GetAllListBoxItems()
        {
            foreach (var item in ListBox_MainMenu)
                allListBoxItems.Add(item.Tag!.ToString()!, item);
            foreach (var item in ListBox_TypeGroupMenu)
                allListBoxItems.Add(item.Tag!.ToString()!, item);
            foreach (var item in ListBox_UserGroupMenu)
                allListBoxItems.Add(item.Tag!.ToString()!, item);
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
                    allModsTypeGroup.Add(id, kv.Key);
            Logger.Info(I18nRes.TypeGroupRetrievalCompleted);
        }

        private string CheckTypeGroup(string id)
        {
            return allModsTypeGroup.ContainsKey(id)
                ? allModsTypeGroup[id]
                : ModTypeGroup.UnknownMods;
        }
        #endregion
        private void CheckRefreshGroupAndMods(string group)
        {
            if (nowSelectedGroupName == group)
                CheckFilterAndRefreshShowMods();
            RefreshGroupModCount();
        }

        #region RefreshNowShowMods
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
                RefreshNowShowMods(allModShowInfoGroups[nowSelectedGroupName]);
                Logger.Info($"{I18nRes.ShowGroup} {nowSelectedGroupName}");
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
                        => allModShowInfoGroups[nowSelectedGroupName].Where(
                            i => i.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    nameof(ModShowInfo.Id)
                        => allModShowInfoGroups[nowSelectedGroupName].Where(
                            i => i.Id.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    nameof(ModShowInfo.Author)
                        => allModShowInfoGroups[nowSelectedGroupName].Where(
                            i => i.Author.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    nameof(ModShowInfo.UserDescription)
                        => allModShowInfoGroups[nowSelectedGroupName].Where(
                            i =>
                                i.UserDescription.Contains(text, StringComparison.OrdinalIgnoreCase)
                        ),
                    _ => null!
                }
            );
        #endregion
        #region RefreshDisplayInfo
        private void RefreshGroupModCount()
        {
            foreach (var item in allListBoxItems.Values)
            {
                int size = allModShowInfoGroups[item!.Tag!.ToString()!].Count;
                item.Content = $"{item.ToolTip} ({size})";
                Logger.Debug($"{I18nRes.ModCountInGroupRefresh} {item.Content}");
            }
            Logger.Debug(I18nRes.ModCountInGroupRefreshCompleted);
        }

        private void RefreshModsContextMenu()
        {
            foreach (var showInfo in allModsShowInfo.Values)
                showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
            Logger.Info(
                $"{I18nRes.ContextMenuRefreshCompleted} {I18nRes.Size}: {allModsShowInfo.Values.Count}"
            );
        }
        #endregion
        #region ChangeUserGroupContainsSelectedMods
        private void ChangeUserGroupContainsSelectedMods(string group, bool isInGroup)
        {
            int count = nowSelectedMods.Count;
            for (int i = 0; i < nowSelectedMods.Count;)
            {
                ChangeUserGroupContainsSelectedMod(group, nowSelectedMods[i].Id, isInGroup);
                // 如果已选择数量没有变化,则继续下一个选项
                if (count == nowSelectedMods.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (count != nowSelectedMods.Count)
                CloseModDetails();
            CheckRefreshGroupAndMods(group);
            IsRemindSave = true;
        }

        private void ChangeUserGroupContainsSelectedMod(string group, string id, bool isInGroup)
        {
            ModShowInfo showInfo = allModsShowInfo[id];
            if (isInGroup)
            {
                if (allUserGroups[group].Add(id))
                {
                    allModShowInfoGroups[group].Add(allModsShowInfo[id]);
                    Logger.Debug($"{id} {I18nRes.AddModToUserGroup} {group}");
                }
            }
            else
            {
                if (allUserGroups[group].Remove(id))
                {
                    allModShowInfoGroups[group].Remove(allModsShowInfo[id]);
                    Logger.Debug($"{id} {I18nRes.RemoveFromUserGroup} {group}");
                }
            }
            showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
        }

        #endregion
        #region ChangeModEnabled
        private void ChangeSelectedModsEnabled(bool? enabled = null)
        {
            int count = nowSelectedMods.Count;
            for (int i = 0; i < nowSelectedMods.Count;)
            {
                ChangeModEnabled(nowSelectedMods[i].Id, enabled);
                // 如果已选择数量没有变化,则继续下一个选项
                if (count == nowSelectedMods.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (count != nowSelectedMods.Count)
                CloseModDetails();
            CheckRefreshGroupAndMods(nameof(ModTypeGroup.Enabled));
            CheckEnabledModsDependencies();
            IsRemindSave = true;
        }

        private void ClearAllEnabledMods()
        {
            while (allEnabledModsId.Count > 0)
                ChangeModEnabled(allEnabledModsId.ElementAt(0), false);
            Logger.Info(I18nRes.DisableAllEnabledMods);
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
            Logger.Debug(
                $"{id} {I18nRes.ChangeEnabledStateTo} {showInfo.IsEnabled}"
            );
        }
        #endregion
        private void CheckEnabledModsDependencies()
        {
            foreach (var showInfo in allModShowInfoGroups[ModTypeGroup.Enabled])
            {
                if (showInfo.DependenciesSet != null)
                {
                    showInfo.MissDependenciesMessage = string.Join(
                        " , ",
                        showInfo.DependenciesSet.Where(s => !allEnabledModsId.Contains(s.Id))
                    );
                    if (showInfo.DependenciesSet.Any())
                    {
                        Logger.Info(
                            $"{showInfo.Id} {I18nRes.NotEnableDependencies} {showInfo.DependenciesSet}"
                        );
                        showInfo.MissDependencies = true;
                    }
                    else
                        showInfo.MissDependencies = false;
                }
            }
        }

        #region ChangeModCollected
        private void ChangeSelectedModsCollected(bool? collected = null)
        {
            int count = nowSelectedMods.Count;
            for (int i = 0; i < nowSelectedMods.Count;)
            {
                ChangeModCollected(nowSelectedMods[i].Id, collected);
                if (count == nowSelectedMods.Count)
                    i++;
            }
            // 判断显示的数量与原来的数量是否一致
            if (count != nowSelectedMods.Count)
                CloseModDetails();
            CheckRefreshGroupAndMods(nameof(ModTypeGroup.Collected));
            IsRemindSave = true;
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
            Logger.Debug(
                $"{id} {I18nRes.ChangeCollectStateTo} {showInfo.IsCollected}"
            );
        }
        #endregion

        #region SaveAllData
        private void SaveAllData()
        {
            SaveEnabledMods(GameInfo.EnabledModsJsonFile);
            SaveUserData(userDataFile);
            SaveAllUserGroup(userGroupFile);
        }

        private void SaveEnabledMods(string filePath)
        {
            JsonObject jsonObject = new() { [strEnabledMods] = new JsonArray() };
            foreach (var mod in allEnabledModsId)
                jsonObject[strEnabledMods]!.AsArray().Add(mod);
            jsonObject.SaveTo(filePath);
            Logger.Info($"{I18nRes.EnabledListSaveCompleted} {I18nRes.Path}: {filePath}");
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

        private void SaveAllUserGroup(string filePath, string group = strAll)
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
            Logger.Info($"{I18nRes.UserGroupSaveCompleted} {I18nRes.Path}: {filePath}");
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
        #endregion
        #region ModDetails
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
            Logger.Debug($"{I18nRes.CloseDetails}");
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
        #endregion
        #region AddUserGroup
        internal bool TryAddUserGroup(string icon, string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && !allUserGroups.ContainsKey(name))
            {
                if (name == ModTypeGroup.Collected || name == strUserCustomData)
                    MessageBoxVM.Show(
                        new(
                            string.Format(
                                I18nRes.UserGroupCannotNamed,
                                ModTypeGroup.Collected,
                                strUserCustomData
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
            // 调用全局资源需要写全
            SetListBoxItemData(ref listBoxItem, name);
            ContextMenuVM contextMenu =
                new() { RenameUserGroupMenuItemVM(), RemoveUserGroupMenuItemVM() };
            listBoxItem.ContextMenu = contextMenu;
            listBoxItem.Icon = icon;
            ListBox_UserGroupMenu.Add(listBoxItem);
            allUserGroups.Add(name, new());
            allListBoxItems.Add(name, listBoxItem);
            allModShowInfoGroups.Add(name, new());
            ComboBox_ExportUserGroup.Add(new() { Content = name, Tag = name });
            Logger.Info($"{I18nRes.AddUserGroup} {icon} {name}");
            RefreshGroupModCount();
            RefreshModsContextMenu();
            if (remindSave)
                IsRemindSave = true;

            MenuItemVM RenameUserGroupMenuItemVM()
            {
                // 重命名分组
                MenuItemVM menuItem = new();
                menuItem.Header = I18nRes.RenameUserGroup;
                menuItem.CommandEvent += (p) => RenameUserGroup(listBoxItem);
                Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
                return menuItem;
            }
            MenuItemVM RemoveUserGroupMenuItemVM()
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
            if (nowSelectedGroup == listBoxItem)
                ListBox_TypeGroupMenu.SelectedIndex = 0;
            ListBox_UserGroupMenu.Remove(listBoxItem);
            allUserGroups.Remove(name);
            allListBoxItems.Remove(name);
            allModShowInfoGroups.Remove(name);
            RefreshModsContextMenu();
            IsRemindSave = true;
            // 删除导出用户分组下拉列表的此分组选择
            if (ComboBox_ExportUserGroup.SelectedItem!.Tag!.ToString() == name)
            {
                ComboBox_ExportUserGroup.Remove(ComboBox_ExportUserGroup.SelectedItem);
                ComboBox_ExportUserGroup.SelectedIndex = 0;
            }
        }

        private void RenameUserGroup(ListBoxItemVM listBoxItem)
        {
            string icon = listBoxItem.Icon!.ToString()!;
            string name = listBoxItem.ToolTip!.ToString()!;
            // TODO: 使用了window 需移至外部
            Views.ModManagerPage.AddUserGroupWindow window = new();
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
                            ShowMainWindowBlurEffect = false
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
                    IsRemindSave = true;
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

        #endregion
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
                !(
                    filesInfo.FirstOrDefault(defaultValue: null) is FileInfo fileInfo
                    && fileInfo.FullName is string jsonPath
                )
            )
            {
                Logger.Info($"{I18nRes.ZipFileError} {I18nRes.Path}: {filePath}");
                MessageBoxVM.Show(new($"{I18nRes.ZipFileError}\n{I18nRes.Path}: {filePath}"));
                return;
            }
            string directoryName = Path.GetFileName(fileInfo.DirectoryName)!;
            if (
                ModInfo.Parse(
                    Utils.JsonParse2Object(jsonPath)!,
                    $"{GameInfo.ModsDirectory}\\{directoryName}"
                )
                is not ModInfo newModInfo
            )
            {
                MessageBoxVM.Show(new($"{I18nRes.FileError}\n{I18nRes.Path}: {filePath}"));
                return;
            }
            if (!allModInfos.ContainsKey(newModInfo.Id))
            {
                Utils.CopyDirectory(Path.GetDirectoryName(jsonPath)!, GameInfo.ModsDirectory);
                AddMod(newModInfo);
                RefreshGroupModCount();
                return;
            }
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
                await Utils.ArchiveDirToDir(tempDirectory, backupModsDirectory, directoryName);
                Directory.Delete(tempDirectory, true);
                Directory.Delete(originalModInfo.ModDirectory, true);
                Utils.CopyDirectory(Path.GetDirectoryName(jsonPath)!, GameInfo.ModsDirectory);
                RemoveMod(newModInfo.Id);
                AddMod(newModInfo);
                RefreshGroupModCount();
                IsRemindSave = true;
                Logger.Info(
                    $"{I18nRes.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}"
                );
            }
            else if (result == MessageBoxVM.Result.No)
            {
                Utils.DeleteDirToRecycleBin(originalModInfo.ModDirectory);
                Utils.CopyDirectory(Path.GetDirectoryName(jsonPath)!, GameInfo.ModsDirectory);
                RemoveMod(newModInfo.Id);
                AddMod(newModInfo);
                RefreshGroupModCount();
                IsRemindSave = true;
                Logger.Info(
                    $"{I18nRes.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}"
                );
            }
            CheckFilterAndRefreshShowMods();
            dirs.Delete(true);
        }
    }
}
