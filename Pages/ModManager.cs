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
        const string modGroupPath = @"ModGroup.toml";
        readonly static Uri modGroupUri = new("/Resources/ModGroup.toml", UriKind.Relative);
        const string userGroupPath = @"UserGroup.toml";
        Dictionary<string, ModInfo> allModsInfo = new();
        HashSet<string> enabledModsId = new();
        HashSet<string> collectedModsId = new();
        Dictionary<string, HashSet<string>> allModsGroupId = new();
        Dictionary<string, ListBoxItem> allListBoxItemsFromGroup = new();
        Dictionary<string, ModShowInfo> allModShowInfo = new();

        bool groupMenuOpen = false;
        bool showModInfo = false;
        string? nowSelectedMod = null;
        string nowGroup = GroupType.All;
        Dictionary<string, HashSet<string>> allUserGroup = new();
        static class GroupType
        {
            /// <summary>全部模组</summary>
            public const string All = "All";
            /// <summary>已启用模组</summary>
            public const string Enabled = "Enabled";
            /// <summary>未启用模组</summary>
            public const string Disable = "Disable";
            /// <summary>前置模组</summary>
            public const string Libraries = "Libraries";
            /// <summary>大型模组</summary>
            public const string Megamods = "Megamods";
            /// <summary>派系模组</summary>
            public const string FactionMods = "FactionMods";
            /// <summary>内容模组</summary>
            public const string ContentExpansions = "ContentExpansions";
            /// <summary>功能模组</summary>
            public const string UtilityMods = "UtilityMods";
            /// <summary>闲杂模组</summary>
            public const string MiscellaneousMods = "MiscellaneousMods";
            /// <summary>美化模组</summary>
            public const string BeautifyMods = "BeautifyMods";
            /// <summary>全部模组</summary>
            public const string Unknown = "Unknown";
            /// <summary>已收藏模组</summary>
            public const string Collected = "Collected";
        };

        Dictionary<string, ObservableCollection<ModShowInfo>> allShowModInfoAtGroup = new()
        {
            {GroupType.All,new() },
            {GroupType.Enabled,new() },
            {GroupType.Disable,new() },
            {GroupType.Libraries,new() },
            {GroupType.Megamods,new() },
            {GroupType.FactionMods,new() },
            {GroupType.ContentExpansions,new() },
            {GroupType.UtilityMods,new() },
            {GroupType.MiscellaneousMods,new() },
            {GroupType.BeautifyMods,new() },
            {GroupType.Unknown,new() },
            {GroupType.Collected,new() },
        };
        class ButtonStyle
        {
            public Style Enabled = null!;
            public Style Disable = null!;
            public Style Collected = null!;
            public Style Uncollected = null!;
        }
        readonly ButtonStyle buttonStyle = new();
        class LabelStyle
        {
            public Style VersionNormal = null!;
            public Style VersionWarn = null!;
            public Style IsUtility = null!;
            public Style NotUtility = null!;
        }
        readonly LabelStyle labelStyle = new();
        public class ModShowInfo : INotifyPropertyChanged
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Author { get; set; }
            public string? Version { get; set; }
            public string? GameVersion { get; set; }
            private Style? gameVersionStyle = null;
            public Style? GameVersionStyle
            {
                get { return gameVersionStyle; }
                set
                {
                    gameVersionStyle = value;
                    PropertyChanged?.Invoke(this, new(nameof(GameVersionStyle)));
                }
            }
            public bool? Utility { get; set; }
            private Style? utilityStyle = null;
            public Style? UtilityStyle
            {
                get { return utilityStyle; }
                set
                {
                    utilityStyle = value;
                    PropertyChanged?.Invoke(this, new(nameof(UtilityStyle)));
                }
            }
            public bool? Enabled { get; set; }
            public string? ImagePath { get; set; }
            public string? Group { get; set; }
            private string? dependencies { get; set; }
            public string? Dependencies
            {
                get { return dependencies; }
                set
                {
                    dependencies = value;
                    PropertyChanged?.Invoke(this, new(nameof(Dependencies)));
                }
            }
            public List<string>? DependenciesList { get; set; }
            private double rowDetailsHight { get; set; }
            public double RowDetailsHight
            {
                get { return rowDetailsHight; }
                set
                {
                    rowDetailsHight = value;
                    PropertyChanged?.Invoke(this, new(nameof(RowDetailsHight)));
                }
            }
            public string? UserDescription { get; set; }
            private Style? enabledStyle = null;
            public Style? EnabledStyle
            {
                get { return enabledStyle; }
                set
                {
                    enabledStyle = value;
                    PropertyChanged?.Invoke(this, new(nameof(EnabledStyle)));
                }
            }
            public bool? Collected { get; set; }
            private Style? collectedStyle = null;
            public Style? CollectedStyle
            {
                get { return collectedStyle; }
                set
                {
                    collectedStyle = value;
                    PropertyChanged?.Invoke(this, new(nameof(CollectedStyle)));
                }
            }
            private ContextMenu? contextMenu = null;
            public ContextMenu? ContextMenu
            {
                get { return contextMenu; }
                set
                {
                    contextMenu = value;
                    PropertyChanged?.Invoke(this, new(nameof(ContextMenu)));
                }
            }
            public event PropertyChangedEventHandler? PropertyChanged;
        }
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
        void GetAllMods()
        {
            List<string> errList = null!;
            DirectoryInfo dirs = new(Global.gameModsPath);
            string nope = null!;
            foreach (var dir in dirs.GetDirectories())
            {
                try
                {
                    string datas = File.ReadAllText($"{dir.FullName}\\mod_info.json");
                    datas = Regex.Replace(datas, @"(#|//)[\S ]*", "");
                    datas = Regex.Replace(datas, @",(?=[\r\n \t]*[\]\}])|(?<=[\}\]]),[ \t]*\r?\Z", "");
                    JsonNode jsonData = JsonNode.Parse(datas)!;
                    ModInfo modInfo = new();
                    foreach (var data in jsonData.AsObject())
                        modInfo.SetData(data);
                    modInfo.Path = dir.FullName;
                    allModsInfo.Add(modInfo.Id!, modInfo);
                }
                catch (Exception e)
                {
                    errList ??= new();
                    nope ??= "以下模组加载错误\n";
                    nope += $"{dir.Name}\n";
                    errList.Add($"ModPath:{dir.FullName}\n Message:{e.Message}\n");
                }
            }
            if (nope != null)
                MessageBox.Show(nope, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        void GetEnabledMods()
        {
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
                        var key = mod!.ToString();
                        if (allModsInfo.ContainsKey(key))
                        {
                            enabledModsId.Add(key);
                        }
                        else
                        {
                            err ??= "并未找到游戏启用模组列表中的以下模组:\n";
                            err += $"{key}\n";
                        }
                    }
                    if (err != null)
                        MessageBox.Show(err, MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception)
                {
                    MessageBox.Show("启用列表载入错误", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        void GetAllModGroup()
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
                {
                    allModsGroupId.Add(kv.Key, new());
                    foreach (string id in kv.Value.AsTomlArray)
                        allModsGroupId[kv.Key].Add(id);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("???", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
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
            using TomlTable toml = TOML.Parse(path);
            List<string> errList = new();
            try
            {
                foreach (var kv in toml)
                {
                    switch (kv.Key)
                    {
                        case GroupType.Collected:
                            string err = null!;
                            foreach (string id in kv.Value.AsTomlArray)
                            {
                                if (allModShowInfo.ContainsKey(id))
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
                            break;
                        case "UserModsData":
                            string err1 = null!;
                            foreach (var dic in kv.Value.AsTomlArray)
                            {
                                var id = dic["Id"].AsString;
                                if (allModShowInfo.ContainsKey(id))
                                {
                                    var info = allModShowInfo[id];
                                    info.ImagePath = dic["ImagePath"];
                                    info.UserDescription = dic["UserDescription"];
                                }
                                else
                                {
                                    err1 ??= "并未找到以下包含自定义数据的ModId\n";
                                    err1 += $"    {id}\n";
                                }
                            }
                            if (err1 is not null)
                                errList.Add(err1);
                            break;
                        default:
                            string err2 = null!;
                            string group = kv.Key;
                            if (!allUserGroup.ContainsKey(group))
                            {
                                AddUserGroup(kv.Value["Icon"], kv.Key);
                                foreach (string id in kv.Value["Mods"].AsTomlArray)
                                {
                                    if (allModShowInfo.ContainsKey(id))
                                    {
                                        allUserGroup[group].Add(id);
                                        allShowModInfoAtGroup[group].Add(allModShowInfo[id]);
                                    }
                                    else
                                    {
                                        err2 ??= $"并未用户分组 {group} 找到以下ModId\n";
                                        err2 += $"    {id}\n";
                                    }
                                }
                            }
                            else
                                err2 ??= $"用户分组 {group} 已存在";
                            if (err2 is not null)
                                errList.Add(err2);
                            break;
                    }
                }
                if (errList.Count > 0)
                {
                    MessageBox.Show(string.Join("\n", errList), MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("文件载入错误", MessageBoxCaption_I18n.Warn, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SetAllSizeInListBoxItem();
        }
        void GetAllListBoxItem()
        {
            foreach (ListBoxItem item in ListBox_ModsGroupMenu.Items)
            {
                if (item.Content is string str)
                    allListBoxItemsFromGroup.Add(item.Tag.ToString()!, item);
                else if (item.Content is Expander expander && expander.Content is ListBox listBox)
                    foreach (ListBoxItem item1 in listBox.Items)
                        allListBoxItemsFromGroup.Add(item1.Tag.ToString()!, item1);
            }
        }
        void InitializeDataGridItemsSource()
        {
            foreach (var kv in allModsInfo)
            {
                ModInfo info = kv.Value;
                ModShowInfo showInfo = GetModShowInfo(info);
                showInfo.ContextMenu = CreateContextMenu(showInfo);
                allShowModInfoAtGroup[GroupType.All].Add(showInfo);
                if (showInfo.Enabled is true)
                    allShowModInfoAtGroup[GroupType.Enabled].Add(showInfo);
                else
                    allShowModInfoAtGroup[GroupType.Disable].Add(showInfo);
                allShowModInfoAtGroup[showInfo.Group!].Add(showInfo);
                allModShowInfo.Add(info.Id!, showInfo);
            }
            ShowGroupChange(GroupType.All);
            ListBox_ModsGroupMenu.SelectedIndex = 0;
            CheckEnabledModsDependencies();
        }
        void ShowGroupChange(string group)
        {
            DataGrid_ModsShowList.ItemsSource = allShowModInfoAtGroup[group];
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
            return showInfo;
        }
        void SetAllSizeInListBoxItem()
        {
            foreach (var item in allListBoxItemsFromGroup.Values)
                item.Content = $"{item.Content.ToString()!.Split(" ")[0]} ({allShowModInfoAtGroup[item.Tag.ToString()!].Count})"; ;
        }
        string CheckGroup(string id)
        {
            foreach (var group in allModsGroupId)
                if (group.Value.Contains(id))
                    return group.Key;
            return GroupType.Unknown;
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
            foreach (var info in allModShowInfo.Values)
            {
                info.ContextMenu = CreateContextMenu(info);
            }
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
            if (allUserGroup.Count > 0)
            {
                item = new();
                item.Header = "添加至用户分组";
                foreach (var group in allUserGroup.Keys)
                {
                    if (!allUserGroup[group].Contains(info.Id!))
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
            var haveModGroup = allUserGroup.Where(g => g.Value.Contains(info.Id!));
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
            ModShowInfo info = allModShowInfo[id];
            if (status)
            {
                if (allUserGroup[group].Add(id))
                    allShowModInfoAtGroup[group].Add(allModShowInfo[id]);
            }
            else
            {
                allUserGroup[group].Remove(id);
                allShowModInfoAtGroup[group].Remove(allModShowInfo[id]);
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
        void ModEnabledChange(string id, bool? enabled = null)
        {
            ModShowInfo info = allModShowInfo[id];
            info.Enabled = enabled is null ? !info.Enabled : enabled;
            info.EnabledStyle = info.Enabled is true ? buttonStyle.Enabled : buttonStyle.Disable;
            info.ContextMenu = CreateContextMenu(info);
            if (info.Enabled is true)
            {
                if (enabledModsId.Add(info.Id!))
                {
                    allShowModInfoAtGroup[GroupType.Disable].Remove(info);
                    allShowModInfoAtGroup[GroupType.Enabled].Add(info);
                }
            }
            else
            {
                if (enabledModsId.Remove(info.Id!))
                {
                    allShowModInfoAtGroup[GroupType.Enabled].Remove(info);
                    allShowModInfoAtGroup[GroupType.Disable].Add(info);
                }
            }
        }
        void CheckEnabledModsDependencies()
        {
            foreach (var info in allShowModInfoAtGroup[GroupType.Enabled])
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
            ModShowInfo info = allModShowInfo[id];
            info.Collected = collected is null ? !info.Collected : collected;
            info.CollectedStyle = info.Collected is true ? buttonStyle.Collected : buttonStyle.Uncollected;
            info.ContextMenu = CreateContextMenu(info);
            if (info.Collected is true)
            {
                if (!CheckCollected(info.Id!))
                {
                    collectedModsId.Add(info.Id!);
                    allShowModInfoAtGroup[GroupType.Collected].Add(info);
                }
            }
            else
            {
                collectedModsId.Remove(info.Id!);
                allShowModInfoAtGroup[GroupType.Collected].Remove(info);
            }
        }
        void SearchMods()
        {
            if (TextBox_ModsSearch.Text.Length > 0 && TextBox_ModsSearch.Text is string text)
            {
                ObservableCollection<ModShowInfo> showModInfos = new();
                switch (((ComboBoxItem)ComboBox_SearchType.SelectedItem).Tag.ToString()!)
                {
                    case "Name":
                        foreach (var info in allShowModInfoAtGroup[nowGroup].Where(i => i.Name!.Contains(text)))
                            showModInfos.Add(info);
                        break;
                    case "Id":
                        foreach (var info in allShowModInfoAtGroup[nowGroup].Where(i => i.Id!.Contains(text)))
                            showModInfos.Add(info);
                        break;
                    case "Author":
                        foreach (var info in allShowModInfoAtGroup[nowGroup].Where(i => i.Author!.Contains(text)))
                            showModInfos.Add(info);
                        break;
                    case "UserDescription":
                        foreach (var info in allShowModInfoAtGroup[nowGroup].Where(i => i.UserDescription!.Contains(text)))
                            showModInfos.Add(info);
                        break;
                }
                DataGrid_ModsShowList.ItemsSource = showModInfos;
            }
            else
                DataGrid_ModsShowList.ItemsSource = allShowModInfoAtGroup[nowGroup];
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
                { GroupType.Collected, new TomlArray() },
                { "UserModsData", new TomlArray() }
            };
            foreach (var info in allModShowInfo.Values)
            {
                if (info.Collected is true)
                    toml[GroupType.Collected].Add(info.Id!);
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
            foreach (var kv in allUserGroup)
            {
                toml.Add(kv.Key, new TomlTable()
                {
                    ["Icon"] = ListBoxItemHelper.GetIcon(allListBoxItemsFromGroup[kv.Key]).ToString()!,
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
            if (allModShowInfo[info.Id!].ImagePath is string imagePath && File.Exists(imagePath))
                Image_ModImage.Source = new BitmapImage(new(imagePath));
            Label_ModName.Content = info.Name;
            Label_ModId.Content = info.Id;
            Label_ModVersion.Content = info.Version;
            Label_GameVersion.Content = info.GameVersion;
            Button_ModPath.Content = info.Path;
            TextBlock_ModAuthor.Text = info.Author;
            if (info.Dependencies is List<ModInfo> list)
                TextBlock_ModDependencies.Text = string.Join("\n", list.Select(i => $"{"名称:"} {i.Name} {"ID:"} {i.Id} " + (i.Version is not null ? $"{"版本"} {i.Version}" : ""))!);
            TextBlock_ModDescription.Text = info.Description;
            TextBox_UserDescription.Text = allModShowInfo[info.Id!].UserDescription!;
        }
    }
}
