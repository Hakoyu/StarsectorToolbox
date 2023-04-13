using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HKW.Libs.Log4Cs;
using HKW.ViewModels;
using HKW.ViewModels.Controls;
using HKW.ViewModels.Dialogs;
using StarsectorToolbox.Libs;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.Models.ModInfo;
using I18nRes = StarsectorToolbox.Langs.Pages.ModManager.ModManagerPageI18nRes;

namespace StarsectorToolbox.ViewModels.ModManager;

internal partial class ModManagerPageViewModel : ObservableObject
{
    [ObservableProperty]
    private AddUserGroupWindowViewModel _addUserGroupWindow = null!;

    partial void OnAddUserGroupWindowChanged(AddUserGroupWindowViewModel value)
    {
        InitializeAddUserGroupWindowViewMode(value);
    }

    #region ObservableProperty

    [ObservableProperty]
    private ObservableI18n<I18nRes> _i18n = ObservableI18n<I18nRes>.Create(new());

    [ObservableProperty]
    private bool _groupMenuIsExpand = false;

    [ObservableProperty]
    private ObservableCollection<ModShowInfo> _nowShowMods = new();

    [ObservableProperty]
    private bool _showSpin = true;

    partial void OnShowSpinChanged(bool value)
    {
        if (NowShowMods.Count is 0)
            _showSpin = false;
    }

    /// <summary>当前选择的模组</summary>
    private List<ModShowInfo> _nowSelectedMods = new();

    /// <summary>当前选择的模组</summary>
    [ObservableProperty]
    private ModShowInfo? _nowSelectedMod = null;

    /// <summary>模组详情的展开状态</summary>
    [ObservableProperty]
    private bool _isShowModDetails = false;

    [ObservableProperty]
    private string _modFilterText = string.Empty;

