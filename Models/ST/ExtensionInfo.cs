using System;
using System.Collections.Generic;
using HKW.TOML;
using HKW.TOML.Attributes;
using HKW.TOML.Interfaces;

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
    [TomlPropertyOrder(0)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 图标
    /// </summary>
    [TomlPropertyOrder(1)]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [TomlPropertyOrder(2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 作者
    /// </summary>
    [TomlPropertyOrder(3)]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// 版本
    /// </summary>
    [TomlPropertyOrder(4)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 支持的工具箱版本
    /// </summary>
    [TomlPropertyOrder(5)]
    public string ToolboxVersion { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [TomlPropertyOrder(6)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 入口文件
    /// </summary>
    [TomlPropertyOrder(7)]
    public string ExtensionFile { get; set; } = string.Empty;

    /// <summary>
    /// 入口
    /// </summary>
    [TomlPropertyOrder(8)]
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
