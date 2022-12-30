using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HKW.TomlParse;
using Panuon.WPF.UI;
using StarsectorTools.Libs;
using I18n = StarsectorTools.Langs.Tools.ModManager.ModManager_I18n;

namespace StarsectorTools.Tools.ModManager
{
    public partial class ModManager
    {
        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            ResetRemindSaveThread();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void InitializeData()
        {
            remindSaveThread = new(RemindSave);
            AllEnabledModsId = allEnabledModsId = new();
            AllCollectedModsId = allCollectedModsId = new();
            AllModsInfo = allModsInfo = new();
            allListBoxItems = new();
            allModsShowInfo = new();
            AllUserGroups = allUserGroups = new();
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
            DirectoryInfo dirs = new(ST.gameModsDirectory);
            string err = null!;
            foreach (var dir in dirs.GetDirectories())
            {
                try
                {
                    ModInfo info = GetModInfo($"{dir.FullName}\\{modInfoFile}");
                    allModsInfo.Add(info.Id, info);
                    STLog.Instance.WriteLine($"{I18n.ModAddSuccess}: {info.Id}", STLogLevel.DEBUG);
                }
                catch (Exception ex)
                {
                    STLog.Instance.WriteLine($"{I18n.ModAddFailed}: {dir.Name}", ex);
                    err ??= $"{I18n.ModAddFailed}\n";
                    err += $"{dir.Name}\n";
                    size++;
                }
            }
            STLog.Instance.WriteLine(I18n.ModAddCompleted, STLogLevel.INFO, allModsInfo.Count, size);
            if (err != null)
                ST.ShowMessageBox(err, MessageBoxImage.Warning);
        }

        private static ModInfo GetModInfo(string jsonPath)
        {
            string datas = File.ReadAllText(jsonPath);
            JsonNode jsonData = JsonNode.Parse(ST.JsonParse(datas))!;
            ModInfo modInfo = new();
            foreach (var data in jsonData.AsObject())
                modInfo.SetData(data);
            modInfo.Path = Path.GetDirectoryName(jsonPath)!;
            return modInfo;
        }

        private void CheckEnabledMods()
        {
            if (File.Exists(ST.enabledModsJsonFile))
                GetEnabledMods(ST.enabledModsJsonFile);
            else
                SaveEnabledMods(ST.enabledModsJsonFile);
        }

        private void ImportMode()
        {
            var result = ST.ShowMessageBox(I18n.SelectImportMode, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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
                JsonNode enabledModsJson = JsonNode.Parse(ST.JsonParse(datas))!;
                if (enabledModsJson.AsObject().Count != 1 || enabledModsJson.AsObject().ElementAt(0).Key != strEnabledMods)
                    throw new();
                if (importMode)
                    ImportMode();
                JsonArray enabledModsJsonArray = enabledModsJson[strEnabledMods]!.AsArray();
                STLog.Instance.WriteLine($"{I18n.LoadEnabledModsFile} {I18n.Path}: {filePath}");
                foreach (var modId in enabledModsJsonArray)
                {
                    var id = modId!.GetValue<string>();
                    if (string.IsNullOrEmpty(id))
                        continue;
                    if (allModsInfo.ContainsKey(id))
                    {
                        STLog.Instance.WriteLine($"{I18n.EnableMod} {id}", STLogLevel.DEBUG);
                        ChangeModEnabled(id, true);
                    }
                    else
                    {
                        STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
                        err ??= $"{I18n.NotFoundMod}:\n";
                        err += $"{id}\n";
                    }
                }
                STLog.Instance.WriteLine($"{I18n.EnableMod} {I18n.Size}: {allEnabledModsId.Count}");
                if (err != null)
                    ST.ShowMessageBox(err, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.LoadError} {I18n.Path}: {filePath}", ex);
                ST.ShowMessageBox($"{I18n.LoadError}\n{I18n.Path}: {filePath}", MessageBoxImage.Error);
            }
        }

        private void CheckUserData()
        {
            if (File.Exists(userDataFile))
                GetUserData(userDataFile);
            else
                SaveUserData(userDataFile);
            if (File.Exists(userGroupFile))
                GetUserGroup(userGroupFile);
            else
                SaveUserGroup(userGroupFile);
        }

