
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
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.Network;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.P2P;
using DnsClient;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Catalyst.Core.Network
{
    public sealed class DevDnsClient : IDns
    {
        private readonly IList<string> _seedServers;
        private readonly IList<string> _dnsQueryAnswerValues;

        public DevDnsClient(IPeerSettings peerSettings)
        {
            _seedServers = peerSettings.SeedServers;
            _dnsQueryAnswerValues = new[]
            {
                "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839",
                "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839",
                "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839",
                "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839",
                "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839"
            };
        }

        public async Task<IList<IDnsQueryResponse>> GetTxtRecordsAsync(IList<string> hostnames = null)
        {
            var queries = _seedServers.Select(GetTxtRecordsAsync).ToArray();
            var responses = await Task.WhenAll(queries).ConfigureAwait(false);

            return responses.Where(c => c != null).ToList();
        }

        public async Task<IDnsQueryResponse> GetTxtRecordsAsync(string hostname = "seed1.catalystnetwork.io")
        {
            var devDnsQueryResponse = new DevDnsQueryResponse
            {
                Answers = DevDnsQueryResponse.BuildDnsResourceRecords(hostname, _dnsQueryAnswerValues.FirstOrDefault())
            };
            return await Task.FromResult<IDnsQueryResponse>(devDnsQueryResponse).ConfigureAwait(false);
        }
        
        /// <inheritdoc />
        public IEnumerable<IPeerIdentifier> GetSeedNodesFromDns(IEnumerable<string> seedServers)
        {
            var peers = new List<IPeerIdentifier>();
            var peerChunks = "0x41437c30317c39322e3230372e3137382e3139387c34323036397c3031323334353637383930313233343536373839323232323232323232323232".HexToUTF8String().Split(PeerIdentifier.PidDelimiter);

            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));
            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));
            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));
            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));
            peers.Add(PeerIdentifier.ParseHexPeerIdentifier(peerChunks));

            return peers;
        }
    }
}
