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

using System;
using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Delta;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Core.Modules.Consensus
{
    public class PoaDeltaProducersProvider : IDeltaProducersProvider
    {
        private readonly ILogger _logger;

        /// <inheritdoc />
        public IPeerDiscovery PeerDiscovery { get; }

        public PoaDeltaProducersProvider(IPeerDiscovery peerDiscovery, ILogger logger)
        {
            _logger = logger;
            PeerDiscovery = peerDiscovery;
        }

        public IList<IPeerIdentifier> GetDeltaProducersFromPreviousDelta(Delta previousDelta)
        {
            var allPeers = PeerDiscovery.PeerRepository.GetAll();

            var previousDeltaHash = previousDelta.MerkleRoot.ToByteArray();

            var peerIdsInPriorityOrder = allPeers.Select(p =>
                {
                    var ranking = TEMP_HASH_FUNCTION(p.PeerIdentifier.PeerId.ToByteArray(), previousDeltaHash);
                    return new {PeerIdenitifier = p.PeerIdentifier, Ranking = ranking.ToArray()};
                })
               .OrderBy(h => h.Ranking, ByteListComparer.Default)
               .Select(h => h.PeerIdenitifier)
               .ToList();

            return peerIdsInPriorityOrder;
        }

        //Todo replace with the true one from Crypto package when available
        public byte[] TEMP_HASH_FUNCTION(params byte[][] bytes)
        {
            return bytes.SelectMany(t => t).ToArray();
        }
    }
}
