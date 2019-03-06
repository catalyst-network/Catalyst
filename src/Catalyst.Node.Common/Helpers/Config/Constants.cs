using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Node.Common.Helpers.Enumerator;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Common.Helpers.Config
{
    public static class Constants
    {
        public static string ConfigSubFolder => "Config";
        public static string ModulesSubFolder => "Modules";
        public static string ComponentsJsonConfigFile => "components.json";
        public static string SerilogJsonConfigFile => "serilog.json";
        private static string JsonFilePattern => "{0}.json";
        public static string CatalystSubFolder => ".Catalyst";

        public static IEnumerable<string> AllModuleFiles =>
            Enumeration.GetAll<ModuleName>()
               .Select(m => Path.Combine(ModulesSubFolder, string.Format(JsonFilePattern, m.Name.ToLower())));

        public static string NetworkConfigFile(Network network) { return string.Format(JsonFilePattern, network.Name); }
    }
}