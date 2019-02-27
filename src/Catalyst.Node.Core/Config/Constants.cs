using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Node.Common.Modules;
using Ipfs.Api;

namespace Catalyst.Node.Core.Config
{
    public static class Constants
    {
        public static string ConfigSubFolder => "Config";
        public static string ModulesSubFolder => "Modules";
        public static string ComponentsJsonConfigFile => "components.json";
        public static string SerilogJsonConfigFile => "serilog.json";
        private static string JsonFilePattern => "{0}.json";
        public static string CatalystSubFolder => ".Catalyst";

        public static IEnumerable<string> AllModuleFiles => ModuleNames.All
               .Select(m => Path.Combine(ModulesSubFolder, string.Format(JsonFilePattern, m.ToLower())));

        public static string NetworkConfigFile(NodeOptions.Networks network)
        {
            var networkAsString = Enum.GetName(typeof(NodeOptions.Networks), network);
            return string.Format(JsonFilePattern, networkAsString);
        }
    }
}
