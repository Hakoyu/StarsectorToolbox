using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HKW.Libs.Log4Cs;
using HKW.TOML;
using HKW.TOML.Attributes;
using HKW.TOML.Deserializer;
using HKW.TOML.Interfaces;
using HKW.TOML.Serializer;
using HKW.ViewModels;
using StarsectorToolbox.Resources;
using I18nRes = StarsectorToolbox.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorToolbox.Models.ST;

internal class STSettings : ITomlClassComment
{
    [TomlIgnore]
    public static STSettings Instance { get; set; } = null!;

    [TomlIgnore]
    public static string SettingsFile { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string ClassComment { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Dictionary<string, string> ValueComments { get; set; } = new();

    [TomlPropertyOrder(0)]
    public string Language { get; set; } = "zh-CN";

    [TomlPropertyOrder(1)]
    public string LogLevel { get; set; } = HKW.Libs.Log4Cs.LogLevel.INFO.ToString();

    [TomlPropertyOrder(2)]
    public GameClass Game { get; set; } = new();

    [TomlPropertyOrder(3)]
    public ExtensionClass Extension { get; set; } = new();

    public static void Initialize(string tomlFile)
    {
        SettingsFile = tomlFile;
        Instance = TomlDeserializer.DeserializeFromFile<STSettings>(tomlFile);
    }

    public static void Reset(string tomlFile)
    {
        SettingsFile = tomlFile;
        TomlSerializer.SerializeToFile(tomlFile, new STSettings());
        Instance = TomlDeserializer.DeserializeFromFile<STSettings>(tomlFile);
        Logger.Info($"{I18nRes.ConfigFileCreationCompleted} {I18nRes.Path}: {ST.SettingsTomlFile}");
    }

    public static void Save()
    {
        TomlSerializer.SerializeToFile(SettingsFile, Instance);
    }

    public class GameClass : ITomlClassComment
    {
        /// <inheritdoc/>
        public string ClassComment { get; set; } = string.Empty;

        /// <inheritdoc/>
        public Dictionary<string, string> ValueComments { get; set; } = new();

        [TomlPropertyOrder(0)]
        public string Path { get; set; } = string.Empty;

        [TomlPropertyOrder(1)]
        public bool ClearLogOnStart { get; set; } = false;
    }

    public class ExtensionClass : ITomlClassComment
    {
        /// <inheritdoc/>
        public string ClassComment { get; set; } = string.Empty;

        /// <inheritdoc/>
        public Dictionary<string, string> ValueComments { get; set; } = new();

        [TomlPropertyOrder(0)]
        public string DebugPath { get; set; } = string.Empty;
    }
}
