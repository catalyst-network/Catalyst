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
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Ipfs;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc cref="IDeltaCache" />
    /// <inheritdoc cref="IDisposable" />
    public class DeltaCache : IDeltaCache, IDisposable
    {
        private readonly IHashProvider _hashProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly IDeltaDfsReader _dfsReader;
        private readonly ILogger _logger;
        private readonly Func<MemoryCacheEntryOptions> _entryOptions;
        public MultiHash GenesisHash { get; set; }
        public MultiHash GenesisAddress { get; }

        public static string GetLocalDeltaCacheKey(CandidateDeltaBroadcast candidate)
        {
            return nameof(DeltaCache) + "-LocalDelta-" + candidate.Hash;
        }

        public DeltaCache(IHashProvider hashProvider,
            IMemoryCache memoryCache,
            IDeltaDfsReader dfsReader,
            IDeltaCacheChangeTokenProvider changeTokenProvider,
            ILogger logger)
        {
            GenesisHash = _hashProvider.Cast(new Delta().ToByteArray());
            GenesisAddress = GenesisHash.ToBase32();

            var genesisDelta = new Delta {TimeStamp = Timestamp.FromDateTime(DateTime.MinValue.ToUniversalTime())};
            _hashProvider = hashProvider;
            _memoryCache = memoryCache;
            _memoryCache.Set(GenesisAddress, genesisDelta);

            _dfsReader = dfsReader;
            _logger = logger;
            _entryOptions = () => new MemoryCacheEntryOptions()
               .AddExpirationToken(changeTokenProvider.GetChangeToken())
               .RegisterPostEvictionCallback(EvictionCallback);
        }

        private void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            _logger.Debug("Evicted Delta {0} from cache.", key);
        }

        /// <inheritdoc />
        public bool TryGetOrAddConfirmedDelta(MultiHash hash,
            out Delta delta,
            CancellationToken cancellationToken = default)
        {
            //this calls for a TryGetOrCreate IMemoryCache extension function
            if (_memoryCache.TryGetValue(hash, out delta))
            {
                return true;
            }

            if (!_dfsReader.TryReadDeltaFromDfs(hash, out delta, cancellationToken))
            {
                return false;
            }

            _memoryCache.Set(hash, delta, _entryOptions());
            return true;
        }

        public bool TryGetLocalDelta(CandidateDeltaBroadcast candidate, out Delta delta)
        {
            var tryGetLocalDelta = _memoryCache.TryGetValue(GetLocalDeltaCacheKey(candidate), out delta);
            _logger.Verbose("Retrieved full details {delta}", delta?.ToString() ?? "nothing");
            return tryGetLocalDelta;
        }

        public void AddLocalDelta(CandidateDeltaBroadcast localCandidate, Delta delta)
        {
            _logger.Verbose("Adding full details of candidate delta {candidate}", localCandidate);
            _memoryCache.Set(GetLocalDeltaCacheKey(localCandidate), delta, _entryOptions());
        }

        protected virtual void Dispose(bool disposing) { _memoryCache.Dispose(); }

        public void Dispose() { Dispose(true); }
    }
}
