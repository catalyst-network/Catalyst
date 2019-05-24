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
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Util;
using Catalyst.Protocol.Delta;
using Google.Protobuf;
using Multiformats.Hash.Algorithms;
using Serilog;
using SharpRepository.Repository;
using Peer = Catalyst.Common.P2P.Peer;

namespace Catalyst.Node.Core.Modules.Consensus
{
    public class PoaDeltaProducersProvider : IDeltaProducersProvider
    {
        private readonly ILogger _logger;
        public IMultihashAlgorithm HashAlgorithm { get; }

        /// <inheritdoc />w
        public IRepository<Peer> PeerRepository { get; }

        public PoaDeltaProducersProvider(IRepository<Peer> peerRepository,
            ILogger logger)
        {
            _logger = logger;
            PeerRepository = peerRepository;
            HashAlgorithm = Common.Config.Constants.HashAlgorithm;
        }

        public IList<IPeerIdentifier> GetDeltaProducersFromPreviousDelta(byte[] previousDeltaHash)
        {
            var allPeers = PeerRepository.GetAll();

            var peerIdsInPriorityOrder = allPeers.Select(p =>
                {
                    var array = p.PeerIdentifier.PeerId.ToByteArray().Concat(previousDeltaHash).ToArray();
                    var ranking = HashAlgorithm.ComputeHash(array);
                    return new
                    {
                        p.PeerIdentifier,
                        ranking
                    };
                })
               .OrderBy(h => h.ranking, ByteUtil.ByteListMinSizeComparer.Default)
               .Select(h => h.PeerIdentifier)
               .ToList();

            return peerIdsInPriorityOrder;
        }
    }
}