    partial void OnModFilterTextChanged(string value) => CheckFilterAndRefreshShowMods();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RandomEnableModsCommand))]
    private string _minRandomSize = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RandomEnableModsCommand))]
    private string _maxRandomSize = string.Empty;

    [ObservableProperty]
    private bool _isRemindSave = false;

    [ObservableProperty]
    private bool _nowSelectedIsUserGroup = false;

    #endregion ObservableProperty

    /// <summary>当前选择的列表项</summary>
    private ListBoxItemVM _nowSelectedGroup = null!;

    private string NowSelectedGroupName => _nowSelectedGroup!.Tag!.ToString()!;

    #region ListBox

    [ObservableProperty]
    private ListBoxVM _listBox_MainMenu =
        new()
        {
            new()
            {
                Icon = "🅰",
                Content = I18nRes.AllMods,
                ToolTip = I18nRes.AllMods,
                Tag = ModTypeGroup.All
            },
            new()
            {
                Icon = "✅",
                Content = I18nRes.EnabledMods,
                ToolTip = I18nRes.EnabledMods,
                Tag = ModTypeGroup.Enabled,
            },
            new()
            {
                Icon = "❎",
                Content = I18nRes.DisabledMods,
                ToolTip = I18nRes.DisabledMods,
                Tag = ModTypeGroup.Disabled
            },
        };

    [ObservableProperty]
    private ListBoxVM _listBox_TypeGroupMenu =
        new()
        {
            new()
            {
                Icon = "🔝",
                Content = I18nRes.Libraries,
                ToolTip = I18nRes.Libraries,
                Tag = ModTypeGroup.Libraries
            },
            new()
            {
                Icon = "☢",
                Content = I18nRes.MegaMods,
                ToolTip = I18nRes.MegaMods,
                Tag = ModTypeGroup.MegaMods
            },
            new()
            {
                Icon = "🏁",
                Content = I18nRes.FactionMods,
                ToolTip = I18nRes.FactionMods,
                Tag = ModTypeGroup.FactionMods
            },
            new()
            {
                Icon = "🆙",
                Content = I18nRes.ContentExtensions,
                ToolTip = I18nRes.ContentExtensions,
                Tag = ModTypeGroup.ContentExtensions
            },
            new()
            {
                Icon = "🛠",
                Content = I18nRes.UtilityMods,
                ToolTip = I18nRes.UtilityMods,
                Tag = ModTypeGroup.UtilityMods
            },
            new()
            {
                Icon = "🛄",
                Content = I18nRes.MiscellaneousMods,
                ToolTip = I18nRes.MiscellaneousMods,
                Tag = ModTypeGroup.MiscellaneousMods
            },
            new()
            {
                Icon = "✨",
                Content = I18nRes.BeautifyMods,
                ToolTip = I18nRes.BeautifyMods,
                Tag = ModTypeGroup.BeautifyMods
            },
            new()
            {
                Icon = "🆓",
                Content = I18nRes.UnknownMods,
                ToolTip = I18nRes.UnknownMods,
                Tag = ModTypeGroup.UnknownMods
            },
        };

    [ObservableProperty]
    private ListBoxVM _listBox_UserGroupMenu =
        new()
        {
            new()
            {
                Icon = "🌟",
                Content = I18nRes.CollectedMods,
                ToolTip = I18nRes.CollectedMods,
                Tag = ModTypeGroup.Collected
            },
        };

    #endregion ListBox

    #region ComboBox

    [ObservableProperty]
    private ComboBoxVM _comboBox_ModFilterType =
        new()
        {
            new() { Content = I18nRes.Name, Tag = nameof(ModShowInfo.Name) },
            new() { Content = nameof(ModShowInfo.Id), Tag = nameof(ModShowInfo.Id) },
            new() { Content = I18nRes.Author, Tag = nameof(ModShowInfo.Author) },
            new() { Content = I18nRes.Description, Tag = nameof(ModShowInfo.Description) },
            new() { Content = I18nRes.UserDescription, Tag = nameof(ModShowInfo.UserDescription) },
        };

    [ObservableProperty]
    private ComboBoxVM _comboBox_ExportUserGroup =
        new()
        {
            new() { Content = I18nRes.All, Tag = nameof(I18nRes.All) }
        };

    #endregion ComboBox

    public ModManagerPageViewModel() { }

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
        RefreshGroupModCount(false);
        RefreshModsContextMenuI18n();
    }

    private void ListBox_Menu_SelectionChangedEvent(ListBoxItemVM item)
    {
        if (item is null || _nowSelectedGroup == item)
            return;
        // 若切换选择,可取消原来的选中状态,以此达到多列表互斥
        if (_nowSelectedGroup?.IsSelected is true)
            _nowSelectedGroup.IsSelected = false;
        _nowSelectedGroup = item;
        var group = item.Tag!.ToString()!;
        if (r_allUserGroups.ContainsKey(group))
            NowSelectedIsUserGroup = true;
        else
            NowSelectedIsUserGroup = false;
        ClearSelectedMods(_nowSelectedMods);
        CheckFilterAndRefreshShowMods();
        RefreshAllModsContextMenu();
        if (group == nameof(ModTypeGroup.UnknownMods) && r_allModShowInfoGroups[group].Count > 0)
        {
            MessageBoxVM.Show(
                new(string.Format(I18nRes.UnknownModsMessage, r_allModShowInfoGroups[group].Count))
            );
        }
    }

    #region RelayCommand

    [RelayCommand]
    private void GroupMenuExpand()
    {
        GroupMenuIsExpand = !GroupMenuIsExpand;
    }

    private bool nowClearSelectedMods = false;

    [RelayCommand]
    private void DataGridDoubleClick()
    {
        TryShowModDetails(NowSelectedMod);
    }

    [RelayCommand]
    private void DataGridSelectionChanged(IList items)
    {
        // TODO: 选择操作时会有部分模组没被选中的问题
        if (nowClearSelectedMods)
            return;
        var tempSelectedMods = items.OfType<ModShowInfo>().ToList();
        if (_nowSelectedMods.SequenceEqual(tempSelectedMods))
        {
            NowSelectedMod = null;
            ClearSelectedMods(_nowSelectedMods);
        }
        else
        {
            _nowSelectedMods = tempSelectedMods;
            NowSelectedMod = _nowSelectedMods.LastOrDefault(defaultValue: null!);
            if (IsShowModDetails && NowSelectedMod is not null)
                ShowModDetails(NowSelectedMod);
        }
    }

    //[RelayCommand]
    //private void DataGridLostFocus()
    //{
    //    //ClearSelectedMods(ref _nowSelectedMods);
    //}

    private void ClearSelectedMods(List<ModShowInfo> list)
    {
        if (modDetailsIsMouseOver)
            return;
        nowClearSelectedMods = true;
        foreach (var item in list)
            item.IsSelected = false;
        list.Clear();
        CloseModDetails();
        nowClearSelectedMods = false;
    }

    [RelayCommand]
    private void Collected()
    {
        if (NowSelectedMod is null)
            return;
        ChangeModsCollected(_nowSelectedMods, !NowSelectedMod.IsCollected);
    }

    [RelayCommand]
    private void Enabled()
    {
        if (NowSelectedMod is null)
            return;
        ChangeModsEnabled(_nowSelectedMods, !NowSelectedMod.IsEnabled);
    }

    [RelayCommand]
    private void EnableDependencies()
    {
        if (NowSelectedMod is null)
            return;
        StringBuilder errSB = new();
        foreach (var dependencie in NowSelectedMod.DependenciesSet!)
        {
            if (r_allModInfos.ContainsKey(dependencie.Id) is false)
            {
                errSB.AppendLine(dependencie.ToString());
                continue;
            }
            ChangeModEnabled(dependencie.Id, true);
        }
        if (errSB.Length > 0)
        {
            string err = errSB.ToString();
            Logger.Warring(err);
            MessageBoxVM.Show(new(err) { Icon = MessageBoxVM.Icon.Warning });
        }
        CheckEnabledModsDependencies();
        RefreshGroupModCount();
    }

    [RelayCommand]
    internal void Save()
    {
        SaveAllData();
        IsRemindSave = false;
    }

    [RelayCommand]
    private static void OpenModDirectory()
    {
        if (Utils.DirectoryExists(GameInfo.ModsDirectory))
            Utils.OpenLink(GameInfo.ModsDirectory);
    }

    [RelayCommand]
    private static void OpenBackupDirectory()
    {
        if (Directory.Exists(sr_backupDirectory) is false)
            Directory.CreateDirectory(sr_backupDirectory);
        Utils.OpenLink(sr_backupDirectory);
    }

    [RelayCommand]
    private static void OpenSaveDirectory()
    {
        if (Utils.DirectoryExists(GameInfo.SaveDirectory))
            Utils.OpenLink(GameInfo.SaveDirectory);
    }

    [RelayCommand]
    private void ImportUserData()
    {
        var fileNames = OpenFileDialogVM.Show(
            new() { Title = I18nRes.ImportUserData, Filter = $"Toml {I18nRes.File}|*.toml" }
        );
        if (fileNames?.FirstOrDefault(defaultValue: null) is string fileName)
        {
            GetUserData(fileName);
            RefreshGroupModCount();
        }
    }

    [RelayCommand]
    private void ExportUserData()
    {
        var fileName = SaveFileDialogVM.Show(
            new() { Title = I18nRes.ImportUserData, Filter = $"Toml {I18nRes.File}|*.toml" }
        );
        if (string.IsNullOrEmpty(fileName) is false)
        {
            SaveUserData(fileName);
        }
    }

    [RelayCommand]
    private void ImportUserGroup()
    {
        var fileNames = OpenFileDialogVM.Show(
            new() { Title = I18nRes.ImportUserGroup, Filter = $"Toml {I18nRes.File}|*.toml" }
        );
        if (fileNames?.FirstOrDefault(defaultValue: null) is string fileName)
        {
            GetAllUserGroup(fileName);
            RefreshGroupModCount();
        }
    }

    [RelayCommand]
    private void ExportUserGroup()
    {
        var fileName = SaveFileDialogVM.Show(
            new() { Title = I18nRes.ExportUserGroup, Filter = $"Toml {I18nRes.File}|*.toml" }
        );
        if (string.IsNullOrEmpty(fileName) is false)
        {
            SaveAllUserGroup(fileName, ComboBox_ExportUserGroup.SelectedItem!.Tag!.ToString()!);
        }
    }

    [RelayCommand]
    private void ImportEnabledListFromSave()
    {
        var fileNames = OpenFileDialogVM.Show(
            new() { Title = I18nRes.ImportFromSave, Filter = $"Xml {I18nRes.File}|*.xml" }
        );
        if (fileNames?.FirstOrDefault(defaultValue: null) is not string fileName)
            return;
        string filePath = $"{string.Join("\\", fileName.Split("\\")[..^1])}\\descriptor.xml";
        if (Utils.FileExists(filePath) is false)
            return;
        StringBuilder errSB = new();
        IEnumerable<string> list = null!;
        try
        {
            XElement xes = XElement.Load(filePath);
            list = xes.Descendants("spec")
                .Where(x => x.Element("id") != null)
                .Select(x => x.Element("id")!.Value);
        }
        catch (Exception ex)
        {
            Logger.Error($"{I18nRes.FileError} {I18nRes.Path}: {filePath}\n", ex);
            MessageBoxVM.Show(
                new($"{I18nRes.FileError}\n{I18nRes.Path}: {filePath}\n")
                {
                    Icon = MessageBoxVM.Icon.Question
                }
            );
            return;
        }
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
        foreach (string id in list)
        {
            if (r_allModInfos.ContainsKey(id) is false)
            {
                Logger.Warring($"{I18nRes.NotFoundMod} {id}");
                errSB.AppendLine(id);
                continue;
            }
            ChangeModEnabled(id, true);
        }
        if (errSB.Length > 0)
        {
            Logger.Warring($"{I18nRes.NotFoundMod}\n{errSB}");
            MessageBoxVM.Show(
                new($"{I18nRes.NotFoundMod}\n{errSB}") { Icon = MessageBoxVM.Icon.Warning }
            );
        }
    }

    [RelayCommand]
    private void ImportEnabledList()
    {
        var fileNames = OpenFileDialogVM.Show(
            new() { Title = I18nRes.ImportEnabledList, Filter = $"Json {I18nRes.File}|*.json" }
        );
        if (fileNames?.FirstOrDefault(defaultValue: null) is string fileName)
        {
            TryGetEnabledMods(fileName, true);
            RefreshGroupModCount();
        }
    }

    [RelayCommand]
    private void ExportEnabledList()
    {
        var fileName = SaveFileDialogVM.Show(
            new() { Title = I18nRes.ExportEnabledList, Filter = $"Json {I18nRes.File}|*.json" }
        );
        if (string.IsNullOrEmpty(fileName) is false)
        {
            SaveEnabledMods(fileName);
        }
    }

    [RelayCommand(CanExecute = nameof(RandomEnableModsCanExecute))]
    private void RandomEnableMods()
    {
        string groupName = NowSelectedGroupName;
        int minSize = int.Parse(MinRandomSize);
        int maxSize = int.Parse(MaxRandomSize);
        int count = r_allUserGroups[groupName].Count;
        if (maxSize > count)
        {
            MessageBoxVM.Show(
                new(I18nRes.RandomNumberCannotGreaterTotal) { Icon = MessageBoxVM.Icon.Warning }
            );
            return;
        }
        else if (minSize > maxSize)
        {
            MessageBoxVM.Show(
                new(I18nRes.MinRandomNumberCannotBeGreaterMaxRandomNumber)
                {
                    Icon = MessageBoxVM.Icon.Warning
                }
            );
            return;
        }
        foreach (var id in r_allUserGroups[groupName])
            ChangeModEnabled(id, false);
        int requestSize = new Random(Guid.NewGuid().GetHashCode()).Next(minSize, maxSize + 1);
        var randomList = r_allUserGroups[groupName].OrderBy(
            s => new Random(Guid.NewGuid().GetHashCode()).Next()
        );
        foreach (var id in randomList.Take(requestSize))
            ChangeModEnabled(id, true);
        CheckEnabledModsDependencies();
        RefreshGroupModCount();
    }

    private bool RandomEnableModsCanExecute() =>
        !string.IsNullOrEmpty(MinRandomSize) && !string.IsNullOrEmpty(MaxRandomSize);

    [RelayCommand]
    private static void OpenModPath(string path)
    {
        Utils.OpenLink(path);
    }

    [RelayCommand]
    private void CloseModDetailsButton()
    {
        CloseModDetails();
    }

    private bool modDetailsIsMouseOver = false;

    [RelayCommand]
    private void ModDetailsMouseOver(bool value) => modDetailsIsMouseOver = value;

    [RelayCommand]
    private void AddUserGroup()
    {
        _addUserGroupWindow.ShowDialog();
    }
    #endregion RelayCommand
}
