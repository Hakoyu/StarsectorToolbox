using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StarsectorTools.Resources
{
    internal class ResourceDictionary
    {
        private static Assembly assembly = Assembly.GetExecutingAssembly();
        public const string Config_toml = $"StarsectorTools.Resources.Config.toml";
        public const string ModTypeGroup_toml = "StarsectorTools.Resources.ModTypeGroup.toml";
        public static StreamReader GetResourceStream(string name)
            => new(assembly.GetManifestResourceStream(name)!);
    }
}
