using System.IO;
using System.Reflection;

namespace StarsectorToolbox.Resources;

internal class ResourceDictionary
{
    private static readonly Assembly sr_assembly = Assembly.GetExecutingAssembly();
    public const string ModTypeGroup_toml = "StarsectorToolbox.Resources.ModTypeGroup.toml";

    public static StreamReader GetResourceStream(string name)
        => new(sr_assembly.GetManifestResourceStream(name)!);
}