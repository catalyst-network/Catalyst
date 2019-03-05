using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Node.Common.Helpers;
using Catalyst.Node.Common.Modules;
using Catalyst.Node.Common.P2P;

namespace Catalyst.Node.Common.Config
{
    public static class Constants
    {
        public static string ConfigSubFolder => "Config";
        public static string ModulesSubFolder => "Modules";
        public static string ComponentsJsonConfigFile => "components.json";
        public static string SerilogJsonConfigFile => "serilog.json";
        private static string JsonFilePattern => "{0}.json";
        public static string CatalystSubFolder => ".Catalyst";

        public static IEnumerable<string> AllModuleFiles => Enumeration.GetAll<ModuleName>()
               .Select(m => Path.Combine(ModulesSubFolder, string.Format(JsonFilePattern, (object) m.Name.ToLower())));

        public static string NetworkConfigFile(Network network)
        {
            return string.Format(JsonFilePattern, network.Name);
        }
    }
}
