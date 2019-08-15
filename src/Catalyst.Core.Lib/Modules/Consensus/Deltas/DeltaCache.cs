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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Protocol.Deltas;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Core.Lib.Modules.Consensus.Deltas
{
    /// <inheritdoc cref="IDeltaCache"/>
    /// <inheritdoc cref="IDisposable"/>
    public class DeltaCache : IDeltaCache, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDeltaDfsReader _dfsReader;
        private readonly ILogger _logger;
        private readonly Func<MemoryCacheEntryOptions> _entryOptions;

        public static string GetLocalDeltaCacheKey(CandidateDeltaBroadcast candidate) =>
            nameof(DeltaCache) + "-LocalDelta-" + candidate.Hash.AsMultihashBase64UrlString();

        public DeltaCache(IMemoryCache memoryCache,
            IDeltaDfsReader dfsReader,
            IDeltaCacheChangeTokenProvider changeTokenProvider,
            ILogger logger)
        {
            _memoryCache = memoryCache;
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
        public bool TryGetConfirmedDelta(string hash, out Delta delta)
        {
            //this calls for a TryGetOrCreate IMemoryCache extension function
            if (_memoryCache.TryGetValue(hash, out delta))
            {
                return true;
            }

            if (!_dfsReader.TryReadDeltaFromDfs(hash, out delta, CancellationToken.None))
            {
                return false;
            }

            _memoryCache.Set(hash, delta, _entryOptions());
            return true;
        }

        public bool TryGetLocalDelta(CandidateDeltaBroadcast candidate, out Delta delta)
        {
            _logger.Verbose("Trying to retrieve full details of candidate delta {candidate}", candidate);
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
