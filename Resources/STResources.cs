using System.IO;
using System.Reflection;

namespace StarsectorToolbox.Resources;

internal class STResources
{
    private static readonly Assembly sr_assembly = Assembly.GetExecutingAssembly();
    public const string ModTypeGroup = "StarsectorToolbox.Resources.ModTypeGroup.toml";
    public const string NlogConfig = "StarsectorToolbox.Resources.NLog.config";
    public const string NlogConfigFile = "STCore\\NLog.config";

    public static StreamReader GetResourceStream(string name) =>
        new(sr_assembly.GetManifestResourceStream(name)!);

    public static void ResourceSave(string resourceName, string path)
    {
        using var sr = new StreamReader(sr_assembly.GetManifestResourceStream(resourceName)!);
        using var sw = new StreamWriter(path);
        sr.BaseStream.CopyTo(sw.BaseStream);
    }
}
