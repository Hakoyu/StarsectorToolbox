using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HKW.Libs.Log4Cs;
using HKW.TOML;
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

    [TomlSortOrder(0)]
    public string Language { get; set; } = "en-US";
    [TomlSortOrder(1)]
    public string LogLevel { get; set; } = HKW.Libs.Log4Cs.LogLevel.INFO.ToString();
    [TomlSortOrder(2)]
    public GameClass Game { get; set; } = null!;
    [TomlSortOrder(3)]
    public ExtensionClass Extension { get; set; } = null!;

    public static void Initialize(string tomlFile)
    {
        SettingsFile = tomlFile;
        Instance = TomlDeserializer.DeserializeFromFile<STSettings>(tomlFile);
    }
    public static void Reset()
    {
        TomlSerializer.SerializeToFile(new STSettings(), SettingsFile);
        Instance = TomlDeserializer.DeserializeFromFile<STSettings>(SettingsFile);
        Logger.Info($"{I18nRes.ConfigFileCreationCompleted} {I18nRes.Path}: {ST.ConfigTomlFile}");
    }

    public static void Save()
    {
        TomlSerializer.SerializeToFile(Instance, SettingsFile);
    }
    public class GameClass : ITomlClassComment
    {
        /// <inheritdoc/>
        public string ClassComment { get; set; } = string.Empty;
        /// <inheritdoc/>
        public Dictionary<string, string> ValueComments { get; set; } = new();

        [TomlSortOrder(0)]
        public string Path { get; set; } = string.Empty;
        [TomlSortOrder(1)]
        public bool ClearLogOnStart { get; set; } = false;
    }

    public class ExtensionClass : ITomlClassComment
    {
        /// <inheritdoc/>
        public string ClassComment { get; set; } = string.Empty;
        /// <inheritdoc/>
        public Dictionary<string, string> ValueComments { get; set; } = new();

        [TomlSortOrder(0)]
        public string DebugPath { get; set; } = string.Empty;
    }
}

