#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

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
