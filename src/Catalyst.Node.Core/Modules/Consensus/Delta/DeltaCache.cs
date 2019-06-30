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
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace Catalyst.Node.Core.Modules.Consensus.Delta
{
    /// <inheritdoc cref="IDeltaCache"/>
    /// <inheritdoc cref="IDisposable"/>
    public class DeltaCache : IDeltaCache, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDeltaDfsReader _dfsReader;
        private readonly ILogger _logger;
        private readonly MemoryCacheEntryOptions _entryOptions;

        private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromMinutes(10);

        public DeltaCache(IMemoryCache memoryCache,
            IDeltaDfsReader dfsReader,
            ILogger logger,
            TimeSpan cacheTtl = default)
        {
            _memoryCache = memoryCache;
            _dfsReader = dfsReader;
            _logger = logger;
            var expirationTtl = cacheTtl == default ? DefaultCacheTtl : cacheTtl;
            _entryOptions = new MemoryCacheEntryOptions();
            _entryOptions.AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(expirationTtl).Token))
               .RegisterPostEvictionCallback(EvictionCallback);
        }

        private void EvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            _logger.Debug("Evicted Delta {0} from cache.",
                Multiformats.Hash.Multihash.Decode(((Protocol.Delta.Delta) value).PreviousDeltaDfsHash.ToByteArray()));
        }

        /// <inheritdoc />
        public bool TryGetDelta(string hash, out Protocol.Delta.Delta delta)
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

            _memoryCache.Set(hash, delta, _entryOptions);
            return true;
        }

        public Protocol.Delta.Delta LatestKnownDelta => throw new NotImplementedException();

        protected virtual void Dispose(bool disposing) { _memoryCache.Dispose(); }

        public void Dispose() { Dispose(true); }
    }
}
