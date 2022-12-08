using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using StarsectorTools.Libs;
using System.ComponentModel;
using Panuon.WPF.UI;
using System.Windows.Data;
using System.Collections.ObjectModel;
using HKW.TomlParse;
using System.Windows.Media.Imaging;
using System.Threading;
using Aspose.Zip;
using Aspose.Zip.SevenZip;
using StarsectorTools.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Runtime.CompilerServices;
using I18n = StarsectorTools.Langs.Tools.ModManager.ModManager_I18n;

namespace StarsectorTools.Tools.ModManager
{
    public partial class ModManager
    {
        public void Close()
        {
            ResetRemindSaveThread();
        }
        void InitializeData()
        {
            buttonStyle.Enabled = (Style)Resources["EnableStyle"];
            buttonStyle.Disable = (Style)Resources["DisableStyle"];
            buttonStyle.Collecte = (Style)Resources["CollecteStyle"];
            buttonStyle.CancelCollection = (Style)Resources["CancelCollectionStyle"];
            labelStyle.GameVersionNormal = (Style)Resources["GameVersionNormalStyle"];
            labelStyle.GameVersionWarn = (Style)Resources["GameVersionWarnStyle"];
            labelStyle.IsUtility = (Style)Resources["IsUtilityStyle"];
            labelStyle.NotUtility = (Style)Resources["NotUtilityStyle"];
            remindSaveThread = new(RemindSave);
        }

        void RefreshList()
        {
            allEnabledModsId = new();
            allCollectedModsId = new();
            allModsInfo = new();
            allListBoxItemsFromGroups = new();
            allModsShowInfo = new();
            allUserGroups = new();
            modsIdFromGroups = new()
            {
                {ModGroupType.Libraries,new() },
                {ModGroupType.MegaMods,new() },
                {ModGroupType.FactionMods,new() },
                {ModGroupType.ContentExpansions,new() },
                {ModGroupType.UtilityMods,new() },
                {ModGroupType.MiscellaneousMods,new() },
                {ModGroupType.BeautifyMods,new() },
                {ModGroupType.UnknownMods,new() },
            };
            modsShowInfoFromGroup = new()
            {
                {ModGroupType.All,new() },
                {ModGroupType.Enabled,new() },
                {ModGroupType.Disabled,new() },
                {ModGroupType.Libraries,new() },
                {ModGroupType.MegaMods,new() },
                {ModGroupType.FactionMods,new() },
                {ModGroupType.ContentExpansions,new() },
                {ModGroupType.UtilityMods,new() },
                {ModGroupType.MiscellaneousMods,new() },
                {ModGroupType.BeautifyMods,new() },
                {ModGroupType.UnknownMods,new() },
                {ModGroupType.Collected,new() },
            };
            while (ListBox_UserGroup.Items.Count > 1)
                ListBox_UserGroup.Items.RemoveAt(1);
            GetAllModsInfo();
            GetAllListBoxItems();
            GetAllGroup();
            InitializeDataGridItemsSource();
            CheckEnabledMods();
            CheckUserGroup();
            RefreshModsContextMenu();
            RefreshCountOfListBoxItems();
            ResetRemindSaveThread();
            GC.Collect();
        }
        void GetAllModsInfo()
        {
            int size = 0;
            DirectoryInfo dirs = new(ST.gameModsPath);
            string err = null!;
            foreach (var dir in dirs.GetDirectories())
            {
                try
                {
                    ModInfo modInfo = GetModInfo($"{dir.FullName}\\mod_info.json");
                    allModsInfo.Add(modInfo.Id, modInfo);
                    STLog.Instance.WriteLine($"{I18n.ModAddSuccess}: {modInfo.Id}", STLogLevel.DEBUG);
                }
                catch
                {
                    STLog.Instance.WriteLine($"{I18n.ModAddFailed}: {dir.Name}", STLogLevel.WARN);
                    err ??= $"{I18n.ModAddFailed}\n";
                    err += $"{dir.Name}\n";
                    size++;
                }
            }
            if (err != null)
                MessageBox.Show(err, "", MessageBoxButton.OK, MessageBoxImage.Error);
            STLog.Instance.WriteLine(I18n.ModAddSize, STLogLevel.INFO, allModsInfo.Count, size);
        }

