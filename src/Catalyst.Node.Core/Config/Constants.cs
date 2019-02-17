using System;
using System.Collections.Generic;
using System.Text;
using Dawn;

namespace Catalyst.Node.Core.Config
{
    public class Constants
    {
        public const string ConfigFolder = "Config";
        public const string ComponentsJsonConfigFile = "components.json";
        public const string SerilogJsonConfigFile = "serilog.json";
        private const string NetworkConfigFilePattern = "{0}.json";
        public const string CatalystSubFolder = ".Catalyst";

        public static string NetworkConfigFile(NodeOptions.Networks network)
        {
            var networkAsString = Enum.GetName(typeof(NodeOptions.Networks), network);
            return string.Format(NetworkConfigFilePattern, network);
        }

        public static string NetworkConfigFile(string network)
        {
            var networkAsEnum = (NodeOptions.Networks)Enum.Parse(
                typeof(NodeOptions.Networks), network, true);
            
            return string.Format(NetworkConfigFilePattern, networkAsEnum);
        }
    }
}
