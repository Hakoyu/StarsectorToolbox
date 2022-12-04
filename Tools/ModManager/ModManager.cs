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
using StarsectorTools.Lib;
using System.ComponentModel;
using StarsectorTools.Langs.MessageBox;
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
using Aspose.Zip.Rar;
using System.Windows.Media;
using System.Runtime.CompilerServices;

namespace StarsectorTools.Tools.ModManager
{
    public partial class ModManager
    {
        void InitializeData()
        {
            buttonStyle.Enabled = (Style)Resources["EnabledStyle"];
            buttonStyle.Disable = (Style)Resources["DisableStyle"];
            buttonStyle.Collected = (Style)Resources["CollectedStyle"];
            buttonStyle.Uncollected = (Style)Resources["UncollectedStyle"];
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
                {ModGroupType.Megamods,new() },
                {ModGroupType.FactionMods,new() },
                {ModGroupType.ContentExpansions,new() },
                {ModGroupType.UtilityMods,new() },
                {ModGroupType.MiscellaneousMods,new() },
                {ModGroupType.BeautifyMods,new() },
                {ModGroupType.Unknown,new() },
            };
            modsShowInfoFromGroup = new()
            {
                {ModGroupType.All,new() },
                {ModGroupType.Enabled,new() },
                {ModGroupType.Disable,new() },
                {ModGroupType.Libraries,new() },
                {ModGroupType.Megamods,new() },
                {ModGroupType.FactionMods,new() },
                {ModGroupType.ContentExpansions,new() },
                {ModGroupType.UtilityMods,new() },
                {ModGroupType.MiscellaneousMods,new() },
                {ModGroupType.BeautifyMods,new() },
                {ModGroupType.Unknown,new() },
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
            RefreshAllSizeOfListBoxItems();
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
                    STLog.Instance.WriteLine($"添加模组: {modInfo.Id}", STLogLevel.DEBUG);
                }
                catch
                {
                    STLog.Instance.WriteLine($"模组添加失败: {dir.Name}", STLogLevel.WARN);
                    err ??= "以下模组加载错误\n";
                    err += $"{dir.Name}\n";
                    size++;
                }
            }
            if (err != null)
                MessageBox.Show(err, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
            STLog.Instance.WriteLine($"模组添加成功: {allModsInfo.Count} 失败: {size}");
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
                STLog.Instance.WriteLine($"未找到启动列表 位置: {ST.enabledModsJsonPath}", STLogLevel.WARN);
                if (MessageBox.Show($"启用列表不存在\n位置:{ST.enabledModsJsonPath}\n是否新建?", MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    STLog.Instance.WriteLine($"新建启动列表 位置: {ST.enabledModsJsonPath}");
                    File.Create(ST.enabledModsJsonPath).Close();
                }
            }
            else
                GetEnabledMods(ST.enabledModsJsonPath);
        }
        void GetEnabledMods(string path, bool importMode = false)
        {
            if (importMode)
            {
                var result = MessageBox.Show("选择导入模式\nYes:替换 No:合并 Cancel:取消导入", "选择导入模式", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    ClearAllEnabledMods();
                else if (result == MessageBoxResult.Cancel)
                    return;
            }
            string datas = File.ReadAllText(path);
            if (datas.Length > 0)
            {
                try
                {
                    string err = null!;
                    JsonNode enabledModsJson = JsonNode.Parse(datas)!;
                    JsonArray enabledModsJsonArray = enabledModsJson["enabledMods"]!.AsArray();
                    STLog.Instance.WriteLine($"成功加载启用列表 位置: {path}");
                    foreach (var mod in enabledModsJsonArray)
                    {
                        var id = mod!.GetValue<string>();
                        if (allModsInfo.ContainsKey(id))
                        {
                            STLog.Instance.WriteLine($"启用模组: {id}", STLogLevel.DEBUG);
                            ChangeModEnabled(id, true);
                        }
                        else
                        {
                            STLog.Instance.WriteLine($"未找到模组: {id}");
                            err ??= "并未找到启用列表中的以下模组:\n";
                            err += $"{id}\n";
                        }
                    }
                    STLog.Instance.WriteLine($"启用完成 数量: {allEnabledModsId.Count}");
                    if (err != null)
                        MessageBox.Show(err, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch
                {
                    STLog.Instance.WriteLine($"启用列表载入错误 位置: {path}");
                    MessageBox.Show($"启用列表载入错误\n位置:{path}", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        void CheckUserGroup()
        {
            if (!File.Exists(userGroupFile))
            {
                SaveUserGroup(userGroupFile);
                STLog.Instance.WriteLine($"创建用户分组数据 位置: {userGroupFile}");
            }
            else
            {
                GetUserGroup(userGroupFile);
            }
        }
        void GetUserGroup(string path)
        {
            STLog.Instance.WriteLine($"载入用户分组数据 位置: {path}");
            string err = null!;
            List<string> errList = new();
            try
            {
                using TomlTable toml = TOML.Parse(path);
                if (!toml.Any(kv => kv.Key == ModGroupType.Collected))
                {
                    STLog.Instance.WriteLine($"错误的用户分组文件 位置: {path}");
                    MessageBox.Show($"错误的用户分组文件\n位置: {path}", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (var kv in toml)
                {
                    if (kv.Key == ModGroupType.Collected)
                    {
                        foreach (string id in kv.Value.AsTomlArray)
                        {
                            if (allModsShowInfo.ContainsKey(id))
                            {
                                ChangeModCollected(id, true);
                            }
                            else
                            {
                                STLog.Instance.WriteLine($"收藏列表中的模组不存在 {id}");
                                err ??= "收藏列表中的以下模组不存在\n";
                                err += $"{id}\n";
                            }
                        }
                        if (err is not null)
                            errList.Add(err);
                    }
                    else if (kv.Key == "UserModsData")
                    {
                        foreach (var dic in kv.Value.AsTomlArray)
                        {
                            var id = dic["Id"].AsString;
                            if (allModsShowInfo.ContainsKey(id))
                            {
                                var info = allModsShowInfo[id];
                                info.UserDescription = dic["UserDescription"];
                            }
                            else
                            {
                                STLog.Instance.WriteLine($"自定义数据中的模组不存在 {id}");
                                err ??= "自定义数据中的以下模组不存在\n";
                                err += $"{id}\n";
                            }
                        }
                        if (err is not null)
                            errList.Add(err);
                    }
                    else
                    {
                        string group = kv.Key;
                        if (!allUserGroups.ContainsKey(group))
                        {
                            AddUserGroup(kv.Value["Icon"], group);
                            foreach (string id in kv.Value["Mods"].AsTomlArray)
                            {
                                if (allModsShowInfo.ContainsKey(id))
                                {
                                    if (allUserGroups[group].Add(id))
                                        modsShowInfoFromGroup[group].Add(allModsShowInfo[id]);
                                    else
                                    {
                                        STLog.Instance.WriteLine($"用户分组中已存在 {id}");
                                        err ??= "";
                                        err += $"模组 {id} 已存在";
                                    }
                                }
                                else
                                {
                                    STLog.Instance.WriteLine($"{group} 用户分组中不存在 {id}");
                                    err ??= $" {group} 用户分组中的以下模组不存在\n";
                                    err += $"{id}\n";
                                }
                            }
                        }
                        else
                        {
                            STLog.Instance.WriteLine($"{group} 用户分组已存在");
                            err ??= $"{group} 用户分组已存在";
                        }
                        if (err is not null)
                            errList.Add(err);
                    }
                }
                if (errList.Count > 0)
                {
                    MessageBox.Show(string.Join("\n", errList), MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception)
            {
                STLog.Instance.WriteLine($"用户分组数据载入错误 位置: {path}");
                MessageBox.Show($"用户分组数据载入错误\n位置: {path}", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (Exception)
            {
                MessageBox.Show("获取默认分组失败", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                STLog.Instance.WriteLine($"获取默认分组失败 位置: {modGroupFile}");
            }
        }
        string CheckGroup(string id)
        {
            foreach (var group in modsIdFromGroups)
                if (group.Value.Contains(id))
                    return group.Key;
            return ModGroupType.Unknown;
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
                    modsShowInfoFromGroup[ModGroupType.Disable].Add(showInfo);
            }
            STLog.Instance.WriteLine($"模组显示信息设置成功 数量: {allModsInfo.Count}");
            ListBox_ModsGroupMenu.SelectedIndex = 0;
            CheckEnabledModsDependencies();
        }
        void ChangeShowGroup(string group)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DataGrid_ModsShowList.ItemsSource = modsShowInfoFromGroup[group]);
            STLog.Instance.WriteLine($"显示分组 {group}");
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
                CollectedStyle = isCollected is true ? buttonStyle.Collected : buttonStyle.Uncollected,
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
            STLog.Instance.WriteLine($"{info.Id} 归类至 {showInfo.Group}", STLogLevel.DEBUG);
            return showInfo;
        }
        void RefreshAllSizeOfListBoxItems()
        {
            foreach (var item in allListBoxItemsFromGroups.Values)
            {
                item.Content = $"{item.ToolTip} ({modsShowInfoFromGroup[item.Tag.ToString()!].Count})";
                STLog.Instance.WriteLine($"组数量显示 {item.Content}", STLogLevel.DEBUG);
            }
            STLog.Instance.WriteLine($"组数量显示刷新成功");
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
            STLog.Instance.WriteLine($"右键菜单创建成功 数量: {allModsShowInfo.Values.Count}");
        }
        ContextMenu CreateContextMenu(ModShowInfo info)
        {
            ContextMenu contextMenu = new();
            contextMenu.Style = (Style)Application.Current.Resources["ContextMenu_Style"];
            MenuItem menuItem = new();
            menuItem.Header = info.Enabled is true ? "禁用所选模组" : "启用所选模组";
            menuItem.Click += (o, e) => ChangeSelectedModsEnabled(info.Enabled is not true);
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {menuItem.Header}", STLogLevel.DEBUG);

            menuItem = new();
            menuItem.Header = info.Collected is true ? "取消收藏所选模组" : "收藏所选模组";
            menuItem.Click += (o, e) => ChangeSelectedModsCollected(info.Collected is not true);
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {menuItem.Header}", STLogLevel.DEBUG);

            menuItem = new();
            menuItem.Header = "打开模组文件夹";
            menuItem.Click += (o, e) =>
            {
                STLog.Instance.WriteLine($"打开模组文件夹 位置: {allModsInfo[info.Id].Path}");
                ST.OpenFile(allModsInfo[info.Id].Path);
            };
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {menuItem.Header}", STLogLevel.DEBUG);

            menuItem = new();
            menuItem.Header = "删除模组";
            menuItem.Click += (o, e) =>
            {
                string path = allModsInfo[info.Id].Path;
                if (MessageBox.Show($"确实删除模组?\nID: {info.Id}\n位置: {path}\n", MessageBoxCaption_I18n.Warn, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    RemoveModShowInfo(info.Id);
                    ST.DeleteDirectoryToRecycleBin(path);
                    RefreshAllSizeOfListBoxItems();
                    StartRemindSaveThread();
                }
            };
            contextMenu.Items.Add(menuItem);

            if (allUserGroups.Count > 0)
            {
                menuItem = new();
                menuItem.Header = "添加至用户分组";
                foreach (var group in allUserGroups.Keys)
                {
                    if (!allUserGroups[group].Contains(info.Id))
                    {
                        MenuItem groupItem = new();
                        groupItem.Header = group;
                        groupItem.Background = (Brush)Application.Current.Resources["ColorBG"];
                        // 此语句无法获取色彩透明度 原因未知
                        MenuItemHelper.SetHoverBackground(groupItem, (Brush)Application.Current.Resources["ColorBB"]);
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
                    STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {menuItem.Header}", STLogLevel.DEBUG);
                }
            }
            var haveModGroup = allUserGroups.Where(g => g.Value.Contains(info.Id));
            if (haveModGroup.Count() > 0)
            {
                menuItem = new();
                menuItem.Header = "从用户分组中删除";
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
                STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {menuItem.Header}", STLogLevel.DEBUG);
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
            RefreshAllSizeOfListBoxItems();
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
            STLog.Instance.WriteLine($"{id} 在用户分组 {group} 状态修改为 {status}", STLogLevel.DEBUG);
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
            RefreshAllSizeOfListBoxItems();
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModInfo();
            CheckEnabledModsDependencies();
            StartRemindSaveThread();
        }
        void ClearAllEnabledMods()
        {
            while (allEnabledModsId.Count > 0)
                ChangeModEnabled(allEnabledModsId.ElementAt(0), false);
            STLog.Instance.WriteLine($"取消所有已启用模组");
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
                    modsShowInfoFromGroup[ModGroupType.Disable].Remove(info);
                }
            }
            else
            {
                if (allEnabledModsId.Remove(info.Id))
                {
                    modsShowInfoFromGroup[ModGroupType.Enabled].Remove(info);
                    modsShowInfoFromGroup[ModGroupType.Disable].Add(info);
                    info.RowDetailsHight = 0;
                }
            }
            STLog.Instance.WriteLine($"{id} 启用状态修改为 {info.Collected}", STLogLevel.DEBUG);
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
                        STLog.Instance.WriteLine($"{info.Id} 未启用前置 {info.Dependencies}");
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
            RefreshAllSizeOfListBoxItems();
            StartRemindSaveThread();
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModInfo();
        }
        void ChangeModCollected(string id, bool? collected = null)
        {
            ModShowInfo info = allModsShowInfo[id];
            info.Collected = collected is null ? !info.Collected : collected;
            info.CollectedStyle = info.Collected is true ? buttonStyle.Collected : buttonStyle.Uncollected;
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
            STLog.Instance.WriteLine($"{id} 收藏状态修改为 {info.Collected}", STLogLevel.DEBUG);
        }
        void SaveAllData()
        {
            SaveEnabledMods(ST.enabledModsJsonPath);
            SaveUserGroup(userGroupFile);
        }
        void SaveEnabledMods(string path)
        {
            JsonObject keyValues = new()
            {
                { "enabledMods", new JsonArray() }
            };
            foreach (var mod in allEnabledModsId)
                ((JsonArray)keyValues["enabledMods"]!).Add(mod);
            File.WriteAllText(path, keyValues.ToJsonString(new() { WriteIndented = true }));
            STLog.Instance.WriteLine($"保存启用列表成功 位置: {path}");
        }
        void SaveUserGroup(string path)
        {
            TomlTable toml = new()
            {
                { ModGroupType.Collected, new TomlArray() },
                { "UserModsData", new TomlArray() }
            };
            foreach (var info in allModsShowInfo.Values)
            {
                if (info.Collected is true)
                    toml[ModGroupType.Collected].Add(info.Id);
                if (info.UserDescription!.Length > 0)
                {
                    toml["UserModsData"].Add(new TomlTable()
                    {
                        ["Id"] = info.Id,
                        ["UserDescription"] = info.UserDescription!.Length > 0 ? info.UserDescription : "",
                    });
                }
            }
            foreach (var kv in allUserGroups)
            {
                toml.Add(kv.Key, new TomlTable()
                {
                    ["Icon"] = ((Emoji.Wpf.TextBlock)ListBoxItemHelper.GetIcon(allListBoxItemsFromGroups[kv.Key])).Text,
                    ["Mods"] = new TomlArray(),
                });
                foreach (var id in kv.Value)
                    toml[kv.Key]["Mods"].Add(id);
            }
            toml.SaveTo(path);
            STLog.Instance.WriteLine($"保存用户分组成功 位置: {path}");
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
            GroupBox_ModInfo.Visibility = Visibility.Visible;
            showModInfo = true;
            nowSelectedMod = id;
            SetModInfo(id);
        }
        public void CloseModInfo()
        {
            GroupBox_ModInfo.Visibility = Visibility.Hidden;
            showModInfo = false;
            nowSelectedMod = null;
            TextBox_UserDescription.Text = "";
            STLog.Instance.WriteLine($"关闭详情 {nowSelectedMod}", STLogLevel.DEBUG);
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
                TextBlock_ModDependencies.Text = string.Join("\n", list.Select(i => $"{"名称:"} {i.Name} {"ID:"} {i.Id} " + (i.Version is not null ? $"{"版本"} {i.Version}" : ""))!);
            }
            else
                GroupBox_ModDependencies.Visibility = Visibility.Collapsed;
            TextBlock_ModDescription.Text = info.Description;
            TextBox_UserDescription.Text = allModsShowInfo[info.Id].UserDescription!;
            STLog.Instance.WriteLine($"显示详情 {id}", STLogLevel.DEBUG);
        }
        void DropFile(string filePath)
        {
            string tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}Temp";
            if (!ST.UnArchiveFileToDir(filePath, tempPath))
            {
                MessageBox.Show($"解压错误\n 位置:{filePath}");
                return;
            }
            DirectoryInfo dirs = new(tempPath);
            var filesInfo = dirs.GetFiles("mod_info.json", SearchOption.AllDirectories);
            if (filesInfo.Length > 0 && filesInfo.First() is FileInfo fileInfo && fileInfo.FullName is string jsonPath)
            {
                var modInfo = GetModInfo(jsonPath);
                if (allModsInfo.ContainsKey(modInfo.Id))
                {
                    var originalModInfo = allModsInfo[modInfo.Id];
                    if (MessageBox.Show($"{modInfo.Id} 已存在 是否覆盖?\n原始版本:{originalModInfo.Version}\n新增版本:{modInfo.Version}", "已存在相同模组", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                        allModsInfo.Remove(modInfo.Id);
                        allModsInfo.Add(modInfo.Id, modInfo);
                        Dispatcher.BeginInvoke(() =>
                        {
                            RemoveModShowInfo(modInfo.Id);
                            AddModShowInfo(GetModShowInfo(modInfo));
                            StartRemindSaveThread();
                        });
                        STLog.Instance.WriteLine($"覆盖模组 {modInfo.Id} {originalModInfo.Version} => {modInfo.Version}");
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        AddModShowInfo(GetModShowInfo(modInfo));
                        StartRemindSaveThread();
                    });
                }
            }
            else
            {
                STLog.Instance.WriteLine($"压缩文件未包含模组信息 位置: {filePath}");
                MessageBox.Show($"压缩文件未包含模组信息\n{filePath}");
            }
            dirs.Delete(true);
        }
        ModShowInfo RemoveModShowInfo(string id)
        {
            var modShowInfo = allModsShowInfo[id];
            allModsShowInfo.Remove(modShowInfo.Id);
            modsShowInfoFromGroup[ModGroupType.All].Remove(modShowInfo);
            modsShowInfoFromGroup[modShowInfo.Group!].Remove(modShowInfo);
            STLog.Instance.WriteLine($"删除模组 {modShowInfo.Id} {modShowInfo.Version}", STLogLevel.DEBUG);
            return modShowInfo;
        }
        void AddModShowInfo(ModShowInfo modShowInfo)
        {
            allModsShowInfo.Add(modShowInfo.Id, modShowInfo);
            modsShowInfoFromGroup[ModGroupType.All].Add(modShowInfo);
            modsShowInfoFromGroup[modShowInfo.Group!].Add(modShowInfo);
            STLog.Instance.WriteLine($"添加模组 {modShowInfo.Id} {modShowInfo.Version}", STLogLevel.DEBUG);
        }
        void ClearDataGridSelected()
        {
            while (DataGrid_ModsShowList.SelectedItems.Count > 0)
            {
                if (DataGrid_ModsShowList.ItemContainerGenerator.ContainerFromItem(DataGrid_ModsShowList.SelectedItems[0]) is DataGridRow row)
                    row.IsSelected = false;
            }
            STLog.Instance.WriteLine($"已清空选择的模组", STLogLevel.DEBUG);
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
            menuItem.Header = "重命名分组";
            menuItem.Click += (o, e) =>
            {
                AddUserGroup window = new();
                ((MainWindow)Application.Current.MainWindow).IsEnabled = false;
                window.TextBox_Icon.Text = icon;
                window.TextBox_Name.Text = name;
                window.Show();
                window.Button_OK.Click += (o, e) =>
                {
                    string _icon = window.TextBox_Icon.Text;
                    string _name = window.TextBox_Name.Text;
                    if (_name.Length > 0 && !allUserGroups.ContainsKey(_name))
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

                        SetListBoxItemData(listBoxItem, _name);
                        window.Close();
                        RefreshAllSizeOfListBoxItems();
                        RefreshModsContextMenu();
                        StartRemindSaveThread();
                    }
                    else
                        MessageBox.Show("命名失败,名字为空或者已存在相同名字的分组");
                };
                window.Button_Cancel.Click += (o, e) => window.Close();
                window.Closed += (o, e) => ((MainWindow)Application.Current.MainWindow).IsEnabled = true;
            };
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{name} 分组添加右键菜单 {menuItem.Header}", STLogLevel.DEBUG);

            menuItem = new();
            menuItem.Header = "删除分组";
            menuItem.Click += (o, e) =>
            {
                var _itme = (ListBoxItem)ContextMenuService.GetPlacementTarget(LogicalTreeHelper.GetParent((DependencyObject)o));
                var _name = _itme.Content.ToString()!.Split(" ")[0];
                ListBox_UserGroup.Items.Remove(_itme);
                allUserGroups.Remove(_name);
                allListBoxItemsFromGroups.Remove(_name);
                modsShowInfoFromGroup.Remove(_name);
                RefreshModsContextMenu();
                StartRemindSaveThread();
                ListBox_ModsGroupMenu.SelectedIndex = 0;
            };
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{name} 分组添加右键菜单 {menuItem.Header}", STLogLevel.DEBUG);
            listBoxItem.ContextMenu = contextMenu;
            //ListBoxItemHelper.SetIcon(menuItem, icon);
            ListBoxItemHelper.SetIcon(listBoxItem, new Emoji.Wpf.TextBlock() { Text = icon });
            ListBox_UserGroup.Items.Add(listBoxItem);
            allUserGroups.Add(name, new());
            allListBoxItemsFromGroups.Add(name, listBoxItem);
            modsShowInfoFromGroup.Add(name, new());
            STLog.Instance.WriteLine($"添加用户分组 {icon} {name}");
        }
        void SetListBoxItemData(ListBoxItem item, string name)
        {
            item.Content = new Emoji.Wpf.TextBlock() { Text = name };
            item.ToolTip = name;
            item.Tag = name;
        }
        void SearchMods(string text)
        {
            var type = ((ComboBoxItem)ComboBox_SearchType.SelectedItem).Tag.ToString()!;
            if (text.Length > 0)
            {
                ChangeShowGroup(GetSearchModsShowInfo(text, type));
                STLog.Instance.WriteLine($"模组搜索 {text}", STLogLevel.DEBUG);
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
                "Name" => modsShowInfoFromGroup[nowGroup].Where(i => i.Name!.Contains(text, StringComparison.OrdinalIgnoreCase)),
                "Id" => modsShowInfoFromGroup[nowGroup].Where(i => i.Id.Contains(text, StringComparison.OrdinalIgnoreCase)),
                "Author" => modsShowInfoFromGroup[nowGroup].Where(i => i.Author!.Contains(text, StringComparison.OrdinalIgnoreCase)),
                "UserDescription" => modsShowInfoFromGroup[nowGroup].Where(i => i.UserDescription!.Contains(text, StringComparison.OrdinalIgnoreCase)),
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
