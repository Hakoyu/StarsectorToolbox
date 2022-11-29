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

namespace StarsectorTools.Pages
{
    public partial class ModManager
    {
        void InitializeData()
        {
            buttonStyle.Enabled = (Style)Resources["EnabledStyle"];
            buttonStyle.Disable = (Style)Resources["DisableStyle"];
            buttonStyle.Collected = (Style)Resources["CollectedStyle"];
            buttonStyle.Uncollected = (Style)Resources["UncollectedStyle"];
            labelStyle.VersionNormal = (Style)Resources["VersionNormalStyle"];
            labelStyle.VersionWarn = (Style)Resources["VersionWarnStyle"];
            labelStyle.IsUtility = (Style)Resources["IsUtilityStyle"];
            labelStyle.NotUtility = (Style)Resources["NotUtilityStyle"];
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
        void GetEnabledMods(string path)
        {
            enabledModsId.Clear();
            string datas = File.ReadAllText(path);
            if (datas.Length > 0)
            {
                try
                {
                    string err = null!;
                    JsonNode enabledModsJson = JsonNode.Parse(datas)!;
                    JsonArray enabledModsJsonArray = enabledModsJson["enabledMods"]!.AsArray();
                    STLog.Instance.WriteLine($"成功加载启动列表 位置: {path}");
                    foreach (var mod in enabledModsJsonArray)
                    {
                        var id = mod!.GetValue<string>();
                        if (allModsInfo.ContainsKey(id))
                        {
                            STLog.Instance.WriteLine($"启用模组: {id}");
                            if (!enabledModsId.Add(id))
                            {
                                err ??= "";
                                err += $"{id} 已存在\n";
                            }
                        }
                        else
                        {
                            STLog.Instance.WriteLine($"未找到模组: {id}");
                            err ??= "并未找到启用模组列表中的以下模组:\n";
                            err += $"{id}\n";
                        }
                    }
                    if (err != null)
                        MessageBox.Show(err, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch
                {
                    STLog.Instance.WriteLine($"启用列表载入错误 位置: {path}");
                    MessageBox.Show($"启用列表载入错误 位置:{path}", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        void CheckUserGroup()
        {
            if (!File.Exists(userGroupFile))
            {
                File.Create(userGroupFile).Close();
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
                foreach (var kv in toml)
                {
                    if (kv.Key == ModGroupType.Collected)
                    {
                        foreach (string id in kv.Value.AsTomlArray)
                        {
                            if (modsShowInfo.ContainsKey(id))
                            {
                                ModCollectedChange(id, true);
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
                            if (modsShowInfo.ContainsKey(id))
                            {
                                var info = modsShowInfo[id];
                                info.ImagePath = dic["ImagePath"];
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
                        if (!userGroups.ContainsKey(group))
                        {
                            AddUserGroup(kv.Value["Icon"], group);
                            foreach (string id in kv.Value["Mods"].AsTomlArray)
                            {
                                if (modsShowInfo.ContainsKey(id))
                                {
                                    if (userGroups[group].Add(id))
                                        modsShowInfoFromGroup[group].Add(modsShowInfo[id]);
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
                    listBoxItemsFromGroups.Add(item.Tag.ToString()!, item);
                else if (item.Content is Expander expander && expander.Content is ListBox listBox)
                    foreach (ListBoxItem item1 in listBox.Items)
                        listBoxItemsFromGroups.Add(item1.Tag.ToString()!, item1);
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
        void ShowGroupChange(string group)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () => DataGrid_ModsShowList.ItemsSource = modsShowInfoFromGroup[group]);
            STLog.Instance.WriteLine($"显示分组 {group}");
        }
        void ShowGroupChange(ObservableCollection<ModShowInfo> modsShowInfo)
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
                GameVersionStyle = info.GameVersion == ST.gameVersion ? labelStyle.VersionNormal : labelStyle.VersionWarn,
                RowDetailsHight = 0,
                Dependencies = "",
                DependenciesList = info.Dependencies is not null ? info.Dependencies.Select(i => i.Id).ToList() : null!,
                ImagePath = File.Exists($"{info.Path}\\icon.ico") ? $"{info.Path}\\icon.ico" : "",
                UserDescription = "",
                Utility = info.Utility,
                UtilityStyle = info.Utility is true ? labelStyle.IsUtility : labelStyle.NotUtility,
                Group = CheckGroup(info.Id),
            };
            STLog.Instance.WriteLine($"{info.Id} 归类至 {showInfo.Group}", STLogLevel.DEBUG);
            return showInfo;
        }
        void RefreAllSizeOfListBoxItems()
        {
            foreach (var item in listBoxItemsFromGroups.Values)
            {
                item.Content = $"{item.ToolTip} ({modsShowInfoFromGroup[item.Tag.ToString()!].Count})";
                STLog.Instance.WriteLine($"组数量显示 {item.Content}", STLogLevel.DEBUG);
            }
            STLog.Instance.WriteLine($"组数量显示刷新成功");
        }
        bool CheckEnabled(string id)
        {
            return enabledModsId.Contains(id);
        }
        bool CheckCollected(string id)
        {
            return collectedModsId.Contains(id);
        }
        void RefreshModsShowInfoContextMenu()
        {
            foreach (var info in modsShowInfo.Values)
                info.ContextMenu = CreateContextMenu(info);
            STLog.Instance.WriteLine($"右键菜单创建成功 数量: {modsShowInfo.Values.Count}");
        }
        ContextMenu CreateContextMenu(ModShowInfo info)
        {
            ContextMenu menu = new();
            MenuItem item = new();
            item.Header = info.Enabled is true ? "禁用所选模组" : "启用所选模组";
            item.Click += (o, e) => SelectedModsEnabledChange(info.Enabled is not true);
            menu.Items.Add(item);
            STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {item.Header}", STLogLevel.DEBUG);
            item = new();
            item.Header = info.Collected is true ? "取消收藏所选模组" : "收藏所选模组";
            item.Click += (o, e) => SelectedModsCollectedChange(info.Collected is not true);
            menu.Items.Add(item);
            STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {item.Header}", STLogLevel.DEBUG);
            item = new();
            item.Header = "打开模组文件夹";
            item.Click += (o, e) =>
            {
                STLog.Instance.WriteLine($"打开模组文件夹 位置: {allModsInfo[info.Id].Path}");
                System.Diagnostics.Process.Start("Explorer.exe", allModsInfo[info.Id].Path);
            };
            menu.Items.Add(item);
            STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {item.Header}", STLogLevel.DEBUG);
            if (userGroups.Count > 0)
            {
                item = new();
                item.Header = "添加至用户分组";
                foreach (var group in userGroups.Keys)
                {
                    if (!userGroups[group].Contains(info.Id))
                    {
                        MenuItem groupItem = new();
                        groupItem.Header = group;
                        groupItem.Click += (o, e) =>
                        {
                            SelectedModsUserGroupChange(group, true);
                        };
                        item.Items.Add(groupItem);
                    }
                }
                if (item.Items.Count > 0)
                {
                    menu.Items.Add(item);
                    STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {item.Header}", STLogLevel.DEBUG);
                }
            }
            var haveModGroup = userGroups.Where(g => g.Value.Contains(info.Id));
            if (haveModGroup.Count() > 0)
            {
                item = new();
                item.Header = "从用户分组中删除";
                foreach (var group in haveModGroup)
                {
                    MenuItem groupItem = new();
                    groupItem.Header = group.Key;
                    groupItem.Click += (o, e) =>
                    {
                        SelectedModsUserGroupChange(group.Key, false);
                    };
                    item.Items.Add(groupItem);
                }
                menu.Items.Add(item);
                STLog.Instance.WriteLine($"{info.Id} 添加右键菜单 {item.Header}", STLogLevel.DEBUG);
            }
            return menu;
        }
        void SelectedModsUserGroupChange(string group, bool status)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo info = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ModUserGroupChange(group, info.Id, status);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            RefreAllSizeOfListBoxItems();
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModInfo();
        }
        void ModUserGroupChange(string group, string id, bool status)
        {
            ModShowInfo info = modsShowInfo[id];
            if (status)
            {
                if (userGroups[group].Add(id))
                    modsShowInfoFromGroup[group].Add(modsShowInfo[id]);
            }
            else
            {
                userGroups[group].Remove(id);
                modsShowInfoFromGroup[group].Remove(modsShowInfo[id]);
            }
            info.ContextMenu = CreateContextMenu(info);
            STLog.Instance.WriteLine($"{id} 在用户分组 {group} 状态修改为 {status}", STLogLevel.DEBUG);
        }
        void SelectedModsEnabledChange(bool? enabled = null)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo info = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ModEnabledChange(info.Id, enabled);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            RefreAllSizeOfListBoxItems();
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModInfo();
            CheckEnabledModsDependencies();
        }
        void ClearEnabledMod()
        {
            foreach (var info in modsShowInfoFromGroup[ModGroupType.Enabled])
                modsShowInfoFromGroup[ModGroupType.Disable].Add(info);
            modsShowInfoFromGroup[ModGroupType.Enabled].Clear();
            enabledModsId.Clear();
            STLog.Instance.WriteLine($"取消所有已启用模组");
        }
        void ModEnabledChange(string id, bool? enabled = null)
        {
            ModShowInfo info = modsShowInfo[id];
            info.Enabled = enabled is null ? !info.Enabled : enabled;
            info.EnabledStyle = info.Enabled is true ? buttonStyle.Enabled : buttonStyle.Disable;
            info.ContextMenu = CreateContextMenu(info);
            if (info.Enabled is true)
            {
                if (enabledModsId.Add(info.Id))
                {
                    modsShowInfoFromGroup[ModGroupType.Enabled].Add(info);
                    modsShowInfoFromGroup[ModGroupType.Disable].Remove(info);
                }
            }
            else
            {
                if (enabledModsId.Remove(info.Id))
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
                    info.Dependencies = string.Join(" , ", info.DependenciesList.Where(s => !enabledModsId.Contains(s)));
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
        void SelectedModsCollectedChange(bool? collected = null)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo info = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ModCollectedChange(info.Id, collected);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            RefreAllSizeOfListBoxItems();
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModInfo();
        }
        void ModCollectedChange(string id, bool? collected = null)
        {
            ModShowInfo info = modsShowInfo[id];
            info.Collected = collected is null ? !info.Collected : collected;
            info.CollectedStyle = info.Collected is true ? buttonStyle.Collected : buttonStyle.Uncollected;
            info.ContextMenu = CreateContextMenu(info);
            if (info.Collected is true)
            {
                if (!CheckCollected(info.Id))
                {
                    collectedModsId.Add(info.Id);
                    modsShowInfoFromGroup[ModGroupType.Collected].Add(info);
                }
            }
            else
            {
                collectedModsId.Remove(info.Id);
                modsShowInfoFromGroup[ModGroupType.Collected].Remove(info);
            }
            STLog.Instance.WriteLine($"{id} 收藏状态修改为 {info.Collected}", STLogLevel.DEBUG);
        }
        void SeveAllData()
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
            foreach (var mod in enabledModsId)
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
            foreach (var info in modsShowInfo.Values)
            {
                if (info.Collected is true)
                    toml[ModGroupType.Collected].Add(info.Id);
                if (info.ImagePath!.Length > 0 || info.UserDescription!.Length > 0)
                {
                    toml["UserModsData"].Add(new TomlTable()
                    {
                        ["Id"] = info.Id,
                        ["ImagePath"] = info.ImagePath!.Length > 0 ? info.ImagePath : "",
                        ["UserDescription"] = info.UserDescription!.Length > 0 ? info.UserDescription : "",
                    });
                }
            }
            foreach (var kv in userGroups)
            {
                toml.Add(kv.Key, new TomlTable()
                {
                    ["Icon"] = ListBoxItemHelper.GetIcon(listBoxItemsFromGroups[kv.Key]).ToString()!,
                    ["Mods"] = new TomlArray(),
                });
                foreach (var id in kv.Value)
                    toml[kv.Key]["Mods"].Add(id);
            }
            toml.SaveTo(path);
            STLog.Instance.WriteLine($"保存用户分组成功 位置: {path}");
        }
        void ModInfoShowChange(string id)
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
            if (modsShowInfo[info.Id].ImagePath is string imagePath && imagePath.Length > 0)
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
            TextBox_UserDescription.Text = modsShowInfo[info.Id].UserDescription!;
            STLog.Instance.WriteLine($"显示详情 {id}", STLogLevel.DEBUG);
        }
        void DropFile(string filePath)
        {
            string tempPath = $"{AppDomain.CurrentDomain.BaseDirectory}temp";
            var head = "";
            using StreamReader sr = new(filePath);
            {
                head = $"{sr.Read()}{sr.Read()}";
            }
            try
            {

                if (head == "8075")
                {
                    using (var archive = new Archive(filePath, new() { Encoding = Encoding.UTF8 }))
                    {
                        archive.ExtractToDirectory(tempPath);
                    }
                }
                else if (head == "8297")
                {
                    using (var archive = new RarArchive(filePath))
                    {
                        archive.ExtractToDirectory(tempPath);
                    }
                }
                else if (head == "55122")
                {
                    using (var archive = new SevenZipArchive(filePath))
                    {
                        archive.ExtractToDirectory(tempPath);
                    }
                }
                else
                {
                    STLog.Instance.WriteLine($"此文件不是压缩文件 位置: {filePath}");
                    MessageBox.Show($"此文件不是压缩文件\n 位置: {filePath}");
                    return;
                }
            }
            catch
            {
                STLog.Instance.WriteLine($"文件错误 位置:{filePath}");
                MessageBox.Show($"文件错误\n 位置:{filePath}");
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath);
                return;
            }
            DirectoryInfo dirs = new(tempPath);
            var filesInfo = dirs.GetFiles("mod_info.json", SearchOption.AllDirectories);
            if (filesInfo.Length > 0 && filesInfo.First().FullName is string jsonPath)
            {
                var modInfo = GetModInfo(jsonPath);
                if (allModsInfo.ContainsKey(modInfo.Id))
                {
                    var originalModInfo = allModsInfo[modInfo.Id];
                    if (MessageBox.Show($"{modInfo.Id} 已存在 是否覆盖?\n原始版本:{originalModInfo.Version}\n新增版本:{modInfo.Version}", "已存在相同模组", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        ST.CopyDirectory(originalModInfo.Path, modBackUpDirectory);
                        Directory.Delete(originalModInfo.Path, true);
                        ST.CopyDirectory(Path.GetDirectoryName(jsonPath)!, ST.gameModsPath);
                        allModsInfo.Remove(modInfo.Id);
                        allModsInfo.Add(modInfo.Id, modInfo);
                        Dispatcher.BeginInvoke(() =>
                        {
                            RemoveModShowInfo(modInfo.Id);
                            AddModShowInfo(GetModShowInfo(modInfo));
                        });
                        STLog.Instance.WriteLine($"覆盖模组 {modInfo.Id} {originalModInfo.Version} => {modInfo.Version}");
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        AddModShowInfo(GetModShowInfo(modInfo));
                    });
                }
            }
            else
            {
                STLog.Instance.WriteLine($"压缩文件未包含模组信息 位置: {filePath}");
                MessageBox.Show($"压缩文件未包含模组信息\n{filePath}");
            }
            Directory.Delete(tempPath, true);
        }
        ModShowInfo RemoveModShowInfo(string id)
        {
            var modShowInfo = modsShowInfo[id];
            modsShowInfo.Remove(modShowInfo.Id);
            modsShowInfoFromGroup[ModGroupType.All].Remove(modShowInfo);
            modsShowInfoFromGroup[modShowInfo.Group!].Remove(modShowInfo);
            STLog.Instance.WriteLine($"删除模组 {modShowInfo.Id} {modShowInfo.Version}", STLogLevel.DEBUG);
            return modShowInfo;
        }
        void AddModShowInfo(ModShowInfo modShowInfo)
        {
            modsShowInfo.Add(modShowInfo.Id, modShowInfo);
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
            ListBoxItem item = new();
            SetListBoxItemData(item, name);
            ContextMenu contextMenu = new();
            MenuItem menuItem = new();
            menuItem.Header = "重命名分组";
            menuItem.Click += (o, e) =>
            {
                Window_AddGroup window = new();
                ((MainWindow)Application.Current.MainWindow).IsEnabled = false;
                window.Show();
                window.Button_OK.Click += (o, e) =>
                {
                    string _icon = window.TextBox_Icon.Text;
                    string _name = window.TextBox_Name.Text;
                    if (_name.Length > 0 && !userGroups.ContainsKey(_name))
                    {
                        ListBoxItemHelper.SetIcon(item, window.TextBox_Icon.Text);
                        var temp = userGroups[name];
                        userGroups.Remove(name);
                        userGroups.Add(_name, temp);

                        listBoxItemsFromGroups.Remove(name);
                        listBoxItemsFromGroups.Add(_name, item);

                        var _temp = modsShowInfoFromGroup[name];
                        modsShowInfoFromGroup.Remove(name);
                        modsShowInfoFromGroup.Add(_name, _temp);

                        SetListBoxItemData(item, _name);
                        window.Close();
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
                userGroups.Remove(_name);
                listBoxItemsFromGroups.Remove(_name);
                modsShowInfoFromGroup.Remove(_name);
            };
            contextMenu.Items.Add(menuItem);
            STLog.Instance.WriteLine($"{name} 分组添加右键菜单 {menuItem.Header}", STLogLevel.DEBUG);
            item.ContextMenu = contextMenu;
            ListBoxItemHelper.SetIcon(item, icon);
            ListBox_UserGroup.Items.Add(item);
            userGroups.Add(name, new());
            listBoxItemsFromGroups.Add(name, item);
            modsShowInfoFromGroup.Add(name, new());
            STLog.Instance.WriteLine($"添加用户分组 {icon} {name}");
            //RefreAllSizeOfListBoxItems();
            //RefreshModsShowInfoContextMenu();
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
                ShowGroupChange(GetSearchModsShowInfo(text, type));
                STLog.Instance.WriteLine($"模组搜索 {text}");
            }
            else
            {
                ShowGroupChange(nowGroup);
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
    }
}
