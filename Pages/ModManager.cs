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
            labelStyle.VersionWarn = (Style)Resources["VersionWarn"];
            labelStyle.IsUtility = (Style)Resources["IsUtilityStyle"];
            labelStyle.NotUtility = (Style)Resources["NotUtilityStyle"];
        }
        void GetAllModsInfo()
        {
            DirectoryInfo dirs = new(Global.gameModsPath);
            string err = null!;
            foreach (var dir in dirs.GetDirectories())
            {
                try
                {
                    ModInfo modInfo = GetModInfo($"{dir.FullName}\\mod_info.json");
                    allModsInfo.Add(modInfo.Id!, modInfo);
                    //string datas = File.ReadAllText();
                    //datas = Regex.Replace(datas, @"(#|//)[\S ]*", "");
                    //datas = Regex.Replace(datas, @",(?=[\r\n \t]*[\]\}])|(?<=[\}\]]),[ \t]*\r?\Z", "");
                    //JsonNode jsonData = JsonNode.Parse(datas)!;
                    //ModInfo modInfo = new();
                    //foreach (var data in jsonData.AsObject())
                    //    modInfo.SetData(data);
                    //modInfo.Path = dir.FullName;
                    //allModsInfo.Add(modInfo.Id!, modInfo);
                }
                catch
                {
                    err ??= "以下模组加载错误\n";
                    err += $"{dir.Name}\n";
                }
            }
            if (err != null)
                MessageBox.Show(err, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        ModInfo GetModInfo(string jsonPath)
        {
            string datas = File.ReadAllText(jsonPath);
            datas = Regex.Replace(datas, @"(#|//)[\S ]*", "");
            datas = Regex.Replace(datas, @",(?=[\r\n \t]*[\]\}])|(?<=[\}\]]),[ \t]*\r?\Z", "");
            JsonNode jsonData = JsonNode.Parse(datas)!;
            ModInfo modInfo = new();
            foreach (var data in jsonData.AsObject())
                modInfo.SetData(data);
            modInfo.Path = Global.GetDirectory(jsonPath)!;
            return modInfo;
        }
        void GetEnabledMods()
        {
            if (!File.Exists(Global.enabledModsJsonPath))
            {
                MessageBox.Show($"启用列表不存在\n{Global.enabledModsJsonPath}", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            string datas = File.ReadAllText(Global.enabledModsJsonPath);
            if (datas.Length > 0)
            {
                try
                {
                    string err = null!;
                    JsonNode enabledModsJson = JsonNode.Parse(datas)!;
                    JsonArray enabledModsJsonArray = enabledModsJson["enabledMods"]!.AsArray();
                    foreach (var mod in enabledModsJsonArray)
                    {
                        var id = mod!.GetValue<string>();
                        if (allModsInfo.ContainsKey(id))
                        {
                            if (!enabledModsId.Add(id))
                            {
                                err ??= "";
                                err += $"{id} 已存在\n";
                            }
                        }
                        else
                        {
                            err ??= "并未找到游戏启用模组列表中的以下模组:\n";
                            err += $"{id}\n";
                        }
                    }
                    if (err != null)
                        MessageBox.Show(err, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch
                {
                    MessageBox.Show("启用列表载入错误", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        void CheckUserGroup()
        {
            if (!File.Exists(userGroupPath))
            {
                File.Create(userGroupPath).Close();
            }
            else
            {
                GetAllUserGroup(userGroupPath);
            }
        }
        void GetAllUserGroup(string path)
        {
            string err = null!;
            using TomlTable toml = TOML.Parse(path);
            List<string> errList = new();
            try
            {
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
                                err ??= "并未找到以下已收藏的ModId\n";
                                err += $"    {id}\n";
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
                                err ??= "并未找到以下包含自定义数据的ModId\n";
                                err += $"    {id}\n";
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
                            AddUserGroup(kv.Value["Icon"], kv.Key);
                            foreach (string id in kv.Value["Mods"].AsTomlArray)
                            {
                                if (modsShowInfo.ContainsKey(id))
                                {
                                    if (userGroups[group].Add(id))
                                        modsShowInfoFromGroup[group].Add(modsShowInfo[id]);
                                    else
                                    {
                                        err ??= "";
                                        err += $"模组{id}已存在";
                                    }
                                }
                                else
                                {
                                    err ??= $"并未用户分组 {group} 找到以下ModId\n";
                                    err += $"    {id}\n";
                                }
                            }
                        }
                        else
                            err ??= $"用户分组 {group} 已存在";
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
                MessageBox.Show("用户数据文件载入错误", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SetAllSizeInListBoxItem();
        }
        void GetAllListBoxItem()
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
            if (!File.Exists(modGroupPath))
            {
                using StreamReader sr = new(Application.GetResourceStream(modGroupUri).Stream);
                File.WriteAllText(modGroupPath, sr.ReadToEnd());
            }
            try
            {
                using TomlTable toml = TOML.Parse(modGroupPath);
                foreach (var kv in toml)
                    foreach (string id in kv.Value.AsTomlArray)
                        modsIdFromGroups[kv.Key].Add(id);
            }
            catch (Exception)
            {
                MessageBox.Show("???", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
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
                //modsShowInfo.Add(info.Id!, showInfo);
                //modsShowInfoFromGroup[ModGroupType.All].Add(showInfo);
                //modsShowInfoFromGroup[showInfo.Group!].Add(showInfo);
                if (showInfo.Enabled is true)
                    modsShowInfoFromGroup[ModGroupType.Enabled].Add(showInfo);
                else
                    modsShowInfoFromGroup[ModGroupType.Disable].Add(showInfo);
            }
            ShowGroupChange(ModGroupType.All);
            ListBox_ModsGroupMenu.SelectedIndex = 0;
            CheckEnabledModsDependencies();
        }
        void ShowGroupChange(string group)
        {
            DataGrid_ModsShowList.ItemsSource = modsShowInfoFromGroup[group];
        }
        ModShowInfo GetModShowInfo(ModInfo info)
        {
            bool isCollected = CheckCollected(info.Id!);
            bool isEnabled = CheckEnabled(info.Id!);
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
                GameVersionStyle = info.GameVersion == Global.gameVersion ? labelStyle.VersionNormal : labelStyle.VersionWarn,
                RowDetailsHight = 0,
                Dependencies = "",
                DependenciesList = info.Dependencies is not null ? info.Dependencies.Select(i => i.Id!).ToList() : null!,
                ImagePath = "",
                UserDescription = "",
                Utility = info.Utility,
                UtilityStyle = info.Utility is true ? labelStyle.IsUtility : labelStyle.NotUtility,
                Group = CheckGroup(info.Id!),
            };
            showInfo.ContextMenu = CreateContextMenu(showInfo);
            return showInfo;
        }
        void SetAllSizeInListBoxItem()
        {
            foreach (var item in listBoxItemsFromGroups.Values)
                item.Content = $"{item.Content.ToString()!.Split(" ")[0]} ({modsShowInfoFromGroup[item.Tag.ToString()!].Count})"; ;
        }
        bool CheckEnabled(string id)
        {
            return enabledModsId.Contains(id);
        }
        bool CheckCollected(string id)
        {
            return collectedModsId.Contains(id);
        }
        void RefreshShowModsItemContextMenu()
        {
            foreach (var info in modsShowInfo.Values)
                info.ContextMenu = CreateContextMenu(info);
        }
        ContextMenu CreateContextMenu(ModShowInfo info)
        {
            ContextMenu menu = new();
            MenuItem item = new();
            item.Header = info.Enabled is true ? "禁用所选模组" : "启用所选模组";
            item.Click += (o, e) => SelectedModsEnabledChange(info.Enabled is not true);
            menu.Items.Add(item);
            item = new();
            item.Header = info.Collected is true ? "取消收藏所选模组" : "收藏所选模组";
            item.Click += (o, e) => SelectedModsCollectedChange(info.Collected is not true);
            menu.Items.Add(item);
            item = new();
            item.Header = "打开模组文件夹";
            item.Click += (o, e) =>
            {
                System.Diagnostics.Process.Start("Explorer.exe", allModsInfo[info.Id!].Path);
            };
            menu.Items.Add(item);
            if (userGroups.Count > 0)
            {
                item = new();
                item.Header = "添加至用户分组";
                foreach (var group in userGroups.Keys)
                {
                    if (!userGroups[group].Contains(info.Id!))
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
                    menu.Items.Add(item);
            }
            var haveModGroup = userGroups.Where(g => g.Value.Contains(info.Id!));
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
            }
            return menu;
        }
        void SelectedModsUserGroupChange(string group, bool status)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo info = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ModUserGroupChange(group, info.Id!, status);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            SetAllSizeInListBoxItem();
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
        }
        void SelectedModsEnabledChange(bool? enabled = null)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo info = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ModEnabledChange(info.Id!, enabled);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            SetAllSizeInListBoxItem();
            if (conut != DataGrid_ModsShowList.SelectedItems.Count)
                CloseModInfo();
            CheckEnabledModsDependencies();
        }
        void ClearEnabledMod()
        {
            foreach (var info in modsShowInfoFromGroup[ModGroupType.Enabled])
            {
                modsShowInfoFromGroup[ModGroupType.Disable].Add(info);
            }
            modsShowInfoFromGroup[ModGroupType.Enabled].Clear();
            enabledModsId.Clear();
        }
        void ModEnabledChange(string id, bool? enabled = null)
        {
            ModShowInfo info = modsShowInfo[id];
            info.Enabled = enabled is null ? !info.Enabled : enabled;
            info.EnabledStyle = info.Enabled is true ? buttonStyle.Enabled : buttonStyle.Disable;
            info.ContextMenu = CreateContextMenu(info);
            if (info.Enabled is true)
            {
                if (enabledModsId.Add(info.Id!))
                {
                    modsShowInfoFromGroup[ModGroupType.Disable].Remove(info);
                    modsShowInfoFromGroup[ModGroupType.Enabled].Add(info);
                }
            }
            else
            {
                if (enabledModsId.Remove(info.Id!))
                {
                    modsShowInfoFromGroup[ModGroupType.Enabled].Remove(info);
                    modsShowInfoFromGroup[ModGroupType.Disable].Add(info);
                }
            }
        }
        void CheckEnabledModsDependencies()
        {
            foreach (var info in modsShowInfoFromGroup[ModGroupType.Enabled])
            {
                if (info.DependenciesList != null)
                {
                    info.Dependencies = string.Join(" , ", info.DependenciesList.Where(s => !enabledModsId.Contains(s)));
                    if (info.Dependencies.Length > 0)
                        info.RowDetailsHight = 30;
                    else
                        info.RowDetailsHight = 0;
                }
            }
        }
        void SelectedModsCollectedChange(bool collected = false)
        {
            int conut = DataGrid_ModsShowList.SelectedItems.Count;
            for (int i = 0; i < DataGrid_ModsShowList.SelectedItems.Count;)
            {
                ModShowInfo info = (ModShowInfo)DataGrid_ModsShowList.SelectedItems[i]!;
                ModCollectedChange(info.Id!, collected);
                if (conut == DataGrid_ModsShowList.SelectedItems.Count)
                    i++;
            }
            SetAllSizeInListBoxItem();
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
                if (!CheckCollected(info.Id!))
                {
                    collectedModsId.Add(info.Id!);
                    modsShowInfoFromGroup[ModGroupType.Collected].Add(info);
                }
            }
            else
            {
                collectedModsId.Remove(info.Id!);
                modsShowInfoFromGroup[ModGroupType.Collected].Remove(info);
            }
        }
        void SeveAllData()
        {
            SaveEnabledMods(Global.enabledModsJsonPath);
            SaveUserGroup(userGroupPath);
        }
        void SaveEnabledMods(string path)
        {
            JsonObject keyValues = new();
            keyValues.Add("enabledMods", new JsonArray());
            foreach (var mod in enabledModsId)
                ((JsonArray)keyValues["enabledMods"]!).Add(mod);
            File.WriteAllText(path, keyValues.ToJsonString(new() { WriteIndented = true }));
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
                    toml[ModGroupType.Collected].Add(info.Id!);
                if (info.ImagePath!.Length > 0 || info.UserDescription!.Length > 0)
                {
                    toml["UserModsData"].Add(new TomlTable()
                    {
                        ["Id"] = info.Id!,
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

        }
        void SetModInfo(string id)
        {
            ModInfo info = allModsInfo[id];
            if (modsShowInfo[info.Id!].ImagePath is string imagePath && File.Exists(imagePath))
                Image_ModImage.Source = new BitmapImage(new(imagePath));
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
            TextBox_UserDescription.Text = modsShowInfo[info.Id!].UserDescription!;
        }
    }
}
