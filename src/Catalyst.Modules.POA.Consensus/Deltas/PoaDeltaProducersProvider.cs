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
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Core.Modules.Kvm;
using Dawn;
using Lib.P2P;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Nethermind.Core;
using Serilog;
using Peer = Catalyst.Core.Lib.P2P.Models.Peer;

namespace Catalyst.Modules.POA.Consensus.Deltas
{
    public class PoaDeltaProducersProvider : IDeltaProducersProvider
    {
        private static string GetCacheKey(string rawKey) { return nameof(PoaDeltaProducersProvider) + "-" + rawKey; }

        private readonly ILogger _logger;
        private readonly IMemoryCache _producersByPreviousDelta;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;
        private readonly Peer _selfAsPeer;
        private readonly IHashProvider _hashProvider;
        private readonly IValidatorSetStore _validatorSetStore;

        /// <inheritdoc />
        public IPeerRepository PeerRepository { get; }

        public PoaDeltaProducersProvider(IPeerSettings peerSettings,
            IMemoryCache producersByPreviousDelta,
            IHashProvider hashProvider,
            IValidatorSetStore validatorSetStore,
            ILogger logger)
        {
            _logger = logger;
            _selfAsPeer = new Peer { Address = peerSettings.Address };
            _hashProvider = hashProvider;
            _validatorSetStore = validatorSetStore;
            _cacheEntryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(
                    new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token));
            _producersByPreviousDelta = producersByPreviousDelta;

        }

        public IList<Address> GetDeltaProducersFromPreviousDelta(Cid previousDeltaHash)
        {
            Guard.Argument(previousDeltaHash, nameof(previousDeltaHash)).NotNull();

            if (_producersByPreviousDelta.TryGetValue(GetCacheKey(previousDeltaHash),
                out IList<Address> cachedPeerIdsInPriorityOrder))
            {
                _logger.Information("Retrieved favourite delta producers for successor of {0} from cache.",
                    previousDeltaHash);
                return cachedPeerIdsInPriorityOrder;
            }

            _logger.Information("Calculating favourite delta producers for the successor of {0}.",
                previousDeltaHash);

            var validators = _validatorSetStore.Get(0).GetValidators();

            var previous = previousDeltaHash.ToArray();

            var peerAddressesInPriorityOrder = validators.Select(address =>
                {
                    var ranking = _hashProvider.ComputeMultiHash(address.Bytes, previous).ToArray();
                    return new
                    {
                        address,
                        ranking
                    };
                })
               .OrderBy(h => h.ranking, ByteUtil.ByteListMinSizeComparer.Default)
               .Select(h => h.address)
               .ToList();

            _logger.Information("Adding favourite delta producers for the successor of {0} to cache.",
                previousDeltaHash);
            _logger.Debug("Favourite producers are, in that order, [{0}]", string.Join(", ", peerAddressesInPriorityOrder));
            _producersByPreviousDelta.Set(GetCacheKey(previousDeltaHash), peerAddressesInPriorityOrder,
                _cacheEntryOptions);

            return peerAddressesInPriorityOrder;
        }
    }
}
