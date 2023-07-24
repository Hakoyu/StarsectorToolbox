using HKW.TOML.Attributes;
using HKW.TOML.Deserializer;
using HKW.TOML.Interfaces;
using HKW.TOML.Serializer;
using StarsectorToolbox.Libs;
using I18nRes = StarsectorToolbox.Langs.Windows.MainWindow.MainWindowI18nRes;

namespace StarsectorToolbox.Models.ST;

internal class STSettings : ITomlClassComment
{
    private static readonly NLog.Logger sr_logger = NLog.LogManager.GetCurrentClassLogger();

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
    public string Theme { get; set; } = "WindowsDefault";

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
        sr_logger.Info(
            $"{I18nRes.ConfigFileCreationCompleted} {I18nRes.Path}: {ST.SettingsTomlFile}"
        );
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
