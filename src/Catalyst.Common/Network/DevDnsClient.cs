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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Network;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Dawn;
using DnsClient;
using DnsClient.Protocol;
using Microsoft.Extensions.Configuration;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Catalyst.Common.Network
{
    public sealed class DevDnsClient : IDns
    {
        private readonly IList<string> _seedServers;
        private readonly IList<string> _dnsQueryAnswerValues;

        public DevDnsClient(IConfigurationRoot configurationRoot)
        {
            _seedServers = ConfigValueParser.GetStringArrValues(configurationRoot, "SeedServers");
            _dnsQueryAnswerValues = configurationRoot.GetSection("QueryAnswerValues").GetChildren().Select(p => p.Value).ToArray();
        }

        public async Task<IList<IDnsQueryResponse>> GetTxtRecords(IList<string> hostnames = null)
        {
            var queries = _seedServers.Select(GetTxtRecords).ToArray();
            var responses = await Task.WhenAll(queries).ConfigureAwait(false);

            return responses.Where(c => c != null).ToList();
        }

        public async Task<IDnsQueryResponse> GetTxtRecords(string hostname = "seed1.catalystnetwork.io")
        {
            var devDnsQueryResponse = new DevDnsQueryResponse
            {
                Answers = DevDnsQueryResponse.BuildDnsResourceRecords(hostname, _dnsQueryAnswerValues.FirstOrDefault())
            };
            return await Task.FromResult<IDnsQueryResponse>(devDnsQueryResponse).ConfigureAwait(false);
        }
        
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="seedServers"></param>
        public IEnumerable<IPeerIdentifier> GetSeedNodesFromDns(IEnumerable<string> seedServers)
        {
            var peers = new List<IPeerIdentifier>();
            var peerChunks = "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839".HexToUTF8String().Split("|");

            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));
            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));
            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));
            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));
            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));

            return peers;
        }
    }
}
