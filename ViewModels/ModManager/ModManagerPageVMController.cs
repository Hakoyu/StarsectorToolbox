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
using HKW.TOML;
using HKW.TOML.TomlDeserializer;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialogs;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.Models.ModInfo;
using StarsectorToolbox.Models.ST;
using StarsectorToolbox.Resources;
using I18nRes = StarsectorToolbox.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorToolbox.ViewModels.ModManager;

internal partial class ModManagerPageViewModel
{
    private static readonly string sr_userDataFile = $"{ST.CoreDirectory}\\UserData.toml";
    private static readonly string sr_userGroupFile = $"{ST.CoreDirectory}\\UserGroup.toml";
    private static readonly string sr_backupDirectory = $"{ST.CoreDirectory}\\Backup";
    private static readonly string sr_backupModsDirectory = $"{sr_backupDirectory}\\Mods";
    private const string c_modInfoFile = "mod_info.json";
    private const string c_strEnabledMods = "enabledMods";
    private const string c_strAll = "All";
    private const string c_strId = "Id";
    private const string c_strIcon = "Icon";
    private const string c_strMods = "Mods";
    private const string c_strUserCustomData = "UserCustomData";

    /// <summary>已启用的模组ID</summary>
    private readonly HashSet<string> r_allEnabledModsId = new();

    /// <summary>已收藏的模组ID</summary>
    private readonly HashSet<string> r_allCollectedModsId = new();

    /// <summary>
    /// <para>全部分组列表项</para>
    /// <para><see langword="Key"/>: 列表项Tag或ModGroupType</para>
    /// <para><see langword="Value"/>: 列表项</para>
    /// </summary>
    private readonly Dictionary<string, ListBoxItemVM> r_allListBoxItems = new();

    /// <summary>
    /// <para>全部模组信息</para>
    /// <para><see langword="Key"/>: 模组ID</para>
    /// <para><see langword="Value"/>: 模组信息</para>
    /// </summary>
    private readonly Dictionary<string, ModInfo> r_allModInfos = new();

    /// <summary>
    /// <para>全部模组显示信息</para>
    /// <para><see langword="Key"/>: 模组ID</para>
    /// <para><see langword="Value"/>: 模组显示信息</para>
    /// </summary>
    private readonly Dictionary<string, ModShowInfo> r_allModsShowInfo = new();

    /// <summary>
    /// <para>全部用户分组</para>
    /// <para><see langword="Key"/>: 分组名称</para>
    /// <para><see langword="Value"/>: 包含的模组</para>
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> r_allUserGroups = new();

    /// <summary>
    /// <para>全部分组包含的模组显示信息列表</para>
    /// <para><see langword="Key"/>: 分组名称</para>
    /// <para><see langword="Value"/>: 包含的模组显示信息的列表</para>
    /// </summary>
    private readonly Dictionary<string, ObservableCollection<ModShowInfo>> r_allModShowInfoGroups =
        new()
        {
            [ModTypeGroupName.All] = new(),
            [ModTypeGroupName.Enabled] = new(),
            [ModTypeGroupName.Disabled] = new(),
            [ModTypeGroupName.Libraries] = new(),
            [ModTypeGroupName.MegaMods] = new(),
            [ModTypeGroupName.FactionMods] = new(),
            [ModTypeGroupName.ContentExtensions] = new(),
            [ModTypeGroupName.UtilityMods] = new(),
            [ModTypeGroupName.MiscellaneousMods] = new(),
            [ModTypeGroupName.BeautifyMods] = new(),
            [ModTypeGroupName.UnknownMods] = new(),
            [ModTypeGroupName.Collected] = new(),
        };

    /// <summary>
    /// 初始化数据
    /// </summary>
    private void InitializeData()
    {
        // 设置外部API
        ModInfos.AllModInfos = new(r_allModInfos);
        ModInfos.AllEnabledModIds = r_allEnabledModsId;
        ModInfos.AllCollectedModIds = r_allCollectedModsId;
        ModInfos.AllUserGroups = r_allUserGroups.AsReadOnly<
            string,
            HashSet<string>,
            IReadOnlySet<string>
        >();
        GetAllModInfos();
        GetAllListBoxItems();
        TryGetModTypeGroup(ModTypeGroup.File);
        GetAllModShowInfos();
        CheckEnabledMods();
        CheckEnabledModsDependencies();
        CheckUserData();
        RefreshGroupModCount(false);
        RefreshAllGroupItemContextMenus();
    }

    private void GetAllModInfos()
    {
        // TODO: 可以保存游戏版本,启用列表以及模组本体的完全备份包
        int errSize = 0;
        int repeatSize = 0;
        var errSB = new StringBuilder();
        var errRepeat = new StringBuilder();
        var dirInfo = new DirectoryInfo(GameInfo.ModsDirectory);
        // 使用并行循环提高性能
        var allModInfo = dirInfo
            .EnumerateDirectories()
            .AsParallel()
            .AsOrdered()
            .Select(dir => (dir, ModInfo.Parse(Path.Combine(dir.FullName, c_modInfoFile))));
        foreach ((var dir, var info) in allModInfo)
        {
            if (info is null)
            {
                errSize++;
                errSB.AppendLine(dir.FullName);
                continue;
            }
            if (r_allModInfos.TryAdd(info.Id, info) is false)
            {
                repeatSize++;
                var modInfo = r_allModInfos[info.Id];
                Logger.Warring(
                    string.Format(I18nRes.ModIsContains, modInfo.ModDirectory, info.ModDirectory)
                );
                errRepeat.AppendJoin(", ", info.Id);
            }
        }

        if (repeatSize is not 0)
        {
            MessageBoxVM.Show(
                new(I18nRes.ModIsContainsMessage) { Icon = MessageBoxVM.Icon.Warning }
            );
        }
        Logger.Info(string.Format(I18nRes.ModAddCompleted, r_allModInfos.Count, errSize));
        if (errSize is not 0)
        {
            MessageBoxVM.Show(
                new($"{I18nRes.ModAddFailed} {I18nRes.Size}: {errSize}\n{errSB}")
                {
                    Icon = MessageBoxVM.Icon.Warning
                }
            );
        }
    }

