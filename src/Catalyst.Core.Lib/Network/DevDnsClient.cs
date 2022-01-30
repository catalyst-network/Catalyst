
#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Abstractions.Network;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Peer;
using DnsClient;
using Google.Protobuf;
using MultiFormats;
using SimpleBase;

namespace Catalyst.Core.Lib.Network
{
    public sealed class DevDnsClient : IDns
    {
        private readonly IList<string> _seedServers;
        private readonly IList<string> _dnsQueryAnswerValues;
        private readonly IEnumerable<MultiAddress> _peerIds;

        public DevDnsClient(IPeerSettings peerSettings)
        {
            _seedServers = peerSettings.SeedServers;

            _peerIds = Enumerable.Range(0, 5)
              .Select(i => new MultiAddress($"/ip4/192.168.0.181/tcp/{4000 + i}/ipfs/18n3naE9kBZoVvgYMV6saMZdwu2yu3QMzKa2BDkb5C5pcuhtrH1G9HHbztbbxA8tGmf4"));

            var peerIdsAsStrings = _peerIds.Select(p => p.ToString());
            _dnsQueryAnswerValues = peerIdsAsStrings.ToArray();
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
#pragma warning disable 1998
        public async Task<IEnumerable<MultiAddress>> GetSeedNodesFromDnsAsync(IEnumerable<string> seedServers)
#pragma warning restore 1998
        {
            return _peerIds;
        }
    }
}
