using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using HKW.HKWViewModels.Controls;

namespace StarsectorToolbox.Models;

/// <summary>
/// 模组类型分组项
/// </summary>
public partial class ModTypeGroupItem : ObservableObject
{
    /// <summary>
    /// 名称
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Show))]
    private string? _name;

    /// <summary>
    /// 数量
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Show))]
    [NotifyPropertyChangedFor(nameof(ShowCount))]
    private int _count;

    /// <summary>
    /// 显示所有信息
    /// </summary>
    public string? Show => $"{Name} ({Count})";

    /// <summary>
    /// 显示数量信息
    /// </summary>
    public string? ShowCount => $"({Count})";
}