    private void GetAllModShowInfos()
    {
        foreach (var modInfo in r_allModInfos.Values)
            AddModShowInfo(modInfo);
        Logger.Info($"{I18nRes.ModShowInfoSetSuccess} {I18nRes.Size}: {r_allModInfos.Count}");
    }

    private ModShowInfo CreateModShowInfo(ModInfo info)
    {
        var dependenciesMessage = string.Empty;
        if (info.DependenciesSet is not null)
        {
            dependenciesMessage = string.Join(
                "\n",
                info.DependenciesSet?.Select(
                    i =>
                        $"{I18nRes.Name}: {i.Name} ID: {i.Id} "
                        + (i.Version is not null ? $"{I18nRes.Version} {i.Version}" : "")
                )!
            );
        }
        return new ModShowInfo(info)
        {
            IsCollected = r_allCollectedModsId.Contains(info.Id),
            IsEnabled = r_allEnabledModsId.Contains(info.Id),
            MissDependencies = false,
            DependenciesMessage = dependenciesMessage,
            ImageSource = GetImage($"{info.ModDirectory}\\icon.ico"),
        };
        static BitmapImage? GetImage(string filePath)
        {
            if (File.Exists(filePath) is false)
                return null;
            try
            {
                using var stream = new StreamReader(filePath).BaseStream;
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
        r_allModInfos.Add(modInfo.Id, modInfo);
        AddModShowInfo(modInfo);
        Logger.Debug($"{I18nRes.AddMod} {modInfo.Id} {modInfo.Version}");
    }

    private void AddModShowInfo(ModInfo modInfo)
    {
        if (r_allModsShowInfo.ContainsKey(modInfo.Id))
            return;
        ModShowInfo showInfo = CreateModShowInfo(modInfo);
        // 添加至总分组
        r_allModsShowInfo.Add(showInfo.Id, showInfo);
        r_allModShowInfoGroups[ModTypeGroupName.All].Add(showInfo);
        // 添加至类型分组
        r_allModShowInfoGroups[ModTypeGroup.GetGroupNameFromId(modInfo.Id)].Add(showInfo);
        // 添加至已启用或已禁用分组
        if (showInfo.IsEnabled)
            r_allModShowInfoGroups[ModTypeGroupName.Enabled].Add(showInfo);
        else
            r_allModShowInfoGroups[ModTypeGroupName.Disabled].Add(showInfo);
        // 添加至已收藏分组
        if (showInfo.IsCollected)
            r_allModShowInfoGroups[ModTypeGroupName.Collected].Add(showInfo);
        // 添加至用户分组
        foreach (var userGroup in r_allUserGroups)
        {
            if (userGroup.Value.Contains(modInfo.Id))
            {
                userGroup.Value.Add(modInfo.Id);
                r_allModShowInfoGroups[userGroup.Key].Add(showInfo);
            }
        }
        showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
        Logger.Debug($"{I18nRes.AddMod} {showInfo.Id} {showInfo.Version}");
    }

    #endregion AddMod

    #region RemoveMod

    private void RemoveMod(string id)
    {
        var modInfo = r_allModInfos[id];
        r_allModInfos.Remove(id);
        RemoveModShowInfo(id);
        Logger.Debug($"{I18nRes.RemoveMod} {id} {modInfo.Version}");
    }

    private void RemoveModShowInfo(string id)
    {
        var modShowInfo = r_allModsShowInfo[id];
        // 从总分组中删除
        r_allModsShowInfo.Remove(id);
        r_allModShowInfoGroups[ModTypeGroupName.All].Remove(modShowInfo);
        // 从类型分组中删除
        r_allModShowInfoGroups[ModTypeGroup.GetGroupNameFromId(id)].Remove(modShowInfo);
        // 从已启用或已禁用分组中删除
        if (r_allEnabledModsId.Remove(id))
        {
            r_allModShowInfoGroups[ModTypeGroupName.Enabled].Remove(modShowInfo);
        }
        else
            r_allModShowInfoGroups[ModTypeGroupName.Disabled].Remove(modShowInfo);
        // 从已收藏中删除
        if (r_allCollectedModsId.Remove(id))
        {
            r_allModShowInfoGroups[ModTypeGroupName.Collected].Remove(modShowInfo);
        }
        // 从用户分组中删除
        foreach (var userGroup in r_allUserGroups)
        {
            if (userGroup.Value.Remove(id))
            {
                r_allModShowInfoGroups[userGroup.Key].Remove(modShowInfo);
            }
        }
    }

    #endregion RemoveMod

    private void RefreshAllGroupItemContextMenus()
    {
        foreach (var item in ListBox_MainMenu)
        {
            var group = item.Tag!.ToString()!;
            item.ContextMenu = CreateGroupItemContextMenu(group);
        }
        foreach (var item in ListBox_TypeGroupMenu)
        {
            var group = item.Tag!.ToString()!;
            item.ContextMenu = CreateGroupItemContextMenu(group);
        }
        ListBox_UserGroupMenu[0].ContextMenu = CreateGroupItemContextMenu(
            nameof(ModTypeGroupName.Collected)
        );
    }

    private ContextMenuVM CreateGroupItemContextMenu(string group)
    {
        return new(
            (list) =>
            {
                list.Add(EnableAllModsMenuItem(group));
                list.Add(DisableAllModsMenuItem(group));
                if (r_allUserGroups.Count > 0)
                {
                    list.Add(AddModsToUserGroupMenuItem(group));
                    list.Add(RemoveModsFromUserGroupMenuItem(group));
                }
            }
        );

        MenuItemVM EnableAllModsMenuItem(string group)
        {
            // 启用列表中的所有模组
            MenuItemVM menuItem = new();
            menuItem.Icon = "✅";
            menuItem.Header = I18nRes.EnableAllMods;
            menuItem.ItemsSource = new();
            menuItem.CommandEvent += (p) =>
            {
                ChangeModsEnabled(r_allModShowInfoGroups[group], true);
                CheckAndRefreshDisplayData();
            };
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
        MenuItemVM DisableAllModsMenuItem(string group)
        {
            // 禁用列表中的所有模组
            MenuItemVM menuItem = new();
            menuItem.Icon = "❎";
            menuItem.Header = I18nRes.DisableAllMods;
            menuItem.ItemsSource = new();
            menuItem.CommandEvent += (p) =>
            {
                ChangeModsEnabled(r_allModShowInfoGroups[group], false);
                CheckAndRefreshDisplayData();
            };
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
        MenuItemVM AddModsToUserGroupMenuItem(string group)
        {
            // 添加组内模组至用户分组
            MenuItemVM menuItem = new();
            menuItem.Icon = "➡";
            menuItem.Header = I18nRes.AddToUserGroup;
            menuItem.ToolTip = I18nRes.AddToUserGroupToolTip;
            menuItem.ItemsSource = new();
            foreach (var userGroup in r_allUserGroups)
            {
                MenuItemVM userGroupMenuItem = new();
                userGroupMenuItem.Header = userGroup.Key;
                userGroupMenuItem.CommandEvent += (p) =>
                {
                    var userGroupName = userGroup.Key;
                    ChangeUserGroupContainsMods(r_allModShowInfoGroups[group], userGroupName, true);
                    CheckAndRefreshDisplayData(userGroupName);
                };
                menuItem.Add(userGroupMenuItem);
            }
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
        MenuItemVM RemoveModsFromUserGroupMenuItem(string group)
        {
            // 删除用户分组中包含的组内模组
            MenuItemVM menuItem = new();
            menuItem.Icon = "⬅";
            menuItem.Header = I18nRes.RemoveFromUserGroup;
            menuItem.ToolTip = I18nRes.RemoveFromUserGroupToolTip;
            menuItem.ItemsSource = new();
            foreach (var userGroup in r_allUserGroups)
            {
                MenuItemVM userGroupMenuItem = new();
                userGroupMenuItem.Header = userGroup.Key;
                userGroupMenuItem.CommandEvent += (p) =>
                {
                    var userGroupName = userGroup.Key;
                    ChangeUserGroupContainsMods(
                        r_allModShowInfoGroups[group],
                        userGroupName,
                        false
                    );
                    CheckAndRefreshDisplayData(userGroupName);
                };
                menuItem.Add(userGroupMenuItem);
            }
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
    }

    #region CreateModShowContextMenu

    private ContextMenuVM CreateModShowContextMenu(ModShowInfo showInfo)
    {
        Logger.Debug($"{showInfo.Id} {I18nRes.AddContextMenu}");
        ContextMenuVM contextMenu =
            new(
                (list) =>
                {
                    list.Add(EnableOrDisableSelectedModsMenuItem(_nowSelectedMods));
                    list.Add(CollectOrUncollectSelectedModsMenuItem(_nowSelectedMods));
                    list.Add(OpenModDirectoryMenuItem(showInfo));
                    list.Add(DeleteModMenuItem(showInfo));
                    RefreshModContextMenu(showInfo, false);
                }
            );
        return contextMenu;
        MenuItemVM EnableOrDisableSelectedModsMenuItem(IList<ModShowInfo> mods)
        {
            // 启用或禁用
            MenuItemVM menuItem = new();
            menuItem.Header = showInfo.IsEnabled
                ? I18nRes.DisableSelectedMods
                : I18nRes.EnableSelectedMods;
            menuItem.CommandEvent += (p) => ChangeModsEnabled(mods);
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
        MenuItemVM CollectOrUncollectSelectedModsMenuItem(IList<ModShowInfo> mods)
        {
            // 收藏或取消收藏
            MenuItemVM menuItem = new();
            menuItem.Header = showInfo.IsCollected
                ? I18nRes.UncollectSelectedMods
                : I18nRes.CollectSelectedMods;
            menuItem.CommandEvent += (p) => ChangeModsCollected(mods);
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
        MenuItemVM OpenModDirectoryMenuItem(ModShowInfo showInfo)
        {
            // 打开模组文件夹
            MenuItemVM menuItem = new();
            menuItem.Header = I18nRes.OpenModDirectory;
            menuItem.CommandEvent += (p) =>
            {
                Logger.Info(
                    $"{I18nRes.OpenModDirectory} {I18nRes.Path}: {r_allModInfos[showInfo.Id].ModDirectory}"
                );
                Utils.OpenLink(r_allModInfos[showInfo.Id].ModDirectory);
            };
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
        MenuItemVM DeleteModMenuItem(ModShowInfo showInfo)
        {
            // 删除模组至回收站
            MenuItemVM menuItem = new();
            menuItem.Header = I18nRes.DeleteMod;
            menuItem.CommandEvent += (p) =>
            {
                string path = r_allModInfos[showInfo.Id].ModDirectory;
                if (
                    MessageBoxVM.Show(
                        new(
                            $"{I18nRes.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18nRes.Path}: {path}\n"
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
                    $"{I18nRes.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18nRes.Path}: {path}\n"
                );
                RemoveMod(showInfo.Id);
                CheckFilterAndRefreshShowMods();
                RefreshGroupModCount();
                CloseModDetails();
                Utils.DeleteDirectoryToRecycleBin(path);
            };
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
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
            if (enabledModsJson.Count != 1 || !enabledModsJson.ContainsKey(c_strEnabledMods))
                throw new();
            if (importMode && EnabledModListImportMode() is false)
                return;
            if (enabledModsJson[c_strEnabledMods]?.AsArray() is not JsonArray enabledModsJsonArray)
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
            Logger.Info($"{I18nRes.EnableMod} {I18nRes.Size}: {r_allEnabledModsId.Count}");
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
            if (r_allModInfos.ContainsKey(id) is false)
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
        if (Utils.FileExists(sr_userDataFile))
            GetUserData(sr_userDataFile);
        else
            SaveUserData(sr_userDataFile);
        if (Utils.FileExists(sr_userGroupFile))
            GetAllUserGroup(sr_userGroupFile);
        else
            SaveAllUserGroup(sr_userGroupFile);
    }

    private void GetAllUserGroup(string filePath)
    {
        Logger.Info($"{I18nRes.LoadUserGroup} {I18nRes.Path}: {filePath}");
        try
        {
            StringBuilder errSB = new();
            TomlTable toml = TOML.ParseFromFile(filePath);
            foreach (var kv in toml)
            {
                if (kv.Key == ModTypeGroupName.Collected || kv.Key == c_strUserCustomData)
                    continue;
                string group = kv.Key;
                if (r_allUserGroups.ContainsKey(group))
                {
                    Logger.Info($"{I18nRes.DuplicateUserGroupName} {group}");
                    errSB.AppendLine($"{I18nRes.DuplicateUserGroupName} {group}");
                    continue;
                }
                AddUserGroup(kv.Value[c_strIcon]!, group, false);
                if (GetModsInUserGroup(group, kv.Value[c_strMods].AsTomlArray) is StringBuilder err)
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
            if (r_allModsShowInfo.ContainsKey(id) is false)
            {
                Logger.Warring($"{I18nRes.NotFoundMod} {id}");
                err.AppendLine(id);
                continue;
            }
            if (r_allUserGroups[group].Add(id))
                r_allModShowInfoGroups[group].Add(r_allModsShowInfo[id]);
        }
        return err.Length > 0 ? err : null;
    }

    private void GetUserData(string filePath)
    {
        Logger.Info($"{I18nRes.LoadUserData} {I18nRes.Path}: {filePath}");
        try
        {
            TomlTable toml = TOML.ParseFromFile(filePath);
            if (
                GetUserCollectedMods(toml[ModTypeGroupName.Collected].AsTomlArray)
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
            if (GetUserCustomData(toml[c_strUserCustomData].AsTomlArray) is StringBuilder err1)
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
            if (r_allModsShowInfo.ContainsKey(id) is false)
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
            var id = dict[c_strId].AsString;
            if (string.IsNullOrWhiteSpace(id))
                continue;
            if (r_allModsShowInfo.ContainsKey(id) is false)
            {
                Logger.Warring($"{I18nRes.NotFoundMod} {id}");
                err.AppendLine(id);
                continue;
            }
            var info = r_allModsShowInfo[id];
            info.UserDescription = dict[nameof(ModShowInfo.UserDescription)];
        }
        return err.Length > 0 ? err : null;
    }

    #endregion GetUserData

    private void GetAllListBoxItems()
    {
        foreach (var item in ListBox_MainMenu)
            r_allListBoxItems.Add(item.Tag!.ToString()!, item);
        foreach (var item in ListBox_TypeGroupMenu)
            r_allListBoxItems.Add(item.Tag!.ToString()!, item);
        foreach (var item in ListBox_UserGroupMenu)
            r_allListBoxItems.Add(item.Tag!.ToString()!, item);
        Logger.Info(I18nRes.ListBoxItemsRetrievalCompleted);
    }

    #region TypeGroup

    private static void TryGetModTypeGroup(string file)
    {
        try
        {
            GetModTypeGroup(file);
        }
        catch (Exception ex)
        {
            Logger.Error(I18nRes.ModTypeGroupFileError, ex);
            if (File.Exists(file))
                File.Delete(file);
            GetModTypeGroup(file);
        }
        Logger.Info(I18nRes.TypeGroupRetrievalCompleted);
    }

    private static void GetModTypeGroup(string file)
    {
        CheckModTypeGroup(file);
        using var sr = new StreamReader(file);
        var table = TOML.Parse(sr);
        TomlDeserializer.DeserializeStatic(table, typeof(ModTypeGroup));
    }

    private static void CheckModTypeGroup(string file)
    {
        using var resourceStream = ResourceDictionary
            .GetResourceStream(ResourceDictionary.ModTypeGroup_toml)
            .BaseStream;
        if (File.Exists(file) is false)
        {
            CreateModTypeGroup(file, resourceStream);
        }
        else
        {
            var fileInfo = new FileInfo(file);
            if (resourceStream.Length != fileInfo.Length)
            {
                CreateModTypeGroup(file, resourceStream);
            }
        }
    }

    private static void CreateModTypeGroup(string file, Stream resourceStream)
    {
        using var sw = new StreamWriter(file);
        resourceStream.CopyTo(sw.BaseStream);
    }

    #endregion TypeGroup

    #region RefreshDisplayData

    private void CheckAndRefreshDisplayData(string group = "")
    {
        if (NowSelectedGroupName == group)
            ShowSpin = false;
        CheckFilterAndRefreshShowMods();
        RefreshGroupModCount();
    }

    private void CheckFilterAndRefreshShowMods()
    {
        var text = ModFilterText;
        var type = ComboBox_ModFilterType.SelectedItem!.Tag!.ToString()!;
        if (string.IsNullOrWhiteSpace(text) is false)
        {
            RefreshNowShowMods(GetFilterModsShowInfo(text, type));
            Logger.Info($"{I18nRes.SearchMod} {text}");
        }
        else
        {
            RefreshNowShowMods(r_allModShowInfoGroups[NowSelectedGroupName]);
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
                    => r_allModShowInfoGroups[NowSelectedGroupName].Where(
                        i => i.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                    ),
                nameof(ModShowInfo.Id)
                    => r_allModShowInfoGroups[NowSelectedGroupName].Where(
                        i => i.Id.Contains(text, StringComparison.OrdinalIgnoreCase)
                    ),
                nameof(ModShowInfo.Author)
                    => r_allModShowInfoGroups[NowSelectedGroupName].Where(
                        i => i.Author.Contains(text, StringComparison.OrdinalIgnoreCase)
                    ),
                nameof(ModShowInfo.Description)
                    => r_allModShowInfoGroups[NowSelectedGroupName].Where(
                        i => i.Description.Contains(text, StringComparison.OrdinalIgnoreCase)
                    ),
                nameof(ModShowInfo.UserDescription)
                    => r_allModShowInfoGroups[NowSelectedGroupName].Where(
                        i => i.UserDescription.Contains(text, StringComparison.OrdinalIgnoreCase)
                    ),
                _ => null!
            }
        );

    private void RefreshGroupModCount(bool remindSave = true)
    {
        foreach (var item in r_allListBoxItems.Values)
        {
            int size = r_allModShowInfoGroups[item!.Tag!.ToString()!].Count;
            item.Content = $"{item.ToolTip} ({size})";
            Logger.Debug($"{I18nRes.ModCountInGroupRefresh} {item.Content}");
        }
        if (remindSave)
            IsRemindSave = true;
        Logger.Debug(I18nRes.ModCountInGroupRefreshCompleted);
    }

    #endregion RefreshDisplayData

    private void RefreshAllModsContextMenu()
    {
        foreach (var showInfo in r_allModsShowInfo.Values)
            RefreshModContextMenu(showInfo, true);
        Logger.Info(
            $"{I18nRes.ContextMenuRefreshCompleted} {I18nRes.Size}: {r_allModsShowInfo.Values.Count}"
        );
    }

    private void RefreshModContextMenu(ModShowInfo showInfo, bool checkLoaded)
    {
        var contextMenu = showInfo.ContextMenu;
        if (checkLoaded && contextMenu.IsLoaded is false)
            return;
        // 添加至用户分组
        if (
            contextMenu.LastOrDefault(i => i.Id == nameof(I18nRes.AddToUserGroup))
            is MenuItemVM addToUserGroupMenu
        )
        {
            contextMenu.Remove(addToUserGroupMenu);
        }
        if (AddToUserGroup(showInfo) is MenuItemVM newMenuItem)
            contextMenu.Add(newMenuItem);
        // 从用户分组删除
        if (
            contextMenu.LastOrDefault(i => i.Id == nameof(I18nRes.RemoveFromUserGroup))
            is MenuItemVM removeFromUserGroupMenu
        )
        {
            contextMenu.Remove(removeFromUserGroupMenu);
        }
        if (NowSelectedIsUserGroup)
        {
            if (r_allUserGroups[NowSelectedGroupName].Contains(showInfo.Id))
                contextMenu.Add(RemoveFromUserGroup(NowSelectedGroupName, showInfo));
        }
        MenuItemVM? AddToUserGroup(ModShowInfo showInfo)
        {
            // 添加至用户分组
            MenuItemVM? menuItem = null;
            if (r_allUserGroups.Any() is false)
                return menuItem;
            menuItem = new();
            menuItem.Header = I18nRes.AddToUserGroup;
            menuItem.ItemsSource = new();
            foreach (var group in r_allUserGroups.Keys)
            {
                if (r_allUserGroups[group].Contains(showInfo.Id))
                    continue;
                MenuItemVM groupItem = new();
                groupItem.Header = group;
                groupItem.CommandEvent += (p) =>
                    ChangeUserGroupContainsMods(_nowSelectedMods, group, true);
                menuItem.Add(groupItem);
            }
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem.Any() ? menuItem : null;
            ;
        }
        MenuItemVM RemoveFromUserGroup(string group, ModShowInfo showInfo)
        {
            // 从用户分组中删除
            MenuItemVM menuItem = new();
            menuItem.Id = nameof(I18nRes.RemoveFromUserGroup);
            menuItem.Header = I18nRes.RemoveFromUserGroup;
            menuItem.ItemsSource = new();
            menuItem.CommandEvent += (p) =>
                ChangeUserGroupContainsMods(_nowSelectedMods, group, false);
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
    }

    private void RefreshModsContextMenuI18n()
    {
        foreach (var showInfo in r_allModsShowInfo.Values)
        {
            var contextMenu = showInfo.ContextMenu;
            if (contextMenu.IsLoaded is false)
                continue;
            contextMenu[0].Header = showInfo.IsEnabled
                ? I18nRes.DisableSelectedMods
                : I18nRes.EnableSelectedMods;
            contextMenu[1].Header = showInfo.IsCollected
                ? I18nRes.UncollectSelectedMods
                : I18nRes.CollectSelectedMods;
            contextMenu[2].Header = I18nRes.OpenModDirectory;
            contextMenu[3].Header = I18nRes.DeleteMod;
            if (
                contextMenu.LastOrDefault(i => i.Id == nameof(I18nRes.AddToUserGroup))
                is MenuItemVM addToUserGroupMenu
            )
                addToUserGroupMenu.Header = I18nRes.AddToUserGroup;
            if (
                contextMenu.LastOrDefault(i => i.Id == nameof(I18nRes.RemoveFromUserGroup))
                is MenuItemVM removeFromUserGroupMenu
            )
                removeFromUserGroupMenu.Header = I18nRes.RemoveFromUserGroup;
        }
    }

    #region ChangeUserGroupContainsMods

    private void ChangeUserGroupContainsMods(
        IList<ModShowInfo> modShowInfos,
        string userGroup,
        bool isInGroup
    )
    {
        int count = modShowInfos.Count;
        for (int i = 0; i < modShowInfos.Count; )
        {
            ChangeUserGroupContainsMod(userGroup, modShowInfos[i].Id, isInGroup);
            // 如果已选择数量没有变化,则继续下一个选项
            if (count == modShowInfos.Count)
                i++;
        }
        // 判断显示的数量与原来的数量是否一致
        if (count != modShowInfos.Count)
            CloseModDetails();
        CheckAndRefreshDisplayData(userGroup);
    }

    private void ChangeUserGroupContainsMod(string userGroup, string id, bool isInGroup)
    {
        var showInfo = r_allModsShowInfo[id];
        if (isInGroup)
        {
            if (r_allUserGroups[userGroup].Add(id))
            {
                r_allModShowInfoGroups[userGroup].Add(r_allModsShowInfo[id]);
                Logger.Debug($"{id} {I18nRes.AddToUserGroup} {userGroup}");
            }
        }
        else
        {
            if (r_allUserGroups[userGroup].Remove(id))
            {
                r_allModShowInfoGroups[userGroup].Remove(r_allModsShowInfo[id]);
                Logger.Debug($"{id} {I18nRes.RemoveFromUserGroup} {userGroup}");
            }
        }
        showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
    }

    #endregion ChangeUserGroupContainsSelectedMods

    #region ChangeModEnabled

    private void ChangeModsEnabled(IList<ModShowInfo> modShowInfos, bool? enabled = null)
    {
        int count = modShowInfos.Count;
        for (int i = 0; i < modShowInfos.Count; )
        {
            ChangeModEnabled(modShowInfos[i].Id, enabled);
            // 如果已选择数量没有变化,则继续下一个选项
            if (count == modShowInfos.Count)
                i++;
        }
        // 判断显示的数量与原来的数量是否一致
        if (count != modShowInfos.Count)
            CloseModDetails();
        CheckAndRefreshDisplayData(nameof(ModTypeGroupName.Enabled));
        CheckEnabledModsDependencies();
    }

    private void ClearAllEnabledMods()
    {
        while (r_allEnabledModsId.Count > 0)
            ChangeModEnabled(r_allEnabledModsId.ElementAt(0), false);
        Logger.Info(I18nRes.DisableAllEnabledMods);
    }

    private void ChangeModEnabled(string id, bool? enabled = null)
    {
        ModShowInfo showInfo = r_allModsShowInfo[id];
        showInfo.IsEnabled = (bool)(enabled is null ? !showInfo.IsEnabled : enabled);
        showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
        if (showInfo.IsEnabled is true)
        {
            if (r_allEnabledModsId.Add(showInfo.Id))
            {
                r_allModShowInfoGroups[ModTypeGroupName.Enabled].Add(showInfo);
                r_allModShowInfoGroups[ModTypeGroupName.Disabled].Remove(showInfo);
            }
        }
        else
        {
            if (r_allEnabledModsId.Remove(showInfo.Id))
            {
                r_allModShowInfoGroups[ModTypeGroupName.Enabled].Remove(showInfo);
                r_allModShowInfoGroups[ModTypeGroupName.Disabled].Add(showInfo);
                showInfo.MissDependencies = false;
            }
        }
        Logger.Debug($"{id} {I18nRes.ChangeEnabledStateTo} {showInfo.IsEnabled}");
    }

    #endregion ChangeModEnabled

    private void CheckEnabledModsDependencies()
    {
        foreach (var showInfo in r_allModShowInfoGroups[ModTypeGroupName.Enabled])
        {
            if (showInfo.DependenciesSet is null)
                continue;
            showInfo.MissDependenciesMessage = string.Join(
                " , ",
                showInfo.DependenciesSet
                    .Where(s => !r_allEnabledModsId.Contains(s.Id))
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

    private void ChangeModsCollected(IList<ModShowInfo> mods, bool? collected = null)
    {
        int count = mods.Count;
        for (int i = 0; i < mods.Count; )
        {
            ChangeModCollected(mods[i].Id, collected);
            if (count == mods.Count)
                i++;
        }
        // 判断显示的数量与原来的数量是否一致
        if (count != mods.Count)
            CloseModDetails();
        CheckAndRefreshDisplayData(nameof(ModTypeGroupName.Collected));
    }

    private void ChangeModCollected(string id, bool? collected = null)
    {
        ModShowInfo showInfo = r_allModsShowInfo[id];
        showInfo.IsCollected = (bool)(collected is null ? !showInfo.IsCollected : collected);
        showInfo.ContextMenu = CreateModShowContextMenu(showInfo);
        if (showInfo.IsCollected is true)
        {
            if (r_allCollectedModsId.Add(showInfo.Id))
                r_allModShowInfoGroups[ModTypeGroupName.Collected].Add(showInfo);
        }
        else
        {
            if (r_allCollectedModsId.Remove(showInfo.Id))
                r_allModShowInfoGroups[ModTypeGroupName.Collected].Remove(showInfo);
        }
        Logger.Debug($"{id} {I18nRes.ChangeCollectStateTo} {showInfo.IsCollected}");
    }

    #endregion ChangeModCollected

    #region SaveAllData

    private void SaveAllData()
    {
        SaveEnabledMods(GameInfo.EnabledModsJsonFile);
        SaveUserData(sr_userDataFile);
        SaveAllUserGroup(sr_userGroupFile);
    }

    private void SaveEnabledMods(string filePath)
    {
        JsonObject jsonObject = new() { [c_strEnabledMods] = new JsonArray() };
        foreach (var mod in r_allEnabledModsId)
            jsonObject[c_strEnabledMods]!.AsArray().Add(mod);
        jsonObject.SaveTo(filePath);
        Logger.Info($"{I18nRes.EnabledListSaveCompleted} {I18nRes.Path}: {filePath}");
    }

    private void SaveUserData(string filePath)
    {
        TomlTable table =
            new()
            {
                [ModTypeGroupName.Collected] = new TomlArray(),
                [c_strUserCustomData] = new TomlArray(),
            };
        foreach (var info in r_allModsShowInfo.Values)
        {
            if (info.IsCollected is true)
                table[ModTypeGroupName.Collected].Add(info.Id);
            if (info.UserDescription!.Length > 0)
            {
                table[c_strUserCustomData].Add(
                    new TomlTable()
                    {
                        [nameof(ModShowInfo.Id)] = info.Id,
                        [nameof(ModShowInfo.UserDescription)] =
                            info.UserDescription!.Length > 0 ? info.UserDescription : "",
                    }
                );
            }
        }
        table.SaveToFile(filePath);
        Logger.Info($"{I18nRes.SaveUserDataSuccess} {I18nRes.Path}: {filePath}");
    }

    private void SaveAllUserGroup(string filePath, string group = c_strAll)
    {
        TomlTable table = new();
        if (group == c_strAll)
        {
            foreach (var groupData in r_allUserGroups)
                Save(groupData.Key);
        }
        else
        {
            Save(group);
        }
        table.SaveToFile(filePath);
        Logger.Info($"{I18nRes.UserGroupSaveCompleted} {I18nRes.Path}: {filePath}");
        void Save(string name)
        {
            var mods = r_allUserGroups[name];
            table.Add(
                name,
                new TomlTable()
                {
                    [c_strIcon] = r_allListBoxItems[name].Icon!.ToString()!,
                    [c_strMods] = new TomlArray(),
                }
            );
            foreach (var id in mods)
                table[name][c_strMods].Add(id);
        }
    }

    #endregion SaveAllData

    #region ModDetails

    private void TryShowModDetails(ModShowInfo? info)
    {
        if (info is null)
            CloseModDetails();
        else if (IsShowModDetails && NowSelectedMod?.Id == info.Id)
            CloseModDetails();
        else
            ShowModDetails(info);
    }

    private void ShowModDetails(ModShowInfo showInfo)
    {
        IsShowModDetails = true;
        Logger.Debug($"{I18nRes.ShowDetails} {showInfo.Id}");
    }

    private void CloseModDetails()
    {
        IsShowModDetails = false;
        Logger.Debug($"{I18nRes.CloseDetails}");
    }

    #endregion ModDetails

    #region AddUserGroup

    private void InitializeAddUserGroupWindowViewMode(AddUserGroupWindowViewModel viewModel)
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

    private bool TryAddUserGroup(string icon, string group)
    {
        if (
            string.IsNullOrWhiteSpace(group) is false && r_allUserGroups.ContainsKey(group) is false
        )
        {
            if (group == ModTypeGroupName.Collected || group == c_strUserCustomData)
                MessageBoxVM.Show(
                    new(
                        string.Format(
                            I18nRes.UserGroupCannotNamed,
                            ModTypeGroupName.Collected,
                            c_strUserCustomData
                        )
                    )
                    {
                        ShowMainWindowBlurEffect = false
                    }
                );
            else
            {
                AddUserGroup(icon, group);
                return true;
            }
        }
        else
            MessageBoxVM.Show(new(I18nRes.UserGroupNamingFailed));
        return false;
    }

    private void AddUserGroup(string icon, string group, bool remindSave = true)
    {
        ListBoxItemVM listBoxItem = new();
        SetListBoxItemData(listBoxItem, group);
        listBoxItem.ContextMenu = CreateUserGroupItemContextMenu(listBoxItem);
        listBoxItem.Icon = icon;
        ListBox_UserGroupMenu.Add(listBoxItem);
        r_allUserGroups.Add(group, new());
        r_allListBoxItems.Add(group, listBoxItem);
        r_allModShowInfoGroups.Add(group, new());
        AddExportUserGroupItem(group);
        Logger.Info($"{I18nRes.AddUserGroup} {icon} {group}");
        RefreshGroupModCount(remindSave);
        RefreshAllGroupItemContextMenus();
    }

    private ContextMenuVM CreateUserGroupItemContextMenu(ListBoxItemVM listBoxItem)
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
            // 启用用户分组内的所有模组
            MenuItemVM menuItem = new();
            menuItem.Icon = "✅";
            menuItem.Header = I18nRes.EnableAllMods;
            menuItem.CommandEvent += (p) =>
            {
                var name = listBoxItem.ToolTip!.ToString()!;
                ChangeModsEnabled(r_allModShowInfoGroups[name], true);
                CheckAndRefreshDisplayData();
            };
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
        MenuItemVM DisableAllUserGroupModsMenuItem(ListBoxItemVM listBoxItem)
        {
            // 禁用用户分组内所有模组
            MenuItemVM menuItem = new();
            menuItem.Icon = "❎";
            menuItem.Header = I18nRes.DisableAllMods;
            menuItem.CommandEvent += (p) =>
            {
                var name = listBoxItem.ToolTip!.ToString()!;
                ChangeModsEnabled(r_allModShowInfoGroups[name], false);
                CheckAndRefreshDisplayData();
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
                r_allUserGroups[name].Clear();
                r_allModShowInfoGroups[name].Clear();
                CheckAndRefreshDisplayData();
            };
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
        MenuItemVM RenameUserGroupMenuItem(ListBoxItemVM listBoxItem)
        {
            // 重命名分组
            MenuItemVM menuItem = new();
            menuItem.Icon = "🔄";
            menuItem.Header = I18nRes.RenameUserGroup;
            menuItem.CommandEvent += (p) => PrepareRenameUserGroup(listBoxItem);
            Logger.Debug($"{I18nRes.AddMenuItem} {menuItem.Header}");
            return menuItem;
        }
        MenuItemVM RemoveUserGroupMenuItem(ListBoxItemVM listBoxItem)
        {
            // 删除分组
            MenuItemVM menuItem = new();
            menuItem.Icon = "❌";
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
        r_allUserGroups.Remove(name);
        r_allListBoxItems.Remove(name);
        r_allModShowInfoGroups.Remove(name);
        IsRemindSave = true;
        RefreshAllGroupItemContextMenus();
        RemoveExportUserGroupItem(name);
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
        if (newName == ModTypeGroupName.Collected || newName == c_strUserCustomData)
        {
            MessageBoxVM.Show(
                new(
                    string.Format(
                        I18nRes.UserGroupCannotNamed,
                        ModTypeGroupName.Collected,
                        c_strUserCustomData
                    )
                )
                {
                    Icon = MessageBoxVM.Icon.Warning,
                    ShowMainWindowBlurEffect = false
                }
            );
            return false;
        }
        if (r_allUserGroups.ContainsKey(newName))
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
        var tempUserGroup = r_allUserGroups[name];
        r_allUserGroups.Remove(name);
        r_allUserGroups.Add(newName, tempUserGroup);
        // 重命名组名称
        var tempShowInfos = r_allModShowInfoGroups[name];
        r_allModShowInfoGroups.Remove(name);
        r_allModShowInfoGroups.Add(newName, tempShowInfos);
        // 重命名列表项
        r_allListBoxItems.Remove(name);
        r_allListBoxItems.Add(newName, listBoxItem);
        RefreshGroupModCount();
        RefreshAllGroupItemContextMenus();
        RenameExportUserGroupItem(name, newName);
    }

    private static void SetListBoxItemData(ListBoxItemVM item, string name)
    {
        item.Content = name;
        item.ToolTip = name;
        item.Tag = name;
    }

    private void AddExportUserGroupItem(string group)
    {
        ComboBox_ExportUserGroup.Add(new() { Content = group });
    }

    private void RenameExportUserGroupItem(string group, string newGroup)
    {
        var item = ComboBox_ExportUserGroup.First(i => i.Content!.ToString() == group);
        item.Content = newGroup;
    }

    private void RemoveExportUserGroupItem(string group)
    {
        // 删除导出用户分组下拉列表的此分组选择
        var item = ComboBox_ExportUserGroup.First(i => i.Content!.ToString() == group);
        ComboBox_ExportUserGroup.Remove(item);
        if (ComboBox_ExportUserGroup.SelectedItem is null)
            ComboBox_ExportUserGroup.SelectedIndex = 0;
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
            completed += await ParseDropFile(
                path,
                count,
                completed,
                pendingHandler,
                tempDirectoryInfo
            );
        }
        CheckAndRefreshDisplayData();
        tempDirectoryInfo.Delete(true);
    }

    private async Task<int> ParseDropFile(
        string path,
        int count,
        int completed,
        PendingVMHandler pendingHandler,
        DirectoryInfo tempDirectoryInfo
    )
    {
        await Task.Delay(1);
        if (Directory.Exists(path))
        {
            Logger.Info($"{I18nRes.ParseDirectory} {path}");
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)!;
            count += files.Length;
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
                await AddModFromFile(subFile, tempDirectoryInfo);
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
        return completed;
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
                Directory.GetLastWriteTime(tempDirectoryInfo.FullName),
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
        if (r_allModInfos.ContainsKey(newModInfo.Id) is false)
        {
            Utils.CopyDirectory(Path.GetDirectoryName(jsonFile)!, GameInfo.ModsDirectory);
            AddMod(newModInfo);
            return;
        }
        await TryOverwriteMod(
            jsonFile,
            modDirectory,
            r_allModInfos[newModInfo.Id],
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
            if (await Utils.UnArchiveFileToDirectory(file, tempDirectory) is false)
            {
                MessageBoxVM.Show(
                    new($"{I18nRes.UnzipError}\n {I18nRes.Path}:{file}")
                    {
                        ShowMainWindowBlurEffect = false
                    }
                );
                return null;
            }
            var filesInfo = tempDirectoryInfo.EnumerateFiles(
                c_modInfoFile,
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
            var showInfo = r_allModsShowInfo[modInfo.Id];
            var isCollected = showInfo.IsCollected;
            var isEnabled = showInfo.IsEnabled;
            if (result is MessageBoxVM.Result.Yes)
            {
                Utils.CopyDirectory(
                    modInfo.ModDirectory,
                    $"{sr_backupModsDirectory}\\{tempDirectoryInfo.Name}"
                );
                string tempDirectory = $"{sr_backupModsDirectory}\\{tempDirectoryInfo.Name}";
                await Utils.ArchiveDirectoryToFile(
                    tempDirectory,
                    sr_backupModsDirectory,
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
