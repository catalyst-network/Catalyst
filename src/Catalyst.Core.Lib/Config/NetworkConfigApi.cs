using System.IO;
using System.Threading.Tasks;
using Catalyst.Protocol.Network;

namespace Catalyst.Core.Lib.Config
{
    public class NetworkConfigApi : ConfigApiBase
    {
        public NetworkConfigApi(NetworkType networkType) : base(Constants.NetworkConfigFile(networkType)) {}

        protected override Task OnFileNotExisting()
        {
           throw new FileNotFoundException("Could not find the network configuration file.");
        }
    }
}
