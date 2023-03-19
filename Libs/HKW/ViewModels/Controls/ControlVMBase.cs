using System.Collections.Generic;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HKW.ViewModels.Controls;

/// <summary>
/// 基础控件模型
/// </summary>
[DebuggerDisplay("{Name}, Count = {TagDictionary.Count}")]
public partial class ControlVMBase : ObservableObject
{
    /// <summary>
    /// Id
    /// </summary>
    [ObservableProperty]
    private string? id;

    /// <summary>
    /// 名称
    /// </summary>
    [ObservableProperty]
    private string? name;

    /// <summary>
    /// 标签
    /// </summary>
    [ObservableProperty]
    private object? tag;

    /// <summary>
    /// 提示
    /// </summary>
    [ObservableProperty]
    private object? toolTip;

    /// <summary>
    /// 上下文菜单
    /// </summary>
    [ObservableProperty]
    private ContextMenuVM? contextMenu;

    /// <summary>
    /// 数据字典
    /// </summary>
    public Dictionary<string, object?>? DataDictionary { get; set; }
}