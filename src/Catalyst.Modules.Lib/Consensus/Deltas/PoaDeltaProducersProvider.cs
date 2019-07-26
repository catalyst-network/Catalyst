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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Util;
using Dawn;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Multiformats.Hash.Algorithms;
using Nethereum.Hex.HexConvertors.Extensions;
using Serilog;

namespace Catalyst.Modules.Lib.Consensus.Deltas
{
    public class PoaDeltaProducersProvider : IDeltaProducersProvider
    {
        private static string GetCacheKey(string rawKey) => nameof(PoaDeltaProducersProvider) + "-" + rawKey;

        private readonly ILogger _logger;

        private readonly IMemoryCache _producersByPreviousDelta;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;
        public IMultihashAlgorithm HashAlgorithm { get; }

        /// <inheritdoc />
        public IPeerRepository PeerRepository { get; }

        public PoaDeltaProducersProvider(IPeerRepository peerRepository,
            IMemoryCache producersByPreviousDelta,
            IMultihashAlgorithm hashAlgorithm,
            ILogger logger)
        {
            _logger = logger;
            PeerRepository = peerRepository;
            HashAlgorithm = hashAlgorithm;
            _cacheEntryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token));
            _producersByPreviousDelta = producersByPreviousDelta;
        }

        public IList<IPeerIdentifier> GetDeltaProducersFromPreviousDelta(byte[] previousDeltaHash)
        {
            Guard.Argument(previousDeltaHash, nameof(previousDeltaHash)).NotNull();

            var previousDeltaHashAsHex = previousDeltaHash.ToHex();

            if (_producersByPreviousDelta.TryGetValue(GetCacheKey(previousDeltaHashAsHex),
                out IList<IPeerIdentifier> cachedPeerIdsInPriorityOrder))
            {
                _logger.Information("Retrieved favourite delta producers for successor of {0} from cache.", previousDeltaHashAsHex);
                return cachedPeerIdsInPriorityOrder;
            }

            _logger.Information("Calculating favourite delta producers for the successor of {0}.", previousDeltaHashAsHex);

            var allPeers = PeerRepository.GetAll();

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

            _logger.Information("Adding favourite delta producers for the successor of {0} to cache.", previousDeltaHashAsHex);
            _producersByPreviousDelta.Set(GetCacheKey(previousDeltaHashAsHex), peerIdsInPriorityOrder, _cacheEntryOptions);

            return peerIdsInPriorityOrder;
        }
    }
}