        private void GetUserGroup(string filePath)
        {
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
                                STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
                                err ??= $"{I18n.NotFoundMod}\n";
                                err += $"{id}\n";
                            }
                        }
                    }
                    else
                    {
                        STLog.Instance.WriteLine($"{I18n.DuplicateUserGroupName} {group}");
                        err ??= $"{I18n.DuplicateUserGroupName} {group}";
                    }
                }
                if (err is not null)
                    ST.ShowMessageBox(err, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.FileError} {filePath}", ex);
                ST.ShowMessageBox($"{I18n.FileError} {filePath}", MessageBoxImage.Error);
            }
        }

        private void GetUserData(string filePath)
        {
            STLog.Instance.WriteLine($"{I18n.LoadUserData} {I18n.Path}: {filePath}");
            string err = null!;
            try
            {
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
                        STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
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
                        STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}", STLogLevel.WARN);
                        err ??= $"{I18n.NotFoundMod}\n";
                        err += $"{id}\n";
                    }
                }
                if (!string.IsNullOrEmpty(err))
                    ST.ShowMessageBox(err, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.UserDataLoadError} {I18n.Path}: {filePath}", ex);
                ST.ShowMessageBox($"{I18n.UserDataLoadError}\n{I18n.Path}: {filePath}", MessageBoxImage.Error);
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
            STLog.Instance.WriteLine(I18n.ListBoxItemsRetrievalCompleted);
        }

        private void GetTypeGroup()
        {
            if (!File.Exists(modTypeGroupFile))
                CreateTypeModGroup();
            try
            {
                TomlTable toml = TOML.Parse(modTypeGroupFile);
                foreach (var kv in toml)
                    foreach (string id in kv.Value.AsTomlArray)
                        allModsTypeGroup.Add(id, kv.Key);
                STLog.Instance.WriteLine(I18n.TypeGroupRetrievalCompleted);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.ModGroupFailedToGet} {I18n.Path}: {modTypeGroupFile}", ex);
                ST.ShowMessageBox($"{I18n.ModGroupFailedToGet}\n{I18n.Path}: {modTypeGroupFile}", MessageBoxImage.Error);
                CreateTypeModGroup();
            }
            void CreateTypeModGroup()
            {
                using StreamReader sr = new(Application.GetResourceStream(modTypeGroupUri).Stream);
                File.WriteAllText(modTypeGroupFile, sr.ReadToEnd());
            }
        }
        private string CheckTypeGroup(string id)
        {
            return allModsTypeGroup.ContainsKey(id) ? allModsTypeGroup[id] : ModTypeGroup.UnknownMods;
        }

        private void GetAllModsShowInfo()
        {
            foreach (var modInfo in allModsInfo.Values)
                AddModShowInfo(modInfo, false);
            STLog.Instance.WriteLine($"{I18n.ModShowInfoSetSuccess} {I18n.Size}: {allModsInfo.Count}");
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
                    STLog.Instance.WriteLine($"{I18n.SearchMod} {text}", STLogLevel.DEBUG);
                }
                else
                {
                    DataGrid_ModsShowList.ItemsSource = allModShowInfoGroups[nowGroupName];
                    STLog.Instance.WriteLine($"{I18n.ShowGroup} {nowGroupName}");
                    GC.Collect();
                }
            });
        }

        private ObservableCollection<ModShowInfo> GetSearchModsShowInfo(string text, string type)
        {
            return new ObservableCollection<ModShowInfo>(type switch
            {
                strName => allModShowInfoGroups[nowGroupName].Where(i => i.Name.Contains(text, StringComparison.OrdinalIgnoreCase)),
                strId => allModShowInfoGroups[nowGroupName].Where(i => i.Id.Contains(text, StringComparison.OrdinalIgnoreCase)),
                strAuthor => allModShowInfoGroups[nowGroupName].Where(i => i.Author.Contains(text, StringComparison.OrdinalIgnoreCase)),
                strUserDescription => allModShowInfoGroups[nowGroupName].Where(i => i.UserDescription.Contains(text, StringComparison.OrdinalIgnoreCase)),
                _ => throw new()
            });
        }

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
                IsSameToGameVersion = info.GameVersion == ST.gameVersion,
                MissDependencies = false,
                DependenciesList = info.Dependencies is not null ? info.Dependencies.Select(i => i.Id).ToList() : null!,
                ImageSource = GetIcon($"{info.Path}\\icon.ico"),
            };
            BitmapImage? GetIcon(string filePath)
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
                    STLog.Instance.WriteLine($"{I18n.IconLoadError} {I18n.Path}: {filePath}", ex);
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
                STLog.Instance.WriteLine($"{I18n.ModCountInGroupRefresh} {item.Content}", STLogLevel.DEBUG);
            }
            STLog.Instance.WriteLine(I18n.ModCountInGroupRefreshCompleted);
        }

        private void RefreshModsContextMenu()
        {
            foreach (var showInfo in allModsShowInfo.Values)
                showInfo.ContextMenu = CreateContextMenu(showInfo);
            STLog.Instance.WriteLine($"{I18n.ContextMenuRefreshCompleted} {I18n.Size}: {allModsShowInfo.Values.Count}");
        }

        private ContextMenu CreateContextMenu(ModShowInfo showInfo)
        {
            STLog.Instance.WriteLine($"{showInfo.Id} {I18n.AddContextMenu}", STLogLevel.DEBUG);
            ContextMenu contextMenu = new();
            // 标记菜单项是否被创建
            contextMenu.Tag = false;
            // 被点击时才加载菜单,可以降低内存占用
            contextMenu.Loaded += (s, e) =>
            {
                contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
                if (contextMenu.Tag is true)
                    return;
                // 启用或禁用
                MenuItem menuItem = new();
                menuItem.Header = showInfo.IsEnabled ? I18n.DisableSelectedMods : I18n.EnabledSelectedMods;
                menuItem.Click += (s, e) => ChangeSelectedModsEnabled();
                contextMenu.Items.Add(menuItem);
                STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 收藏或取消收藏
                menuItem = new();
                menuItem.Header = showInfo.IsCollected ? I18n.UncollectSelectedMods : I18n.CollectSelectedMods;
                menuItem.Click += (s, e) => ChangeSelectedModsCollected();
                contextMenu.Items.Add(menuItem);
                STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 打开模组文件夹
                menuItem = new();
                menuItem.Header = I18n.OpenModDirectory;
                menuItem.Click += (s, e) =>
                {
                    STLog.Instance.WriteLine($"{I18n.OpenModDirectory} {I18n.Path}: {allModsInfo[showInfo.Id].Path}");
                    ST.OpenFile(allModsInfo[showInfo.Id].Path);
                };
                contextMenu.Items.Add(menuItem);
                STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 删除模组至回收站
                menuItem = new();
                menuItem.Header = I18n.DeleteMod;
                menuItem.Click += (s, e) =>
                {
                    string path = allModsInfo[showInfo.Id].Path;
                    if (ST.ShowMessageBox($"{I18n.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18n.Path}: {path}\n", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        STLog.Instance.WriteLine($"{I18n.ConfirmModDeletion}?\nID: {showInfo.Id}\n{I18n.Path}: {path}\n");
                        RemoveMod(showInfo.Id);
                        RefreshDataGrid();
                        CloseModDetails();
                        ST.DeleteDirToRecycleBin(path);
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
                        STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
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
                        STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
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
                    STLog.Instance.WriteLine($"{id} {I18n.AddModToUserGroup} {group}", STLogLevel.DEBUG);
                }
            }
            else
            {
                if (allUserGroups[group].Remove(id))
                {
                    allModShowInfoGroups[group].Remove(allModsShowInfo[id]);
                    STLog.Instance.WriteLine($"{id} {I18n.RemoveFromUserGroup} {group}", STLogLevel.DEBUG);
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
            STLog.Instance.WriteLine(I18n.DisableAllEnabledMods);
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
            STLog.Instance.WriteLine($"{id} {I18n.ChangeEnabledStateTo} {showInfo.IsEnabled}", STLogLevel.DEBUG);
        }

        private void CheckEnabledModsDependencies()
        {
            foreach (var showInfo in allModShowInfoGroups[ModTypeGroup.Enabled])
            {
                if (showInfo.DependenciesList != null)
                {
                    showInfo.Dependencies = string.Join(" , ", showInfo.DependenciesList.Where(s => !allEnabledModsId.Contains(s)));
                    if (showInfo.Dependencies.Length > 0)
                    {
                        STLog.Instance.WriteLine($"{showInfo.Id} {I18n.NotEnableDependencies} {showInfo.Dependencies}");
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
            STLog.Instance.WriteLine($"{id} {I18n.ChangeCollectStateTo} {showInfo.IsCollected}", STLogLevel.DEBUG);
        }

        private void SaveAllData()
        {
            SaveEnabledMods(ST.enabledModsJsonFile);
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
            STLog.Instance.WriteLine($"{I18n.EnabledListSaveCompleted} {I18n.Path}: {filePath}");
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
            STLog.Instance.WriteLine($"{I18n.SaveUserDataSuccess} {I18n.Path}: {filePath}");
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
            STLog.Instance.WriteLine($"{I18n.UserGroupSaveCompleted} {I18n.Path}: {filePath}");
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
            STLog.Instance.WriteLine($"{I18n.CloseDetails} {nowSelectedModId}", STLogLevel.DEBUG);
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
            if (info.Dependencies is List<ModInfo> list)
            {
                GroupBox_ModDependencies.Visibility = Visibility.Visible;
                TextBlock_ModDependencies.Text = string.Join("\n", list.Select(i => $"{I18n.Name}: {i.Name} ID: {i.Id} " + (i.Version is not null ? $"{I18n.Version} {i.Version}" : ""))!);
            }
            else
                GroupBox_ModDependencies.Visibility = Visibility.Collapsed;
            TextBlock_ModDescription.Text = info.Description;
            TextBox_UserDescription.Text = showInfo.UserDescription!;
            STLog.Instance.WriteLine($"{I18n.ShowDetails} {id}", STLogLevel.DEBUG);
        }

        private void DropFile(string filePath)
        {
            string tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}Temp";
            if (!ST.UnArchiveFileToDir(filePath, tempPath))
            {
                ST.ShowMessageBox($"{I18n.UnzipError}\n {I18n.Path}:{filePath}");
                return;
            }
            DirectoryInfo dirs = new(tempPath);
            var filesInfo = dirs.GetFiles(modInfoFile, SearchOption.AllDirectories);
            if (filesInfo.Length > 0 && filesInfo.First() is FileInfo fileInfo && fileInfo.FullName is string jsonPath)
            {
                var newModInfo = GetModInfo(jsonPath);
                string directoryName = Path.GetFileName(fileInfo.DirectoryName)!;
                newModInfo.Path = $"{ST.gameModsDirectory}\\{directoryName}";
                if (allModsInfo.ContainsKey(newModInfo.Id))
                {
                    var originalModInfo = allModsInfo[newModInfo.Id];
                    if (ST.ShowMessageBox($"{newModInfo.Id}\n{string.Format(I18n.DuplicateModExists, originalModInfo.Version, newModInfo.Version)}", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        ST.CopyDirectory(originalModInfo.Path, $"{backupModsDirectory}\\Temp");
                        string tempDirectory = $"{backupModsDirectory}\\Temp";
                        ST.ArchiveDirToDir(tempDirectory, backupModsDirectory, directoryName);
                        Directory.Delete(tempDirectory, true);
                        Directory.Delete(originalModInfo.Path, true);
                        ST.CopyDirectory(Path.GetDirectoryName(jsonPath)!, ST.gameModsDirectory);
                        Dispatcher.BeginInvoke(() =>
                        {
                            RemoveMod(newModInfo.Id);
                            AddMod(newModInfo);
                            RefreshCountOfListBoxItems();
                            StartRemindSaveThread();
                        });
                        STLog.Instance.WriteLine($"{I18n.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}");
                    }
                }
                else
                {
                    ST.CopyDirectory(Path.GetDirectoryName(jsonPath)!, ST.gameModsDirectory);
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
                STLog.Instance.WriteLine($"{I18n.ZipFileError} {I18n.Path}: {filePath}");
                ST.ShowMessageBox($"{I18n.ZipFileError}\n{I18n.Path}: {filePath}");
            }
            Dispatcher.BeginInvoke(() => dirs.Delete(true));
        }

        private void RemoveMod(string id)
        {
            var modInfo = allModsInfo[id];
            allModsInfo.Remove(id);
            RemoveModShowInfo(id);
            STLog.Instance.WriteLine($"{I18n.RemoveMod} {id} {modInfo.Version}", STLogLevel.DEBUG);
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
            STLog.Instance.WriteLine($"{I18n.RemoveMod} {modInfo.Id} {modInfo.Version}", STLogLevel.DEBUG);
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
            STLog.Instance.WriteLine($"{I18n.AddMod} {showInfo.Id} {showInfo.Version}", STLogLevel.DEBUG);
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
            menuItem.Header = I18n.ReplaceUserGroup;
            menuItem.Click += (s, e) =>
            {
                ReplaceUserGroup((ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)s)));
            };
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
            // 删除分组
            menuItem = new();
            menuItem.Header = I18n.RemoveUserGroup;
            menuItem.Click += (s, e) =>
            {
                RemoveUserGroup((ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)s)));
            };
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);

            listBoxItem.ContextMenu = contextMenu;
            ListBoxItemHelper.SetIcon(listBoxItem, new Emoji.Wpf.TextBlock() { Text = icon });
            ListBox_UserGroup.Items.Add(listBoxItem);
            allUserGroups.Add(name, new());
            allListBoxItems.Add(name, listBoxItem);
            allModShowInfoGroups.Add(name, new());
            StartRemindSaveThread();
            ComboBox_ExportUserGroup.Items.Add(new ComboBoxItem() { Content = name, Tag = name, Style = (Style)Application.Current.Resources["ComboBoxItem_Style"] });
            STLog.Instance.WriteLine($"{I18n.AddUserGroup} {icon} {name}");
        }

        private void RemoveUserGroup(ListBoxItem listBoxItem)
        {
            if (ST.ShowMessageBox(I18n.ConfirmUserGroupDeletion, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
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

        private void ReplaceUserGroup(ListBoxItem listBoxItem)
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
                    ST.ShowMessageBox(string.Format(I18n.UserGroupCannotNamed, ModTypeGroup.Collected, strUserCustomData), MessageBoxImage.Warning);
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
                    ST.ShowMessageBox(I18n.UserGroupNamingFailed);
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
    }
}