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
using StarsectorTools.Windows;
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
            allEnabledModsId = new();
            allCollectedModsId = new();
            allModsInfo = new();
            allListBoxItems = new();
            allModsShowInfo = new();
            allUserGroups = new();
            allModsTypeGroup = new();
            allModShowInfoGroups = new()
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
            GetAllModsInfo();
            GetAllListBoxItems();
            GetAllGroup();
            InitializeDataGridItemsSource();
            CheckEnabledMods();
            CheckUserData();
            RefreshModsContextMenu();
            RefreshCountOfListBoxItems();
            ResetRemindSaveThread();
            GC.Collect();
        }

        private void GetAllModsInfo()
        {
            int size = 0;
            DirectoryInfo dirs = new(ST.gameModsPath);
            string err = null!;
            foreach (var dir in dirs.GetDirectories())
            {
                try
                {
                    ModInfo info = GetModInfo($"{dir.FullName}\\mod_info.json");
                    allModsInfo.Add(info.Id, info);
                    STLog.Instance.WriteLine($"{I18n.ModAddSuccess}: {info.Id}", STLogLevel.DEBUG);
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
                MessageBox.Show(err, "", MessageBoxButton.OK, MessageBoxImage.Warning);
            STLog.Instance.WriteLine(I18n.ModAddSize, STLogLevel.INFO, allModsInfo.Count, size);
        }

        private static ModInfo GetModInfo(string jsonPath)
        {
            string datas = File.ReadAllText(jsonPath);
            // 清除json中的注释
            datas = Regex.Replace(datas, @"(#|//)[\S ]*", "");
            // 清除json中不符合规定的逗号
            datas = Regex.Replace(datas, @",(?=[\r\n \t]*[\]\}])|(?<=[\}\]]),[ \t]*\r?\Z", "");
            JsonNode jsonData = JsonNode.Parse(datas)!;
            ModInfo info = new();
            foreach (var data in jsonData.AsObject())
                info.SetData(data);
            info.Path = Path.GetDirectoryName(jsonPath)!;
            return info;
        }

        private void CheckEnabledMods()
        {
            if (File.Exists(ST.enabledModsJsonPath))
                GetEnabledMods(ST.enabledModsJsonPath);
            else
                SaveEnabledMods(ST.enabledModsJsonPath);
            STLog.Instance.WriteLine($"{I18n.CreateFile} {I18n.Path}: {ST.enabledModsJsonPath}");
        }

        private void ImportMode()
        {
            var result = MessageBox.Show(I18n.SelectImportMode, "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
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
                JsonNode enabledModsJson = JsonNode.Parse(datas)!;
                if (enabledModsJson.AsObject().Count != 1 || enabledModsJson.AsObject().ElementAt(0).Key != strEnabledMods)
                    throw new();
                if (importMode)
                    ImportMode();
                JsonArray enabledModsJsonArray = enabledModsJson[strEnabledMods]!.AsArray();
                STLog.Instance.WriteLine($"{I18n.LoadEnabledModsFile} {I18n.Path}: {filePath}");
                foreach (var modId in enabledModsJsonArray)
                {
                    var id = modId!.GetValue<string>();
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
                    MessageBox.Show(err, "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.LoadError} {I18n.Path}: {filePath}", STLogLevel.ERROR);
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                MessageBox.Show($"{I18n.LoadError}\n{I18n.Path}: {filePath}", "", MessageBoxButton.OK, MessageBoxImage.Error);
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
                SaveAllUserGroup(userGroupFile);
        }

        private void GetUserGroup(string filePath)
        {
            try
            {
                string err = null!;
                using TomlTable toml = TOML.Parse(filePath);
                foreach (var kv in toml)
                {
                    if (kv.Key == ModGroupType.Collected || kv.Key == strUserCustomData)
                        continue;
                    string group = kv.Key;
                    if (!allUserGroups.ContainsKey(group))
                    {
                        AddUserGroup(kv.Value[strIcon]!, group);
                        foreach (string id in kv.Value[strMods].AsTomlArray)
                        {
                            if (allModsShowInfo.ContainsKey(id))
                            {
                                if (allUserGroups[group].Add(id))
                                    allModShowInfoGroups[group].Add(allModsShowInfo[id]);
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
                    MessageBox.Show(err, "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.FileError} {filePath}", STLogLevel.ERROR);
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                MessageBox.Show($"{I18n.FileError} {filePath}", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GetUserData(string filePath)
        {
            STLog.Instance.WriteLine($"{I18n.LoadUserData} {I18n.Path}: {filePath}");
            string err = null!;
            try
            {
                using TomlTable toml = TOML.Parse(filePath);
                foreach (string id in toml[ModGroupType.Collected].AsTomlArray)
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
                foreach (var dict in toml[strUserCustomData].AsTomlArray)
                {
                    var id = dict[strId].AsString;
                    if (allModsShowInfo.ContainsKey(id))
                    {
                        var info = allModsShowInfo[id];
                        info.UserDescription = dict[strUserDescription];
                    }
                    else
                    {
                        STLog.Instance.WriteLine($"{I18n.NotFoundMod} {id}");
                        err ??= $"{I18n.NotFoundMod}\n";
                        err += $"{id}\n";
                    }
                }
                if (!string.IsNullOrEmpty(err))
                    MessageBox.Show(err, "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.UserDataLoadError} {I18n.Path}: {filePath}", STLogLevel.ERROR);
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                MessageBox.Show($"{I18n.UserDataLoadError}\n{I18n.Path}: {filePath}", "", MessageBoxButton.OK, MessageBoxImage.Error);
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
        }

        private void GetAllGroup()
        {
            if (!File.Exists(modGroupFile))
                CreateModGroupFile();
            try
            {
                using TomlTable toml = TOML.Parse(modGroupFile);
                foreach (var kv in toml)
                    foreach (string id in kv.Value.AsTomlArray)
                        allModsTypeGroup.Add(id, kv.Key);
            }
            catch (Exception ex)
            {
                STLog.Instance.WriteLine($"{I18n.ModGroupFailedToGet} {I18n.Path}: {modGroupFile}", STLogLevel.ERROR);
                STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
                MessageBox.Show($"{I18n.ModGroupFailedToGet}\n{I18n.Path}: {modGroupFile}", "", MessageBoxButton.OK, MessageBoxImage.Error);
                CreateModGroupFile();
            }
            void CreateModGroupFile()
            {
                using StreamReader sr = new(Application.GetResourceStream(modGroupUri).Stream);
                File.WriteAllText(modGroupFile, sr.ReadToEnd());
            }
        }

        private string CheckGroup(string id)
        {
            if (allModsTypeGroup.ContainsKey(id))
                return allModsTypeGroup[id];
            else
                return ModGroupType.UnknownMods;
        }

        private void InitializeDataGridItemsSource()
        {
            foreach (var info in allModsInfo.Values)
                AddModShowInfo(info);
            STLog.Instance.WriteLine($"{I18n.ModShowInfoSetSuccess} {I18n.Size}: {allModsInfo.Count}");
            ListBox_ModsGroupMenu.SelectedIndex = 0;
            CheckEnabledModsDependencies();
        }

        private void ChangeShowGroup(string group)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DataGrid_ModsShowList.ItemsSource = allModShowInfoGroups[group]);
            STLog.Instance.WriteLine($"{I18n.ShowGroup} {group}");
            //viewModel?.ChangeCollectionView(allModShowInfoGroups[userGroup]);
        }

        private void ChangeShowGroup(ObservableCollection<ModShowInfo> modsShowInfo)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DataGrid_ModsShowList.ItemsSource = modsShowInfo);
        }

        //void RefreshShowGroup()
        //{
        //    viewModel?.CollectionView?.Refresh();
        //}
        private ModShowInfo CreateModShowInfo(ModInfo info)
        {
            return new ModShowInfo()
            {
                IsCollected = allCollectedModsId.Contains(info.Id),
                IsEnabled = allEnabledModsId.Contains(info.Id),
                IsUtility = info.IsUtility,
                Name = info.Name,
                Id = info.Id,
                Author = info.Author,
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
                    STLog.Instance.WriteLine($"{I18n.IconLoadError} {I18n.Path}: {filePath}", STLogLevel.ERROR);
                    STLog.Instance.WriteLine(ex.Message, STLogLevel.ERROR);
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
                STLog.Instance.WriteLine($"{I18n.GroupModCountRefresh} {item.Content}", STLogLevel.DEBUG);
            }
            STLog.Instance.WriteLine(I18n.GroupModCountRefreshComplete);
        }

        private void RefreshModsContextMenu()
        {
            foreach (var info in allModsShowInfo.Values)
                info.ContextMenu = CreateContextMenu(info);
            STLog.Instance.WriteLine($"{I18n.ContextMenuRefreshComplete} {I18n.Size}: {allModsShowInfo.Values.Count}");
        }

        private ContextMenu CreateContextMenu(ModShowInfo info)
        {
            STLog.Instance.WriteLine($"{info.Id} {I18n.AddContextMenu}", STLogLevel.DEBUG);
            ContextMenu contextMenu = new();
            // 标记菜单项是否被创建
            contextMenu.Tag = false;
            // 被点击时才加载菜单,可以降低内存占用
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            contextMenu.Loaded += (s, e) =>
            {
                if (contextMenu.Tag is true)
                    return;
                // 启用或禁用
                MenuItem menuItem = new();
                menuItem.Header = info.IsEnabled ? I18n.DisableSelectedMods : I18n.EnabledSelectedMods;
                menuItem.Click += (s, e) => ChangeSelectedModsEnabled();
                contextMenu.Items.Add(menuItem);
                STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 收藏或取消收藏
                menuItem = new();
                menuItem.Header = info.IsCollected ? I18n.CancelCollectionSelectedMods : I18n.CollectionSelectedMods;
                menuItem.Click += (s, e) => ChangeSelectedModsCollected();
                contextMenu.Items.Add(menuItem);
                STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 打开模组文件夹
                menuItem = new();
                menuItem.Header = I18n.OpenModDirectory;
                menuItem.Click += (s, e) =>
                {
                    STLog.Instance.WriteLine($"{I18n.OpenModDirectory} {I18n.Path}: {allModsInfo[info.Id].Path}");
                    ST.OpenFile(allModsInfo[info.Id].Path);
                };
                contextMenu.Items.Add(menuItem);
                STLog.Instance.WriteLine($"{I18n.AddMenuItem} {menuItem.Header}", STLogLevel.DEBUG);
                // 删除模组至回收站
                menuItem = new();
                menuItem.Header = I18n.DeleteMod;
                menuItem.Click += (s, e) =>
                {
                    string path = allModsInfo[info.Id].Path;
                    if (MessageBox.Show($"{I18n.ConfirmDeleteMod}?\nID: {info.Id}\n{I18n.Path}: {path}\n", "", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        ST.DeleteDirToRecycleBin(path);
                        RemoveModShowInfo(info.Id);
                        CloseModDetails();
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
                        if (!allUserGroups[group].Contains(info.Id))
                        {
                            MenuItem groupItem = new();
                            groupItem.Header = group;
                            //groupItem.Style = (Style)Application.Current.Resources["MenuItem_Style"];
                            // 有绑定问题,暂无解决方案
                            groupItem.Background = (Brush)Application.Current.Resources["ColorBG"];
                            MenuItemHelper.SetHoverBackground(groupItem, (Brush)Application.Current.Resources["ColorSelected"]);
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
                var groupWithMod = allUserGroups.Where(g => g.Value.Contains(info.Id));
                if (groupWithMod.Count() > 0)
                {
                    menuItem = new();
                    menuItem.Header = I18n.RemoveFromUserGroup;
                    foreach (var group in groupWithMod)
                    {
                        MenuItem groupItem = new();
                        groupItem.Header = group.Key;
                        //groupItem.Style = (Style)Application.Current.Resources["MenuItem_Style"];
                        // 有绑定问题,暂无解决方案
                        groupItem.Background = (Brush)Application.Current.Resources["ColorBG"];
                        // 此语句无法获取色彩透明度 原因未知
                        MenuItemHelper.SetHoverBackground(groupItem, (Brush)Application.Current.Resources["ColorSelected"]);
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
                    allModShowInfoGroups[group].Add(allModsShowInfo[id]);
            }
            else
            {
                if (allUserGroups[group].Remove(id))
                    allModShowInfoGroups[group].Remove(allModsShowInfo[id]);
            }
            showInfo.ContextMenu = CreateContextMenu(showInfo);
            STLog.Instance.WriteLine(I18n.ChangeModUserGroup, STLogLevel.DEBUG, id, group, isInGroup);
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
                    allModShowInfoGroups[ModGroupType.Enabled].Add(showInfo);
                    allModShowInfoGroups[ModGroupType.Disabled].Remove(showInfo);
                }
            }
            else
            {
                if (allEnabledModsId.Remove(showInfo.Id))
                {
                    allModShowInfoGroups[ModGroupType.Enabled].Remove(showInfo);
                    allModShowInfoGroups[ModGroupType.Disabled].Add(showInfo);
                    showInfo.MissDependencies = false;
                }
            }
            STLog.Instance.WriteLine($"{id} {I18n.ModEnabledStatus} {showInfo.IsEnabled}", STLogLevel.DEBUG);
        }

        private void CheckEnabledModsDependencies()
        {
            foreach (var showInfo in allModShowInfoGroups[ModGroupType.Enabled])
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
                    allModShowInfoGroups[ModGroupType.Collected].Add(showInfo);
            }
            else
            {
                if (allCollectedModsId.Remove(showInfo.Id))
                    allModShowInfoGroups[ModGroupType.Collected].Remove(showInfo);
            }
            STLog.Instance.WriteLine($"{id} {I18n.ModCollectedStatus} {showInfo.IsCollected}", STLogLevel.DEBUG);
        }

        private void SaveAllData()
        {
            SaveEnabledMods(ST.enabledModsJsonPath);
            SaveUserData(userDataFile);
            SaveAllUserGroup(userGroupFile);
        }

        private void SaveEnabledMods(string filePath)
        {
            JsonObject keyValues = new()
            {
                { strEnabledMods, new JsonArray() }
            };
            foreach (var mod in allEnabledModsId)
                ((JsonArray)keyValues[strEnabledMods]!).Add(mod);
            File.WriteAllText(filePath, keyValues.ToJsonString(new() { WriteIndented = true }));
            STLog.Instance.WriteLine($"{I18n.SaveEnabledListSuccess} {I18n.Path}: {filePath}");
        }

        private void SaveUserData(string filePath)
        {
            using TomlTable toml = new()
            {
                { ModGroupType.Collected, new TomlArray() },
                { strUserCustomData, new TomlArray() }
            };
            foreach (var info in allModsShowInfo.Values)
            {
                if (info.IsCollected is true)
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
            toml.SaveTo(filePath);
            STLog.Instance.WriteLine($"{I18n.SaveUserDataSuccess} {I18n.Path}: {filePath}");
        }

        private void SaveAllUserGroup(string filePath, string tag = strAll)
        {
            TomlTable toml = new();
            if (tag == strAll)
            {
                foreach (var kv in allUserGroups)
                    SaveUserGroup(kv.Key);
            }
            else
            {
                SaveUserGroup(tag);
            }
            toml.SaveTo(filePath);
            STLog.Instance.WriteLine($"{I18n.SaveUserGroupSuccess} {I18n.Path}: {filePath}");
            void SaveUserGroup(string name)
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

        private void ChangeModInfoShow(string id)
        {
            if (isShowModInfo)
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

        public void ShowModDetails(string id)
        {
            if (nowSelectedModId == id)
                return;
            Grid_ModInfo.Visibility = Visibility.Visible;
            isShowModInfo = true;
            nowSelectedModId = id;
            SetModDetails(id);
        }

        public void CloseModDetails()
        {
            Grid_ModInfo.Visibility = Visibility.Hidden;
            isShowModInfo = false;
            nowSelectedModId = null;
            TextBox_UserDescription.Text = "";
            STLog.Instance.WriteLine($"{I18n.CloseDetails} {nowSelectedModId}", STLogLevel.DEBUG);
        }

        private void SetModDetails(string id)
        {
            ModInfo info = allModsInfo[id];
            if (allModsShowInfo[info.Id].ImageSource != null)
                Image_ModImage.Source = allModsShowInfo[info.Id].ImageSource;
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

        private void DropFile(string filePath)
        {
            string tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}Temp";
            if (!ST.UnArchiveFileToDir(filePath, tempPath))
            {
                MessageBox.Show($"{I18n.UnzipError}\n {I18n.Path}:{filePath}");
                return;
            }
            DirectoryInfo dirs = new(tempPath);
            var filesInfo = dirs.GetFiles(modInfoJsonFile, SearchOption.AllDirectories);
            if (filesInfo.Length > 0 && filesInfo.First() is FileInfo fileInfo && fileInfo.FullName is string jsonPath)
            {
                var newModInfo = GetModInfo(jsonPath);
                if (allModsInfo.ContainsKey(newModInfo.Id))
                {
                    var originalModInfo = allModsInfo[newModInfo.Id];
                    if (MessageBox.Show($"{newModInfo.Id}\n{string.Format(I18n.SameModAlreadyExists, originalModInfo.Version, newModInfo.Version)}", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        ST.CopyDirectory(originalModInfo.Path, $"{backupModsDirectory}\\Temp");
                        new Task(() =>
                        {
                            string name = Path.GetFileName(fileInfo.DirectoryName)!;
                            string tempDir = $"{backupModsDirectory}\\Temp";
                            ST.ArchiveDirToDir(tempDir, backupModsDirectory, name);
                            Directory.Delete(tempDir, true);
                        }).Start();
                        Directory.Delete(originalModInfo.Path, true);
                        ST.CopyDirectory(Path.GetDirectoryName(jsonPath)!, ST.gameModsPath);
                        allModsInfo.Remove(newModInfo.Id);
                        allModsInfo.Add(newModInfo.Id, newModInfo);
                        Dispatcher.BeginInvoke(() =>
                        {
                            RemoveModShowInfo(newModInfo.Id);
                            AddModShowInfo(newModInfo);
                            RefreshCountOfListBoxItems();
                            StartRemindSaveThread();
                        });
                        STLog.Instance.WriteLine($"{I18n.ReplaceMod} {newModInfo.Id} {originalModInfo.Version} => {newModInfo.Version}");
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        ST.CopyDirectory(Path.GetDirectoryName(jsonPath)!, ST.gameModsPath);
                        AddModShowInfo(newModInfo);
                        RefreshCountOfListBoxItems();
                        StartRemindSaveThread();
                    });
                }
            }
            else
            {
                STLog.Instance.WriteLine($"{I18n.ZipFileError} {I18n.Path}: {filePath}");
                MessageBox.Show($"{I18n.ZipFileError}\n{I18n.Path}: {filePath}");
            }
            Dispatcher.BeginInvoke(() => dirs.Delete(true));
        }

        private void RemoveModShowInfo(string id)
        {
            var modShowInfo = allModsShowInfo[id];
            // 从总分组中删除
            allModsInfo.Remove(id);
            allModsShowInfo.Remove(id);
            allModShowInfoGroups[ModGroupType.All].Remove(modShowInfo);
            // 从类型分组中删除
            allModShowInfoGroups[allModsTypeGroup[id]].Remove(modShowInfo);
            // 从已启用或已禁用分组中删除
            if (modShowInfo.IsEnabled)
            {
                allEnabledModsId.Remove(id);
                allModShowInfoGroups[ModGroupType.Enabled].Remove(modShowInfo);
            }
            else
                allModShowInfoGroups[ModGroupType.Disabled].Remove(modShowInfo);
            // 从已收藏中删除
            if (modShowInfo.IsCollected)
            {
                allCollectedModsId.Remove(id);
                allModShowInfoGroups[ModGroupType.Collected].Remove(modShowInfo);
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
            STLog.Instance.WriteLine($"{I18n.RemoveMod} {modShowInfo.Id} {modShowInfo.Version}", STLogLevel.DEBUG);
        }

        private void AddModShowInfo(ModInfo modInfo)
        {
            if (allModsShowInfo.ContainsKey(modInfo.Id))
                return;
            ModShowInfo showInfo = CreateModShowInfo(modInfo);
            // 添加至总分组
            allModsShowInfo.Add(showInfo.Id, showInfo);
            allModShowInfoGroups[ModGroupType.All].Add(showInfo);
            // 添加至类型分组
            allModShowInfoGroups[allModsTypeGroup[showInfo.Id]].Add(showInfo);
            // 添加至已启用或已禁用分组
            if (showInfo.IsEnabled)
                allModShowInfoGroups[ModGroupType.Enabled].Add(showInfo);
            else
                allModShowInfoGroups[ModGroupType.Disabled].Add(showInfo);
            // 添加至已收藏分组
            if (showInfo.IsCollected)
                allModShowInfoGroups[ModGroupType.Collected].Add(showInfo);
            // 添加至用户分组
            foreach (var userGroup in allUserGroups)
            {
                if (userGroup.Value.Contains(modInfo.Id))
                {
                    userGroup.Value.Add(modInfo.Id);
                    allModShowInfoGroups[userGroup.Key].Add(showInfo);
                }
            }
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
            menuItem.Header = I18n.ReplaceUserGroupName;
            menuItem.Click += (s, e) =>
            {
                ReplaceUserGroupName((ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)s)));
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
            if (MessageBox.Show(I18n.ConfirmDeletionUserGroup, "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
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
            GC.Collect();
        }

        private void ReplaceUserGroupName(ListBoxItem listBoxItem)
        {
            string icon = ((Emoji.Wpf.TextBlock)ListBoxItemHelper.GetIcon(listBoxItem)).Text;
            string name = listBoxItem.ToolTip.ToString()!;
            AddUserGroup window = new();
            ((MainWindow)Application.Current.MainWindow).IsEnabled = false;
            window.TextBox_Icon.Text = icon;
            window.TextBox_Name.Text = name;
            window.Show();
            window.Button_Yes.Click += (s, e) =>
            {
                string _icon = window.TextBox_Icon.Text;
                string _name = window.TextBox_Name.Text;
                if (_name == ModGroupType.Collected || _name == strUserCustomData)
                {
                    MessageBox.Show(string.Format(I18n.UserGroupCannotNamed, ModGroupType.Collected, strUserCustomData), "", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show(I18n.AddUserNamingFailed);
            };
            window.Button_Cancel.Click += (s, e) => window.Close();
            window.Closed += (s, e) => ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
            GC.Collect();
        }

        private void SetListBoxItemData(ListBoxItem item, string name)
        {
            item.Content = name;
            item.ToolTip = name;
            item.Tag = name;
        }

        private void SearchMods(string text)
        {
            var type = ((ComboBoxItem)ComboBox_SearchType.SelectedItem).Tag.ToString()!;
            if (text.Length > 0)
            {
                ChangeShowGroup(GetSearchModsShowInfo(text, type));
                STLog.Instance.WriteLine($"{I18n.SearchMod} {text}", STLogLevel.DEBUG);
            }
            else
            {
                ChangeShowGroup(nowGroupName);
                GC.Collect();
            }
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