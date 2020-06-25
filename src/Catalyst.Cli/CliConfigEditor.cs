using System.Collections.Generic;
using Catalyst.Core.Lib.Config;
using Catalyst.Protocol.Network;

namespace Catalyst.Cli
{
    public class CliConfigEditor : ConfigEditor
    {
        protected override Dictionary<string, List<KeyValuePair<string, string>>> RequiredConfigFileEdits(NetworkType network)
        {
            return null;
                
        }
    }
}
