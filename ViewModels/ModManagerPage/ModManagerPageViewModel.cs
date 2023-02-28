using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Libs.Log4Cs;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialogs;
using StarsectorTools.Libs.GameInfo;
using StarsectorTools.Libs.Utils;
using I18nRes = StarsectorTools.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorTools.ViewModels.ModManagerPage
{
    internal partial class ModManagerPageViewModel : ObservableObject
    {
        #region ObservableProperty

        [ObservableProperty]
        private ObservableI18n<I18nRes> i18n = ObservableI18n<I18nRes>.Create(new());

        [ObservableProperty]
        private bool groupMenuIsExpand = false;

        [ObservableProperty]
        private ObservableCollection<ModShowInfo> nowShowMods = new();

        /// <summary>当前选择的模组</summary>
        private List<ModShowInfo> nowSelectedMods = new();

        /// <summary>当前选择的模组</summary>
        private ModShowInfo? nowSelectedMod;

        /// <summary>模组详情的展开状态</summary>
        [ObservableProperty]
        private bool isShowModDetails = false;

        [ObservableProperty]
        private bool showModDependencies = false;

        [ObservableProperty]
        private BitmapImage? modDetailImage;

        [ObservableProperty]
        private string? modDetailName;

        [ObservableProperty]
        private string? modDetailId;

        [ObservableProperty]
        private string? modDetailModVersion;

        [ObservableProperty]
        private string? modDetailGameVersion;

        [ObservableProperty]
        private string? modDetailPath;

        [ObservableProperty]
        private string? modDetailAuthor;

        [ObservableProperty]
        private string? modDetailDescription;

        [ObservableProperty]
        private string? modDetailDependencies;

        [ObservableProperty]
        private string? modDetailUserDescription;

        [ObservableProperty]
        private string modFilterText = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RandomEnableModsCommand))]
        private string minRandomSize;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RandomEnableModsCommand))]
        private string maxRandomSize;

        [ObservableProperty]
        private bool isRemindSave = false;

        [ObservableProperty]
        private bool nowSelectedIsUserGroup = false;

        #endregion

        /// <summary>当前选择的列表项</summary>
        private ListBoxItemVM nowSelectedGroup = null!;
        private string nowSelectedGroupName => nowSelectedGroup!.Tag!.ToString()!;
        #region ListBox
        [ObservableProperty]
        private ListBoxVM listBox_MainMenu =
            new()
            {
                new()
                {
                    Icon = "🅰",
                    Content = I18nRes.AllMods,ToolTip = I18nRes.AllMods,
                    Tag = ModTypeGroup.All
                },
                new()
                {
                    Icon = "✅",
                    Content = I18nRes.EnabledMods,ToolTip = I18nRes.EnabledMods,
                    Tag = ModTypeGroup.Enabled
                },
                new()
                {
                    Icon = "❎",
                    Content = I18nRes.DisabledMods,ToolTip = I18nRes.DisabledMods,
                    Tag = ModTypeGroup.Disabled
                },
            };

        [ObservableProperty]
        private ListBoxVM listBox_TypeGroupMenu =
            new()
            {
                new()
                {
                    Icon = "🔝",
                    Content = I18nRes.Libraries,ToolTip = I18nRes.Libraries,
                    Tag = ModTypeGroup.Libraries
                },
                new()
                {
                    Icon = "☢",
                    Content = I18nRes.MegaMods,ToolTip = I18nRes.MegaMods,
                    Tag = ModTypeGroup.MegaMods
                },
                new()
                {
                    Icon = "🏁",
                    Content = I18nRes.FactionMods,ToolTip = I18nRes.FactionMods,
                    Tag = ModTypeGroup.FactionMods
                },
                new()
                {
                    Icon = "🆙",
                    Content = I18nRes.ContentExtensions,ToolTip = I18nRes.ContentExtensions,
                    Tag = ModTypeGroup.ContentExtensions
                },
                new()
                {
                    Icon = "🛠",
                    Content = I18nRes.UtilityMods,ToolTip = I18nRes.UtilityMods,
                    Tag = ModTypeGroup.UtilityMods
                },
                new()
                {
                    Icon = "🛄",
                    Content = I18nRes.MiscellaneousMods,ToolTip = I18nRes.MiscellaneousMods,
                    Tag = ModTypeGroup.MiscellaneousMods
                },
                new()
                {
                    Icon = "✨",
                    Content = I18nRes.BeautifyMods,ToolTip = I18nRes.BeautifyMods,
                    Tag = ModTypeGroup.BeautifyMods
                },
                new()
                {
                    Icon = "🆓",
                    Content = I18nRes.UnknownMods,ToolTip = I18nRes.UnknownMods,
                    Tag = ModTypeGroup.UnknownMods
                },
            };

        [ObservableProperty]
        private ListBoxVM listBox_UserGroupMenu =
            new()
            {
                new()
                {
                    Icon = "🌟",
                    Content = I18nRes.CollectedMods,ToolTip = I18nRes.CollectedMods,
                    Tag = ModTypeGroup.Collected
                },
            };
        #endregion

        #region ComboBox
        [ObservableProperty]
        private ComboBoxVM comboBox_ModFilterType =
            new()
            {
                new() { Content = I18nRes.Name ,Tag = nameof(I18nRes.Name) },
                new() { Content = "Id",ToolTip = "Id", Tag = "Id" },
                new() { Content = I18nRes.Author, Tag = nameof(I18nRes.Author) },
                new() { Content = I18nRes.UserDescription, Tag = nameof(I18nRes.UserDescription) },
            };

        [ObservableProperty]
        private ComboBoxVM comboBox_ExportUserGroup = new()
        {
            new(){Content = I18nRes.All ,Tag= nameof(I18nRes.All)}
        };
        #endregion
        public ModManagerPageViewModel()
        {
        }

        public ModManagerPageViewModel(bool noop)
        {
            InitializeData();
            I18n.AddPropertyChangedAction(I18nPropertyChangeAction);
            ListBox_MainMenu.SelectionChangedEvent += ListBox_Menu_SelectionChangedEvent;
            ListBox_TypeGroupMenu.SelectionChangedEvent += ListBox_Menu_SelectionChangedEvent;
            ListBox_UserGroupMenu.SelectionChangedEvent += ListBox_Menu_SelectionChangedEvent;
            ComboBox_ModFilterType.SelectedIndex = 0;
            ListBox_MainMenu.SelectedIndex = 0;
            ComboBox_ExportUserGroup.SelectedIndex = 0;
        }

        private void I18nPropertyChangeAction()
        {
            ListBox_MainMenu[0].ToolTip = I18nRes.AllMods;
            ListBox_MainMenu[1].ToolTip = I18nRes.EnabledMods;
            ListBox_MainMenu[2].ToolTip = I18nRes.DisabledMods;
            ListBox_TypeGroupMenu[0].ToolTip = I18nRes.Libraries;
            ListBox_TypeGroupMenu[1].ToolTip = I18nRes.MegaMods;
            ListBox_TypeGroupMenu[2].ToolTip = I18nRes.FactionMods;
            ListBox_TypeGroupMenu[3].ToolTip = I18nRes.ContentExtensions;
            ListBox_TypeGroupMenu[4].ToolTip = I18nRes.UtilityMods;
            ListBox_TypeGroupMenu[5].ToolTip = I18nRes.MiscellaneousMods;
            ListBox_TypeGroupMenu[6].ToolTip = I18nRes.BeautifyMods;
            ListBox_TypeGroupMenu[7].ToolTip = I18nRes.UnknownMods;
            ListBox_UserGroupMenu[0].ToolTip = I18nRes.CollectedMods;
            ComboBox_ModFilterType[0].Content = I18nRes.Name;
            ComboBox_ModFilterType[1].Content = I18nRes.Author;
            ComboBox_ModFilterType[2].Content = I18nRes.UserDescription;
            ComboBox_ExportUserGroup[0].Content = I18nRes.All;
            RefreshGroupModCount();
            RefreshModsContextMenu();
        }

        private void ListBox_Menu_SelectionChangedEvent(ListBoxItemVM item)
        {
            if (item is null || nowSelectedGroup == item)
                return;
            // 若切换选择,可取消原来的选中状态,以此达到多列表互斥
            if (nowSelectedGroup?.IsSelected is true)
                nowSelectedGroup.IsSelected = false;
            nowSelectedGroup = item;
            if (allUserGroups.ContainsKey(item.Tag!.ToString()!))
                NowSelectedIsUserGroup = true;
            else
                NowSelectedIsUserGroup = false;
            CheckFilterAndRefreshShowMods();
            Logger.Record("114514");
        }
        #region RelayCommand
        [RelayCommand]
        private void GroupMenuExpand()
        {
            GroupMenuIsExpand = !GroupMenuIsExpand;
        }

        [RelayCommand]
        private void DataGridSelectionChanged(IList items)
        {
            nowSelectedMods = new(items.OfType<ModShowInfo>());
            ChangeShowModDetails(nowSelectedMods.LastOrDefault(defaultValue: null!));
        }

        [RelayCommand]
        private void Collected()
        {
            if (nowSelectedMod is null)
                return;
            ChangeSelectedModsCollected(!nowSelectedMod.IsCollected);
        }

        [RelayCommand]
        private void Enabled()
        {
            if (nowSelectedMod is null)
                return;
            ChangeSelectedModsEnabled(!nowSelectedMod.IsEnabled);
        }

        [RelayCommand]
        private void EnableDependencies()
        {
            if (nowSelectedMod is null)
                return;
            string err = null!;
            foreach (var dependencie in nowSelectedMod.DependenciesSet!)
            {
                if (allModInfos.ContainsKey(dependencie.Id))
                    ChangeModEnabled(dependencie.Id, true);
                else
                {
                    err ??= $"{I18nRes.NotFoundDependencies}\n";
                    err += $"{dependencie}\n";
                }
            }
            if (err != null)
            {
                Logger.Record(err, LogLevel.WARN);
                MessageBoxVM.Show(new(err) { Icon = MessageBoxVM.Icon.Warning });
            }
            CheckEnabledModsDependencies();
            RefreshGroupModCount();
            IsRemindSave = true;
        }

        [RelayCommand]
        private void ModFilterTextChanged()
        {
            CheckFilterAndRefreshShowMods();
        }

        [RelayCommand]
        internal void Save()
        {
            SaveAllData();
            IsRemindSave = false;
        }

        [RelayCommand]
        private void OpenModDirectory()
        {
            if (Utils.DirectoryExists(GameInfo.ModsDirectory))
                Utils.OpenLink(GameInfo.ModsDirectory);
        }

        [RelayCommand]
        private void OpenBackupDirectory()
        {
            if (!Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);
            Utils.OpenLink(backupDirectory);
        }

        [RelayCommand]
        private void OpenSaveDirectory()
        {
            if (Utils.DirectoryExists(GameInfo.SaveDirectory))
                Utils.OpenLink(GameInfo.SaveDirectory);
        }

        [RelayCommand]
        private void ImportUserData()
        {
            var filesName = OpenFileDialogVM.Show(new()
            {
                Title = I18nRes.ImportUserData,
                Filter = $"Toml {I18nRes.File}|*.toml"
            });
            if (filesName?.FirstOrDefault(defaultValue: null) is string fileName)
            {
                GetUserData(fileName);
                RefreshModsContextMenu();
                RefreshGroupModCount();
            }
        }

        [RelayCommand]
        private void ExportUserData()
        {
            var fileName = SaveFileDialogVM.Show(new()
            {
                Title = I18nRes.ImportUserData,
                Filter = $"Toml {I18nRes.File}|*.toml"
            });
            if (!string.IsNullOrEmpty(fileName))
            {
                SaveUserData(fileName);
            }
        }

        [RelayCommand]
        private void ImportUserGroup()
        {
            var filesName = OpenFileDialogVM.Show(new()
            {
                Title = I18nRes.ImportUserGroup,
                Filter = $"Toml {I18nRes.File}|*.toml"
            });
            if (filesName?.FirstOrDefault(defaultValue: null) is string fileName)
            {
                GetAllUserGroup(fileName);
                RefreshModsContextMenu();
                RefreshGroupModCount();
                IsRemindSave = true;
            }
        }

        [RelayCommand]
        private void ExportUserGroup()
        {
            var fileName = SaveFileDialogVM.Show(new()
            {
                Title = I18nRes.ExportUserGroup,
                Filter = $"Toml {I18nRes.File}|*.toml"
            });
            if (!string.IsNullOrEmpty(fileName))
            {
                SaveAllUserGroup(fileName, ComboBox_ExportUserGroup.SelectedItem!.Tag!.ToString()!);
            }
        }

        [RelayCommand]
        private void ImportEnabledListFromSave()
        {
            var filesName = OpenFileDialogVM.Show(new()
            {
                Title = I18nRes.ImportFromSave,
                Filter = $"Xml {I18nRes.File}|*.xml"
            });
            if (filesName?.FirstOrDefault(defaultValue: null) is not string fileName)
                return;
            string filePath = $"{string.Join("\\", fileName.Split("\\")[..^1])}\\descriptor.xml";
            if (!Utils.FileExists(filePath))
                return;
            string? err = null;
            IEnumerable<string> list = null!;
            try
            {
                XElement xes = XElement.Load(filePath);
                list = xes.Descendants("spec").Where(x => x.Element("id") != null).Select(x => x.Element("id")!.Value);
            }
            catch (Exception ex)
            {
                Logger.Record($"{I18nRes.FileError} {I18nRes.Path}: {filePath}\n", ex);
                MessageBoxVM.Show(new($"{I18nRes.FileError}\n{I18nRes.Path}: {filePath}\n") { Icon = MessageBoxVM.Icon.Question });
                return;
            }
            var result = MessageBoxVM.Show(new(I18nRes.SelectImportMode)
            {
                Button = MessageBoxVM.Button.YesNoCancel,
                Icon = MessageBoxVM.Icon.Question
            });
            if (result == MessageBoxVM.Result.Yes)
                ClearAllEnabledMods();
            else if (result == MessageBoxVM.Result.Cancel)
                return;
            foreach (string id in list)
            {
                if (allModInfos.ContainsKey(id))
                    ChangeModEnabled(id, true);
                else
                {
                    Logger.Record($"{I18nRes.NotFoundMod} {id}", LogLevel.WARN);
                    err ??= $"{I18nRes.NotFoundMod}\n";
                    err += $"{id}\n";
                }
            }
            if (err != null)
            {
                Logger.Record(err, LogLevel.WARN);
                MessageBoxVM.Show(new(err) { Icon = MessageBoxVM.Icon.Warning });
            }
        }

        [RelayCommand]
        private void ImportEnabledList()
        {
            var filesName = OpenFileDialogVM.Show(new()
            {
                Title = I18nRes.ImportEnabledList,
                Filter = $"Json {I18nRes.File}|*.json"
            });
            if (filesName?.FirstOrDefault(defaultValue: null) is string fileName)
            {
                TryGetEnabledMods(fileName, true);
                RefreshGroupModCount();
            }
        }

        [RelayCommand]
        private void ExportEnabledList()
        {
            var fileName = SaveFileDialogVM.Show(new()
            {
                Title = I18nRes.ExportEnabledList,
                Filter = $"Json {I18nRes.File}|*.json"
            });
            if (!string.IsNullOrEmpty(fileName))
            {
                SaveEnabledMods(fileName);
            }
        }

        [RelayCommand(CanExecute = nameof(RandomEnableModsCanExecute))]
        private void RandomEnableMods()
        {
            string groupName = nowSelectedGroupName;
            int minSize = int.Parse(MinRandomSize);
            int maxSize = int.Parse(MaxRandomSize);
            int count = allUserGroups[groupName].Count;
            if (maxSize > count)
            {
                MessageBoxVM.Show(new(I18nRes.RandomNumberCannotGreaterTotal) { Icon = MessageBoxVM.Icon.Warning });
                return;
            }
            else if (minSize > maxSize)
            {
                MessageBoxVM.Show(new(I18nRes.MinRandomNumberCannotBeGreaterMaxRandomNumber) { Icon = MessageBoxVM.Icon.Warning });
                return;
            }
            foreach (var info in allUserGroups[groupName])
                ChangeModEnabled(info, false);
            int needSize = new Random(Guid.NewGuid().GetHashCode()).Next(minSize, maxSize + 1);
            HashSet<int> set = new();
            while (set.Count < needSize)
                set.Add(new Random(Guid.NewGuid().GetHashCode()).Next(0, count));
            foreach (int i in set)
                ChangeModEnabled(allUserGroups[groupName].ElementAt(i));
            CheckEnabledModsDependencies();
            RefreshGroupModCount();
        }

        private bool RandomEnableModsCanExecute() => !string.IsNullOrEmpty(MinRandomSize) && !string.IsNullOrEmpty(MaxRandomSize);

        [RelayCommand]
        private void OpenModPath(string path)
        {
            Utils.OpenLink(path);
        }

        [RelayCommand]
        private void CloseModDetailsButton()
        {
            CloseModDetails();
        }
        #endregion
    }
}