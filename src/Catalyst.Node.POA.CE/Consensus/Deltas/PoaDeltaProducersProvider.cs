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
using System.Threading;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Consensus.Deltas;
using Catalyst.Core.Extensions;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.Repository;
using Catalyst.Core.Util;
using Dawn;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Node.POA.CE.Consensus.Deltas
{
    public class PoaDeltaProducersProvider : IDeltaProducersProvider
    {
        private static string GetCacheKey(string rawKey) => nameof(PoaDeltaProducersProvider) + "-" + rawKey;

        private readonly ILogger _logger;

        private readonly IMemoryCache _producersByPreviousDelta;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;
        private readonly Peer _selfAsPeer;
        public IMultihashAlgorithm HashAlgorithm { get; }

        /// <inheritdoc />
        public IPeerRepository PeerRepository { get; }

        public PoaDeltaProducersProvider(IPeerRepository peerRepository,
            IPeerIdentifier peerIdentifier,
            IMemoryCache producersByPreviousDelta,
            IMultihashAlgorithm hashAlgorithm,
            ILogger logger)
        {
            _logger = logger;
            _selfAsPeer = new Peer {PeerIdentifier = peerIdentifier};
            PeerRepository = peerRepository;
            HashAlgorithm = hashAlgorithm;
            _cacheEntryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token));
            _producersByPreviousDelta = producersByPreviousDelta;
        }

        public IList<IPeerIdentifier> GetDeltaProducersFromPreviousDelta(byte[] previousDeltaHash)
        {
            Guard.Argument(previousDeltaHash, nameof(previousDeltaHash)).NotNull();

            var previousDeltaHashAsString = previousDeltaHash.AsBase32Address();

            if (_producersByPreviousDelta.TryGetValue(GetCacheKey(previousDeltaHashAsString),
                out IList<IPeerIdentifier> cachedPeerIdsInPriorityOrder))
            {
                _logger.Information("Retrieved favourite delta producers for successor of {0} from cache.", previousDeltaHashAsString);
                return cachedPeerIdsInPriorityOrder;
            }

            _logger.Information("Calculating favourite delta producers for the successor of {0}.", previousDeltaHashAsString);

            var allPeers = PeerRepository.GetAll().Concat(new[] {_selfAsPeer});

            var peerIdsInPriorityOrder = allPeers.Select(p =>
                {
                    var array = p.PeerIdentifier.PeerId.ToByteArray().Concat(previousDeltaHash).ToArray();
                    var ranking = array.ComputeRawHash(HashAlgorithm);
                    return new
                    {
                        p.PeerIdentifier,
                        ranking
                    };
                })
               .OrderBy(h => h.ranking, ByteUtil.ByteListMinSizeComparer.Default)
               .Select(h => h.PeerIdentifier)
               .ToList();

            _logger.Information("Adding favourite delta producers for the successor of {0} to cache.", previousDeltaHashAsString);
            _logger.Debug("Favourite producers are, in that order, [{0}]", string.Join(", ", peerIdsInPriorityOrder));
            _producersByPreviousDelta.Set(GetCacheKey(previousDeltaHashAsString), peerIdsInPriorityOrder, _cacheEntryOptions);

            return peerIdsInPriorityOrder;
        }
    }
}