        static ModInfo GetModInfo(string jsonPath)
        {
            string datas = File.ReadAllText(jsonPath);
            datas = Regex.Replace(datas, @"(#|//)[\S ]*", "");
            datas = Regex.Replace(datas, @",(?=[\r\n \t]*[\]\}])|(?<=[\}\]]),[ \t]*\r?\Z", "");
            JsonNode jsonData = JsonNode.Parse(datas)!;
            ModInfo modInfo = new();
            foreach (var data in jsonData.AsObject())
                modInfo.SetData(data);
            modInfo.Path = Path.GetDirectoryName(jsonPath)!;
            return modInfo;
        }
        void CheckEnabledMods()
        {
            if (!File.Exists(ST.enabledModsJsonPath))
            {
                STLog.Instance.WriteLine($"{I18n.EnabledModsFile} {I18n.NotExist} {I18n.Path}: {ST.enabledModsJsonPath}", STLogLevel.WARN);
                if (MessageBox.Show($"{I18n.EnabledModsFile} {I18n.NotExist}\n{I18n.Path}: {ST.enabledModsJsonPath}\n{I18n.CreateFile}?", "", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    STLog.Instance.WriteLine($"{I18n.CreateFile} {I18n.Path}: {ST.enabledModsJsonPath}");
                    SaveEnabledMods(ST.enabledModsJsonPath);
                }
            }
            else
                GetEnabledMods(ST.enabledModsJsonPath);
        }
        void ImportMode()
        {
            var result = MessageBox.Show(I18n.SelectImportMode, "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                ClearAllEnabledMods();
            else if (result == MessageBoxResult.Cancel)
                return;
        }
        void GetEnabledMods(string path, bool importMode = false)
        {
            string datas = File.ReadAllText(path);
            if (datas.Length > 0)
            {
                try
                {
                    string err = null!;
                    JsonNode enabledModsJson = JsonNode.Parse(datas)!;
                    if (enabledModsJson.AsObject().Count != 1 || enabledModsJson.AsObject().ElementAt(0).Key != enabledMods)
                        throw new();
                    if (importMode)
                        ImportMode();
                    JsonArray enabledModsJsonArray = enabledModsJson[enabledMods]!.AsArray();
                    STLog.Instance.WriteLine($"{I18n.LoadEnabledModsFile} {I18n.Path}: {path}");
                    foreach (var mod in enabledModsJsonArray)
                    {
                        var id = mod!.GetValue<string>();
                        if (allModsInfo.ContainsKey(id))
                        {
                            STLog.Instance.WriteLine($"{I18n.EnableMod} {id}", STLogLevel.DEBUG);
                            ChangeModEnabled(id, true);
                        }
                        else
                        {
                            STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}");
                            err ??= $"{I18n.NotFoundMod}:\n";
                            err += $"{id}\n";
                        }
                    }
                    STLog.Instance.WriteLine($"{I18n.EnableMod} {I18n.Size}: {allEnabledModsId.Count}");
                    if (err != null)
                        MessageBox.Show(err, "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch
                {
                    STLog.Instance.WriteLine($"{I18n.LoadError} {I18n.Path}: {path}");
                    MessageBox.Show($"{I18n.LoadError}\n{I18n.Path}: {path}", "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        void CheckUserGroup()
        {
            if (!File.Exists(userDataPath))
                SaveUserData(userDataPath);
            else
                GetUserData(userDataPath);
            if (!File.Exists(userGroupPath))
                SaveUserGroup(userGroupPath);
            else
                GetUserGroup(userGroupPath);
        }
        void GetUserGroup(string path)
        {
            try
            {
                string err = null!;
                using TomlTable toml = TOML.Parse(path);
                foreach (var kv in toml)
                {
                    string group = kv.Key;
                    if (!allUserGroups.ContainsKey(group))
                    {
                        AddUserGroup(kv.Value[strIcon]!, group);
                        foreach (string id in kv.Value[strMods].AsTomlArray)
                        {
                            if (allModsShowInfo.ContainsKey(id))
                            {
                                if (allUserGroups[group].Add(id))
                                    modsShowInfoFromGroup[group].Add(allModsShowInfo[id]);
                            }
                            else
                            {
                                STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}");
                                err ??= $"{I18n.NotFoundMod}\n";
                                err += $"{id}\n";
                            }
                        }
                    }
                    else
                    {
                        STLog.Instance.WriteLine($"{I18n.AlreadyExistUserGroup} {group}");
                        err ??= $"{I18n.AlreadyExistUserGroup} {group}";
                    }
                }
                if (err is not null)
                    MessageBox.Show(err, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.FileError} {path}", STLogLevel.ERROR);
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                MessageBox.Show($"{I18n.FileError} {path}", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        void GetUserData(string path)
        {
            STLog.Instance.WriteLine($"{I18n.LoadUserData} {I18n.Path}: {path}");
            string err = null!;
            List<string> errList = new();
            try
            {
                using TomlTable toml = TOML.Parse(path);
                if (!toml.Any(kv => kv.Key == ModGroupType.Collected))
                {
                    STLog.Instance.WriteLine($"{I18n.LoadError} {I18n.Path}: {path}");
                    MessageBox.Show($"{I18n.LoadError}\n{I18n.Path}: {path}", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (var kv in toml)
                {
                    if (kv.Key == ModGroupType.Collected)
                    {
                        STLog.Instance.WriteLine($"{I18n.LoadCollectedList}");
                        foreach (string id in kv.Value.AsTomlArray)
                        {
                            if (allModsShowInfo.ContainsKey(id))
                            {
                                ChangeModCollected(id, true);
                            }
                            else
                            {
                                STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}");
                                err ??= $"{I18n.NotFoundMod}\n";
                                err += $"{id}\n";
                            }
                        }
                        if (err is not null)
                            errList.Add(err);
                    }
                    else if (kv.Key == userCustomData)
                    {
                        STLog.Instance.WriteLine($"{I18n.LoadUserCustomData}");
                        foreach (var dic in kv.Value.AsTomlArray)
                        {
                            var id = dic[strId].AsString;
                            if (allModsShowInfo.ContainsKey(id))
                            {
                                var info = allModsShowInfo[id];
                                info.UserDescription = dic[strUserDescription];
                            }
                            else
                            {
                                STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}");
                                err ??= $"{I18n.NotFoundMod}\n";
                                err += $"{id}\n";
                            }
                        }
                        if (err is not null)
                            errList.Add(err);
                    }
                }
                if (errList.Count > 0)
                    MessageBox.Show(string.Join("\n", errList), "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception)
            {
                STLog.Instance.WriteLine($"{I18n.UserDataLoadError} {I18n.Path}: {path}");
                MessageBox.Show($"{I18n.UserDataLoadError}\n{I18n.Path}: {path}", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        void GetAllListBoxItems()
        {
            foreach (ListBoxItem item in ListBox_ModsGroupMenu.Items)
            {
                if (item.Content is string str)
                    allListBoxItemsFromGroups.Add(item.Tag.ToString()!, item);
                else if (item.Content is Expander expander && expander.Content is ListBox listBox)
                    foreach (ListBoxItem item1 in listBox.Items)
                        allListBoxItemsFromGroups.Add(item1.Tag.ToString()!, item1);
            }
        }

        void GetAllGroup()
        {
            if (!File.Exists(modGroupFile))
            {
                using StreamReader sr = new(Application.GetResourceStream(modGroupUri).Stream);
                File.WriteAllText(modGroupFile, sr.ReadToEnd());
            }
            try
            {
                using TomlTable toml = TOML.Parse(modGroupFile);
                foreach (var kv in toml)
                    foreach (string id in kv.Value.AsTomlArray)
                        modsIdFromGroups[kv.Key].Add(id);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.ModGroupFailedToGet} {I18n.Path}: {modGroupFile}", STLogLevel.WARN);
                STLog.Instance.WriteLine(ex.Message, STLogLevel.WARN);
                MessageBox.Show($"{I18n.ModGroupFailedToGet}\n{I18n.Path}: {modGroupFile}", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        string CheckGroup(string id)
        {
            foreach (var group in modsIdFromGroups)
                if (group.Value.Contains(id))
                    return group.Key;
            return ModGroupType.UnknownMods;
        }
        void InitializeDataGridItemsSource()
        {
            foreach (var kv in allModsInfo)
            {
                ModInfo info = kv.Value;
                ModShowInfo showInfo = GetModShowInfo(info);
                AddModShowInfo(showInfo);
                if (showInfo.Enabled is true)
                    modsShowInfoFromGroup[ModGroupType.Enabled].Add(showInfo);
                else
                    modsShowInfoFromGroup[ModGroupType.Disabled].Add(showInfo);
            }
            STLog.Instance.WriteLine($"{I18n.ModShowInfoSetSuccess} {I18n.Size}: {allModsInfo.Count}");
            ListBox_ModsGroupMenu.SelectedIndex = 0;
            CheckEnabledModsDependencies();
        }
        void ChangeShowGroup(string group)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DataGrid_ModsShowList.ItemsSource = modsShowInfoFromGroup[group]);
            STLog.Instance.WriteLine($"{I18n.ShowGroup} {group}");
        }
        void ChangeShowGroup(ObservableCollection<ModShowInfo> modsShowInfo)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DataGrid_ModsShowList.ItemsSource = modsShowInfo);
        }
        ModShowInfo GetModShowInfo(ModInfo info)
        {
            bool isCollected = CheckCollected(info.Id);
            bool isEnabled = CheckEnabled(info.Id);
            ModShowInfo showInfo = new()
            {
                Collected = isCollected,
                CollectedStyle = isCollected is true ? buttonStyle.Collecte : buttonStyle.CancelCollection,
                Enabled = isEnabled,
                EnabledStyle = isEnabled is true ? buttonStyle.Enabled : buttonStyle.Disable,
                Name = info.Name,
                Id = info.Id,
                Author = info.Author,
                Version = info.Version,
                GameVersion = info.GameVersion,
                GameVersionStyle = info.GameVersion == ST.gameVersion ? labelStyle.GameVersionNormal : labelStyle.GameVersionWarn,
                RowDetailsHight = 0,
                Dependencies = "",
                DependenciesList = info.Dependencies is not null ? info.Dependencies.Select(i => i.Id).ToList() : null!,
                ImagePath = File.Exists($"{info.Path}\\icon.ico") ? $"{info.Path}\\icon.ico" : null!,
                UserDescription = "",
                Utility = info.Utility,
                UtilityStyle = info.Utility is true ? labelStyle.IsUtility : labelStyle.NotUtility,
                Group = CheckGroup(info.Id),
            };
            STLog.Instance.WriteLine($"{info.Id} {I18n.ClassifyTo} {showInfo.Group}", STLogLevel.DEBUG);
            return showInfo;
        }
        void RefreshCountOfListBoxItems()
        {
            foreach (var item in allListBoxItemsFromGroups.Values)
            {
                int size = modsShowInfoFromGroup[item.Tag.ToString()!].Count;
                item.Content = $"{item.ToolTip} ({size})";
                STLog.Instance.WriteLine($"{I18n.GroupModCountRefresh} {item.Content}", STLogLevel.DEBUG);
            }
            STLog.Instance.WriteLine(I18n.GroupModCountRefreshComplete);
        }
        bool CheckEnabled(string id)
        {
            return allEnabledModsId.Contains(id);
        }
        bool CheckCollected(string id)
        {
            return allCollectedModsId.Contains(id);
        }
        void RefreshModsContextMenu()
        {
            foreach (var info in allModsShowInfo.Values)
                info.ContextMenu = CreateContextMenu(info);
            STLog.Instance.WriteLine($"{I18n.ContextMenuRefreshComplete} {I18n.Size}: {allModsShowInfo.Values.Count}");
        }
        ContextMenu CreateContextMenu(ModShowInfo info)
        {
            STLog.Instance.WriteLine($"{info.Id} {I18n.AddContextMenu}", STLogLevel.DEBUG);
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            MenuItem menuItem = new();
            menuItem.Header = info.Enabled is true ? I18n.DisableSelectedMods : I18n.EnabledSelectedMods;
            menuItem.Click += (o, e) => ChangeSelectedModsEnabled(info.Enabled is not true);
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);

            menuItem = new();
            menuItem.Header = info.Collected is true ? I18n.CancelCollectionSelectedMods : I18n.CollectionSelectedMods;
            menuItem.Click += (o, e) => ChangeSelectedModsCollected(info.Collected is not true);
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);

            menuItem = new();
            menuItem.Header = I18n.OpenModDirectory;
            menuItem.Click += (o, e) =>
            {
                STLog.Instance.WriteLine($"{I18n.OpenModDirectory} {I18n.Path}: {allModsInfo[info.Id].Path}");
                ST.OpenFile(allModsInfo[info.Id].Path);
            };
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);

            menuItem = new();
            menuItem.Header = I18n.DeleteMod;
            menuItem.Click += (o, e) =>
            {
                string path = allModsInfo[info.Id].Path;
                if (MessageBox.Show($"{I18n.ConfirmDeleteMod}?\nID: {info.Id}\n{I18n.Path}: {path}\n", "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    RemoveModShowInfo(info.Id);
                    ST.DeleteDirToRecycleBin(path);
                    RefreshCountOfListBoxItems();
                    StartRemindSaveThread();
                }
            };
            contextMenu.Items.Add(menuItem);

            if (allUserGroups.Count > 0)
            {
                menuItem = new();
                menuItem.Header = I18n.AddModToUserGroup;
                foreach (var group in allUserGroups.Keys)
                {
                    if (!allUserGroups[group].Contains(info.Id))
                    {
                        MenuItem groupItem = new();
                        groupItem.Header = group;
                        groupItem.Background = (Brush)Application.Current.Resources["ColorBB"];
                        // 此语句无法获取色彩透明度 原因未知
                        MenuItemHelper.SetHoverBackground(groupItem, (Brush)Application.Current.Resources["ColorSelected"]);
                        groupItem.Click += (o, e) =>
                        {
                            ChangeSelectedModsUserGroup(group, true);
                        };
                        menuItem.Items.Add(groupItem);
                    }
                }
                if (menuItem.Items.Count > 0)
                {
                    contextMenu.Items.Add(menuItem);
                    STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                }
            }
            var haveModGroup = allUserGroups.Where(g => g.Value.Contains(info.Id));
            if (haveModGroup.Count() > 0)
            {
                menuItem = new();
                menuItem.Header = I18n.RemoveFromUserGroup;
                foreach (var group in haveModGroup)
                {
                    MenuItem groupItem = new();
                    groupItem.Header = group.Key;
                    groupItem.Background = (Brush)Application.Current.Resources["ColorBG"];
                    // 此语句无法获取色彩透明度 原因未知
                    MenuItemHelper.SetHoverBackground(groupItem, (Brush)Application.Current.Resources["ColorBB"]);
                    groupItem.Click += (o, e) =>
                    {
                        ChangeSelectedModsUserGroup(group.Key, false);
                    };
                    menuItem.Items.Add(groupItem);
                }
                contextMenu.Items.Add(menuItem);
                STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
            }
            return contextMenu;
        }
        void ChangeSelectedModsUserGroup(string group, bool status)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo info = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ChangeModUserGroup(group, info.Id, status);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            RefreshCountOfListBoxItems();
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModInfo();
            StartRemindSaveThread();
        }
        void ChangeModUserGroup(string group, string id, bool status)
        {
            ModShowInfo info = allModsShowInfo[id];
            if (status)
            {
                if (allUserGroups[group].Add(id))
                    modsShowInfoFromGroup[group].Add(allModsShowInfo[id]);
            }
            else
            {
                allUserGroups[group].Remove(id);
                modsShowInfoFromGroup[group].Remove(allModsShowInfo[id]);
            }
            info.ContextMenu = CreateContextMenu(info);
            STLog.Instance.WriteLine(I18n.ChangeModUserGroup, STLogLevel.DEBUG, id, group, status);
        }
        void ChangeSelectedModsEnabled(bool? enabled = null)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo info = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ChangeModEnabled(info.Id, enabled);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            RefreshCountOfListBoxItems();
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModInfo();
            CheckEnabledModsDependencies();
            StartRemindSaveThread();
        }
        void ClearAllEnabledMods()
        {
            while (allEnabledModsId.Count > 0)
                ChangeModEnabled(allEnabledModsId.ElementAt(0), false);
            STLog.Instance.WriteLine(I18n.DisableAllEnabledMods);
        }
        void ChangeModEnabled(string id, bool? enabled = null)
        {
            ModShowInfo info = allModsShowInfo[id];
            info.Enabled = enabled is null ? !info.Enabled : enabled;
            info.EnabledStyle = info.Enabled is true ? buttonStyle.Enabled : buttonStyle.Disable;
            info.ContextMenu = CreateContextMenu(info);
            if (info.Enabled is true)
            {
                if (allEnabledModsId.Add(info.Id))
                {
                    modsShowInfoFromGroup[ModGroupType.Enabled].Add(info);
                    modsShowInfoFromGroup[ModGroupType.Disabled].Remove(info);
                }
            }
            else
            {
                if (allEnabledModsId.Remove(info.Id))
                {
                    modsShowInfoFromGroup[ModGroupType.Enabled].Remove(info);
                    modsShowInfoFromGroup[ModGroupType.Disabled].Add(info);
                    info.RowDetailsHight = 0;
                }
            }
            STLog.Instance.WriteLine($"{id} {I18n.ModEnabledStatus} {info.Enabled}", STLogLevel.DEBUG);
        }
        void CheckEnabledModsDependencies()
        {
            foreach (var info in modsShowInfoFromGroup[ModGroupType.Enabled])
            {
                if (info.DependenciesList != null)
                {
                    info.Dependencies = string.Join(" , ", info.DependenciesList.Where(s => !allEnabledModsId.Contains(s)));
                    if (info.Dependencies.Length > 0)
                    {
                        STLog.Instance.WriteLine($"{info.Id} {I18n.NotEnableDependencies} {info.Dependencies}");
                        info.RowDetailsHight = 30;
                    }
                    else
                        info.RowDetailsHight = 0;
                }
            }
        }
        void ChangeSelectedModsCollected(bool? collected = null)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo info = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ChangeModCollected(info.Id, collected);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            RefreshCountOfListBoxItems();
            StartRemindSaveThread();
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModInfo();
        }
        void ChangeModCollected(string id, bool? collected = null)
        {
            ModShowInfo info = allModsShowInfo[id];
            info.Collected = collected is null ? !info.Collected : collected;
            info.CollectedStyle = info.Collected is true ? buttonStyle.Collecte : buttonStyle.CancelCollection;
            info.ContextMenu = CreateContextMenu(info);
            if (info.Collected is true)
            {
                if (!CheckCollected(info.Id))
                {
                    allCollectedModsId.Add(info.Id);
                    modsShowInfoFromGroup[ModGroupType.Collected].Add(info);
                }
            }
            else
            {
                allCollectedModsId.Remove(info.Id);
                modsShowInfoFromGroup[ModGroupType.Collected].Remove(info);
            }
            STLog.Instance.WriteLine($"{id} {I18n.ModCollectedStatus} {info.Collected}", STLogLevel.DEBUG);
        }
        void SaveAllData()
        {
            SaveEnabledMods(ST.enabledModsJsonPath);
            SaveUserData(userDataPath);
            SaveUserGroup(userGroupPath);
        }
        void SaveEnabledMods(string path)
        {
            JsonObject keyValues = new()
            {
                { enabledMods, new JsonArray() }
            };
            foreach (var mod in allEnabledModsId)
                ((JsonArray)keyValues[enabledMods]!).Add(mod);
            File.WriteAllText(path, keyValues.ToJsonString(new() { WriteIndented = true }));
            STLog.Instance.WriteLine($"{I18n.SaveEnabledListSuccess} {I18n.Path}: {path}");
        }
        void SaveUserData(string path)
        {
            using TomlTable toml = new()
            {
                { ModGroupType.Collected, new TomlArray() },
                { strUserCustomData, new TomlArray() }
            };
            foreach (var info in allModsShowInfo.Values)
            {
                if (info.Collected is true)
                    toml[ModGroupType.Collected].Add(info.Id);
                if (info.UserDescription!.Length > 0)
                {
                    toml[strUserCustomData].Add(new TomlTable()
                    {
                        [strId] = info.Id,
                        [strUserDescription] = info.UserDescription!.Length > 0 ? info.UserDescription : "",
                    });
                }
            }
            toml.SaveTo(path);
            STLog.Instance.WriteLine($"{I18n.SaveUserDataSuccess} {I18n.Path}: {path}");
        }
        void SaveUserGroup(string path, string tag = strAll)
        {
            TomlTable toml = new();
            if (tag == strAll)
            {
                foreach (var kv in allUserGroups)
                {
                    toml.Add(kv.Key, new TomlTable()
                    {
                        [strIcon] = ((Emoji.Wpf.TextBlock)ListBoxItemHelper.GetIcon(allListBoxItemsFromGroups[kv.Key])).Text,
                        [strMods] = new TomlArray(),
                    });
                    foreach (var id in kv.Value)
                        toml[kv.Key][strMods].Add(id);
                }
            }
            else
            {
                var mods = allUserGroups[tag];
                toml.Add(tag, new TomlTable()
                {
                    [strIcon] = ((Emoji.Wpf.TextBlock)ListBoxItemHelper.GetIcon(allListBoxItemsFromGroups[tag])).Text,
                    [strMods] = new TomlArray(),
                });
                foreach (var id in mods)
                    toml[tag][strMods].Add(id);
            }
            toml.SaveTo(path);
            STLog.Instance.WriteLine($"{I18n.SaveUserGroupSuccess} {I18n.Path}: {path}");
        }
        void ChangeModInfoShow(string id)
        {
            if (showModInfo)
            {
                if (nowSelectedMod != id)
                    SetModInfo(id);
                else
                    CloseModInfo();
            }
            else
            {
                ShowModInfo(id);
            }
        }
        public void ShowModInfo(string id)
        {
            Grid_ModInfo.Visibility = Visibility.Visible;
            showModInfo = true;
            nowSelectedMod = id;
            SetModInfo(id);
        }
        public void CloseModInfo()
        {
            Grid_ModInfo.Visibility = Visibility.Hidden;
            showModInfo = false;
            nowSelectedMod = null;
            TextBox_UserDescription.Text = "";
            STLog.Instance.WriteLine($"{I18n.CloseDetails} {nowSelectedMod}", STLogLevel.DEBUG);
        }
        void SetModInfo(string id)
        {
            ModInfo info = allModsInfo[id];
            if (allModsShowInfo[info.Id].ImagePath is string imagePath && imagePath.Length > 0)
                Image_ModImage.Source = new BitmapImage(new(imagePath));
            else
                Image_ModImage.Source = null;
            Label_ModName.Content = info.Name;
            Label_ModId.Content = info.Id;
            Label_ModVersion.Content = info.Version;
            Label_GameVersion.Content = info.GameVersion;
            Button_ModPath.Content = info.Path;
            TextBlock_ModAuthor.Text = info.Author;
            if (info.Dependencies is List<ModInfo> list)
            {
                GroupBox_ModDependencies.Visibility = Visibility.Visible;
                TextBlock_ModDependencies.Text = string.Join("\n", list.Select(i => $"{I18n.Name}: {i.Name} ID: {i.Id} " + (i.Version is not null ? $"{I18n.Version} {i.Version}" : ""))!);
            }
            else
                GroupBox_ModDependencies.Visibility = Visibility.Collapsed;
            TextBlock_ModDescription.Text = info.Description;
            TextBox_UserDescription.Text = allModsShowInfo[info.Id].UserDescription!;
            STLog.Instance.WriteLine($"{I18n.ShowDetails} {id}", STLogLevel.DEBUG);
        }
        void DropFile(string filePath)
        {
            string tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}Temp";
            if (!ST.UnArchiveFileToDir(filePath, tempPath))
            {
                MessageBox.Show($"{I18n.UnzipError}\n {I18n.Path}:{filePath}");
                return;
            }
            DirectoryInfo dirs = new(tempPath);
            var filesInfo = dirs.GetFiles(modInfoJson, SearchOption.AllDirectories);
            if (filesInfo.Length > 0 && filesInfo.First() is FileInfo fileInfo && fileInfo.FullName is string jsonPath)
            {
                var newModInfo = GetModInfo(jsonPath);
                if (allModsInfo.ContainsKey(newModInfo.Id))
                {
                    var originalModInfo = allModsInfo[newModInfo.Id];
                    if (MessageBox.Show($"{newModInfo.Id}\n{string.Format(I18n.SameModAlreadyExists, originalModInfo.Version, newModInfo.Version)}", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        ST.CopyDirectory(originalModInfo.Path, $"{modBackupDirectory}\\Temp");
                        new Task(() =>
                        {
                            string name = Path.GetFileName(fileInfo.DirectoryName)!;
                            string tempDir = $"{modBackupDirectory}\\Temp";
                            ST.ArchiveDirToDir(tempDir, modBackupDirectory, name);
                            Directory.Delete(tempDir, true);
                        }).Start();
                        Directory.Delete(originalModInfo.Path, true);
                        ST.CopyDirectory(Path.GetDirectoryName(jsonPath)!, ST.gameModsPath);
                        allModsInfo.Remove(newModInfo.Id);
                        allModsInfo.Add(newModInfo.Id, newModInfo);
                        Dispatcher.BeginInvoke(() =>
                        {
                            RemoveModShowInfo(newModInfo.Id);
                            AddModShowInfo(GetModShowInfo(newModInfo));
                            StartRemindSaveThread();
                        });
                        STLog.Instance.WriteLine($"{I18n.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}");
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        AddModShowInfo(GetModShowInfo(newModInfo));
                        StartRemindSaveThread();
                    });
                }
            }
            else
            {
                STLog.Instance.WriteLine($"{I18n.ZipFileError} {I18n.Path}: {filePath}");
                MessageBox.Show($"{I18n.ZipFileError}\n{I18n.Path}: {filePath}");
            }
            dirs.Delete(true);
        }
        ModShowInfo RemoveModShowInfo(string id)
        {
            var modShowInfo = allModsShowInfo[id];
            allModsShowInfo.Remove(modShowInfo.Id);
            modsShowInfoFromGroup[ModGroupType.All].Remove(modShowInfo);
            modsShowInfoFromGroup[modShowInfo.Group!].Remove(modShowInfo);
            STLog.Instance.WriteLine($"{I18n.RemoveMod} {modShowInfo.Id} {modShowInfo.Version}", STLogLevel.DEBUG);
            return modShowInfo;
        }
        void AddModShowInfo(ModShowInfo modShowInfo)
        {
            allModsShowInfo.Add(modShowInfo.Id, modShowInfo);
            modsShowInfoFromGroup[ModGroupType.All].Add(modShowInfo);
            modsShowInfoFromGroup[modShowInfo.Group!].Add(modShowInfo);
            STLog.Instance.WriteLine($"{I18n.AddMod} {modShowInfo.Id} {modShowInfo.Version}", STLogLevel.DEBUG);
        }
        void ClearDataGridSelected()
        {
            while (DataGrid_ModsShowList.SelectedItems.Count > 0)
            {
                if (DataGrid_ModsShowList.ItemContainerGenerator.ContainerFromItem(DataGrid_ModsShowList.SelectedItems[0]) is DataGridRow row)
                    row.IsSelected = false;
            }
        }
        void AddUserGroup(string icon, string name)
        {
            ListBoxItem listBoxItem = new();
            // 调用全局资源需要写全
            listBoxItem.Style = (Style)Application.Current.Resources["ListBoxItem_Style"];
            SetListBoxItemData(listBoxItem, name);
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            MenuItem menuItem = new();
            menuItem.Header = I18n.ReplaceUserGroupName;
            menuItem.Click += (o, e) =>
            {
                ReplaceUserGroupName((ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)o)));
            };
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);

            menuItem = new();
            menuItem.Header = I18n.RemoveUserGroup;
            menuItem.Click += (o, e) =>
            {
                RemoveUserGroup((ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)o)));
            };
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);

            listBoxItem.ContextMenu = contextMenu;
            ListBoxItemHelper.SetIcon(listBoxItem, new Emoji.Wpf.TextBlock() { Text = icon });
            ListBox_UserGroup.Items.Add(listBoxItem);
            allUserGroups.Add(name, new());
            allListBoxItemsFromGroups.Add(name, listBoxItem);
            modsShowInfoFromGroup.Add(name, new());
            ComboBox_ExportUserGroup.Items.Add(new ComboBoxItem() { Content = name, Tag = name, Style = (Style)Application.Current.Resources["ComboBoxItem_Style"] });
            STLog.Instance.WriteLine($"{I18n.AddUserGroup} {icon} {name}");
        }

