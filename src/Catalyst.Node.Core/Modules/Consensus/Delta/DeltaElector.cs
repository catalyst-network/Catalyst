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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Catalyst.Common.Protocol;
using Catalyst.Common.Util;
using Catalyst.Protocol.Delta;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Node.Core.Modules.Consensus.Delta
{
    /// <inheritdoc />
    public class DeltaElector : IDeltaElector
    {
        private readonly IMemoryCache _candidatesCache;
        private readonly ILogger _logger;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;

        public static string GetFavouriteCacheKey(FavouriteDeltaBroadcast favourite) =>
            nameof(DeltaElector) + "-" + favourite.Candidate.Hash.ToByteString().ToByteArray().ToHex();

        public static string GetCandidateListCacheKey(FavouriteDeltaBroadcast candidate) =>
            nameof(DeltaElector) + "-" + candidate.Candidate.PreviousDeltaDfsHash.ToByteArray().ToHex();

        public static string GetCandidateListCacheKey(byte[] previousDeltaHash) =>
            nameof(DeltaElector) + "-" + previousDeltaHash.ToHex();
        
        public DeltaElector(IMemoryCache candidatesCache, ILogger logger)
        {
            _candidatesCache = candidatesCache;
            _cacheEntryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token));

            _logger = logger;
        }

        public void OnCompleted()
        {
            _logger.Information("End of {0} stream.", nameof(FavouriteDeltaBroadcast));
        }

        public void OnError(Exception error)
        {
            _logger.Error(error, "Error occured in {0} stream.", nameof(FavouriteDeltaBroadcast));
        }

        public void OnNext(FavouriteDeltaBroadcast candidate)
        {
            try
            {
                Guard.Argument(candidate, nameof(candidate)).NotNull().Require(f => f.IsValid());

                var candidateListKey = GetCandidateListCacheKey(candidate);
                var favouriteKey = GetFavouriteCacheKey(candidate);

                if (_candidatesCache.TryGetValue(candidateListKey, out ConcurrentDictionary<string, bool> retrieved))
                {
                    retrieved.TryAdd(favouriteKey, default);
                }

                _candidatesCache.Set(candidateListKey,
                    new ConcurrentDictionary<string, bool>(
                        new[] {new KeyValuePair<string, bool>(favouriteKey, false)}));
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to process favourite delta {0}", JsonConvert.SerializeObject(candidate));
            }
        }

        public CandidateDeltaBroadcast GetMostPopularCandidateDelta(byte[] previousDeltaDfsHash) { throw new NotImplementedException(); }
    }
}
