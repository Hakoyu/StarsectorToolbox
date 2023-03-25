using System.IO;
using System.Reflection;

namespace StarsectorToolbox.Resources;

internal class ResourceDictionary
{
    private static Assembly assembly = Assembly.GetExecutingAssembly();
    public const string Config_toml = $"StarsectorToolbox.Resources.Config.toml";
    public const string ModTypeGroup_toml = "StarsectorToolbox.Resources.ModTypeGroup.toml";

    public static StreamReader GetResourceStream(string name)
        => new(assembly.GetManifestResourceStream(name)!);
}