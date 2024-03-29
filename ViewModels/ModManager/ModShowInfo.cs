﻿using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.HKWViewModels.Controls;
using StarsectorToolbox.Models.GameInfo;
using StarsectorToolbox.Models.ModInfo;

namespace StarsectorToolbox.ViewModels.ModManager;

/// <summary>模组显示信息</summary>
internal partial class ModShowInfo : ObservableObject, IModInfo
{
    /// <inheritdoc/>
    public string Id { get; private set; }

    /// <inheritdoc/>
    public string Name { get; private set; }

    /// <inheritdoc/>
    public string Author { get; private set; }

    /// <inheritdoc/>
    public string Version { get; private set; }

    /// <inheritdoc/>
    public bool IsUtility { get; private set; }

    /// <inheritdoc/>
    public string Description { get; private set; }

    /// <inheritdoc/>
    public string GameVersion { get; private set; }

    /// <inheritdoc/>
    public string ModPlugin { get; private set; }

    /// <inheritdoc/>
    public string ModDirectory { get; private set; }

    /// <inheritdoc/>
    public bool IsSameToGameVersion => GameVersion == GameInfo.Version;

    /// <inheritdoc/>
    public IReadOnlySet<ModInfo>? DependenciesSet { get; private set; }

    /// <summary>图标资源</summary>
    public BitmapImage? ImageSource { get; set; } = null!;

    /// <inheritdoc/>
    public DateTime LastUpdateTime { get; private set; }

    public string LastUpdateTimeString { get; private set; }

    /// <summary>已启用</summary>
    [ObservableProperty]
    private bool _isEnabled = false;

    /// <summary>已收藏</summary>
    [ObservableProperty]
    private bool _isCollected = false;

    /// <summary>已选中</summary>
    [ObservableProperty]
    private bool _isSelected = false;

    /// <summary>前置信息</summary>
    [ObservableProperty]
    private string _dependenciesMessage = string.Empty;

    /// <summary>缺少前置</summary>
    [ObservableProperty]
    private bool _missDependencies = false;

    /// <summary>缺少前置的信息</summary>
    [ObservableProperty]
    private string _missDependenciesMessage = string.Empty;

    /// <summary>用户描述</summary>
    [ObservableProperty]
    private string _userDescription = string.Empty;

    /// <summary>右键菜单</summary>
    [ObservableProperty]
    private ContextMenuVM _contextMenu = null!;

#if DEBUG
    public ModShowInfo() { }
#endif

    internal ModShowInfo(IModInfo modInfo)
    {
        Id = modInfo.Id;
        Name = modInfo.Name;
        Author = modInfo.Author;
        Version = modInfo.Version;
        IsUtility = modInfo.IsUtility;
        Description = modInfo.Description;
        GameVersion = modInfo.GameVersion;
        ModPlugin = modInfo.ModPlugin;
        ModDirectory = modInfo.ModDirectory;
        DependenciesSet = modInfo.DependenciesSet;
        LastUpdateTime = modInfo.LastUpdateTime;
        LastUpdateTimeString = modInfo.LastUpdateTime.ToShortDateString();
    }
}