        void RemoveUserGroup(ListBoxItem listBoxItem)
        {
            var name = listBoxItem.ToolTip.ToString()!;
            if (nowSelectedListBoxItem == listBoxItem)
                ListBox_ModsGroupMenu.SelectedIndex = 0;
            ListBox_UserGroup.Items.Remove(listBoxItem);
            allUserGroups.Remove(name);
            allListBoxItemsFromGroups.Remove(name);
            modsShowInfoFromGroup.Remove(name);
            RefreshModsContextMenu();
            StartRemindSaveThread();
            Expander_RandomEnable.Visibility = Visibility.Collapsed;
            for (int i = 0; i < ComboBox_ExportUserGroup.Items.Count; i++)
            {
                if (ComboBox_ExportUserGroup.Items.GetItemAt(i) is ComboBoxItem comboBoxItem && comboBoxItem.Content.ToString()! == name)
                {
                    ComboBox_ExportUserGroup.Items.RemoveAt(i);
                    ComboBox_ExportUserGroup.SelectedIndex = 0;
                }
            }
            GC.Collect();
        }

        void ReplaceUserGroupName(ListBoxItem listBoxItem)
        {
            string icon = ((Emoji.Wpf.TextBlock)ListBoxItemHelper.GetIcon(listBoxItem)).Text;
            string name = listBoxItem.ToolTip.ToString()!;
            AddUserGroup window = new();
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;
            window.TextBox_Icon.Text = icon;
            window.TextBox_Name.Text = name;
            window.Show();
            window.Button_Yes.Click += (o, e) =>
            {
                string _icon = window.TextBox_Icon.Text;
                string _name = window.TextBox_Name.Text;
                if (_name == ModGroupType.Collected || _name == userCustomData)
                {
                    MessageBox.Show(string.Format(I18n.UserGroupCannotNamed, ModGroupType.Collected, userCustomData), "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (name == _name || !allUserGroups.ContainsKey(_name))
                {
                    ListBoxItemHelper.SetIcon(listBoxItem, new Emoji.Wpf.TextBlock() { Text = _icon });
                    var temp = allUserGroups[name];
                    allUserGroups.Remove(name);
                    allUserGroups.Add(_name, temp);

                    allListBoxItemsFromGroups.Remove(name);
                    allListBoxItemsFromGroups.Add(_name, listBoxItem);

                    var _temp = modsShowInfoFromGroup[name];
                    modsShowInfoFromGroup.Remove(name);
                    modsShowInfoFromGroup.Add(_name, _temp);

                    window.Close();
                    SetListBoxItemData(listBoxItem, _name);
                    RefreshCountOfListBoxItems();
                    RefreshModsContextMenu();
                    StartRemindSaveThread();
                }
                else
                    MessageBox.Show(I18n.AddUserNamingFailed);
            };
            window.Button_Cancel.Click += (o, e) => window.Close();
            window.Closed += (o, e) => ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
            GC.Collect();
        }
        void SetListBoxItemData(ListBoxItem item, string name)
        {
            item.Content = name;
            item.ToolTip = name;
            item.Tag = name;
        }
        void SearchMods(string text)
        {
            var type = ((ComboBoxItem)ComboBox_SearchType.SelectedItem).Tag.ToString()!;
            if (text.Length > 0)
            {
                ChangeShowGroup(GetSearchModsShowInfo(text, type));
                STLog.Instance.WriteLine($"{I18n.SearchMod} {text}", STLogLevel.DEBUG);
            }
            else
            {
                ChangeShowGroup(nowGroup);
                GC.Collect();
            }
        }

        ObservableCollection<ModShowInfo> GetSearchModsShowInfo(string text, string type)
        {
            ObservableCollection<ModShowInfo> showModsInfo = null!;
            showModsInfo = new(
            type switch
            {
                strName => modsShowInfoFromGroup[nowGroup].Where(i => i.Name!.Contains(text, StringComparison.OrdinalIgnoreCase)),
                strId => modsShowInfoFromGroup[nowGroup].Where(i => i.Id.Contains(text, StringComparison.OrdinalIgnoreCase)),
                strAuthor => modsShowInfoFromGroup[nowGroup].Where(i => i.Author!.Contains(text, StringComparison.OrdinalIgnoreCase)),
                strUserDescription => modsShowInfoFromGroup[nowGroup].Where(i => i.UserDescription!.Contains(text, StringComparison.OrdinalIgnoreCase)),
                _ => throw new NotImplementedException()
            });
            return showModsInfo;
        }
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        void RemindSave()
        {
            while (remindSaveThread.ThreadState != ThreadState.Unstarted)
            {
                Dispatcher.BeginInvoke(() => Button_Save.Background = (Brush)Application.Current.Resources["ColorAqua0"]);
                Thread.Sleep(1000);
                Dispatcher.BeginInvoke(() => Button_Save.Background = (Brush)Application.Current.Resources["ColorLight2"]);
                Thread.Sleep(1000);
            }
        }
        void StartRemindSaveThread()
        {
            if (remindSaveThread.ThreadState == ThreadState.Unstarted)
                remindSaveThread.Start();
        }
        void ResetRemindSaveThread()
        {
            if (remindSaveThread.ThreadState != ThreadState.Unstarted)
                remindSaveThread.Join(1);
            remindSaveThread = new(RemindSave);
        }
    }
}
