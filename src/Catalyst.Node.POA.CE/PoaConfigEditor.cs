using System.Collections.Generic;
using System.Threading.Tasks;
using Catalyst.Core.Lib.Config;
using Catalyst.NetworkUtils;
using Catalyst.Protocol.Network;

namespace Catalyst.Node.POA.CE
{
    public class PoaConfigEditor : ConfigEditor
    {
        private readonly IAddressProvider _addressProvider;
        public PoaConfigEditor(IAddressProvider addressProvider)
        {
            _addressProvider = addressProvider;
        }
        protected override Dictionary<string, List<KeyValuePair<string, string>>> RequiredConfigFileEdits(NetworkType network)
        {
            var publicIp = _addressProvider.GetPublicIpAsync();
            var localIp = _addressProvider.GetLocalIpAsync();
            Task.WaitAll(new Task[] {publicIp, localIp});
            
            var requiredEdits = new List<KeyValuePair<string, string>>();

            if (publicIp.IsCompletedSuccessfully && publicIp.Result!=null)
            {
                requiredEdits.Add(new KeyValuePair<string,string>("CatalystNodeConfiguration.Peer.PublicIpAddress", publicIp.Result.ToString()));
            }
            
            if (localIp.IsCompletedSuccessfully && localIp.Result!=null)
            {
                requiredEdits.Add(new KeyValuePair<string,string>("CatalystNodeConfiguration.Peer.BindAddress", localIp.Result.ToString()));
                requiredEdits.Add(new KeyValuePair<string,string>("CatalystNodeConfiguration.Rpc.BindAddress", localIp.Result.ToString()));
            }
            
            return new Dictionary<string, List<KeyValuePair<string, string>>>
            { 
                {
                    Constants.NetworkConfigFile(network),
                    requiredEdits
                }
            };
        }
    }
}
