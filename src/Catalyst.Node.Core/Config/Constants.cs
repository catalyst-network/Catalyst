using System;
using System.Collections.Generic;
using System.Text;
using Dawn;

namespace Catalyst.Node.Core.Config
{
    public static class Constants
    {
        public static string ConfigFolder => "Config";
        public static string ComponentsJsonConfigFile => "components.json";
        public static string SerilogJsonConfigFile => "serilog.json";
        private static string NetworkConfigFilePattern => "{0}.json";
        public static string CatalystSubFolder => ".Catalyst";

        public static string NetworkConfigFile(NodeOptions.Networks network)
        {
            var networkAsString = Enum.GetName(typeof(NodeOptions.Networks), network);
            return string.Format(NetworkConfigFilePattern, networkAsString);
        }

        public static string NetworkConfigFile(string network)
        {
            var networkAsEnum = (NodeOptions.Networks)Enum.Parse(
                typeof(NodeOptions.Networks), network, true);
            
            return string.Format(NetworkConfigFilePattern, networkAsEnum);
        }
    }
}
