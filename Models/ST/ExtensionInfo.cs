using System;
using System.Collections.Generic;
using HKW.TOML;

namespace StarsectorToolbox.Models.ST;

/// <summary>拓展信息</summary>
internal class ExtensionInfo : ITomlClassComment
{
    /// <inheritdoc/>
    public string ClassComment { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Dictionary<string, string> ValueComments { get; set; } = new();

    /// <summary>
    /// ID
    /// </summary>
    [TomlSortOrder(0)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    [TomlSortOrder(1)]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [TomlSortOrder(2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    [TomlSortOrder(3)]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    [TomlSortOrder(4)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 支持的工具箱版本
    /// </summary>
    [TomlSortOrder(5)]
    public string ToolboxVersion { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [TomlSortOrder(6)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 入口文件
    /// </summary>
    [TomlSortOrder(7)]
    public string ExtensionFile { get; set; } = string.Empty;

    /// <summary>
    /// 入口
    /// </summary>
    [TomlSortOrder(8)]
    public string ExtensionPublic { get; set; } = string.Empty;

    /// <summary>源位置</summary>
    [TomlIgnore]
    public string FileFullName { get; set; } = null!;

    /// <summary>拓展类型</summary>
    [TomlIgnore]
    public Type ExtensionType { get; set; } = null!;

    /// <summary>拓展页面</summary>
    [TomlIgnore]
    public object ExtensionPage { get; set; } = null!;
}
