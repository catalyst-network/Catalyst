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
using System.Linq;
using System.Threading;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Util;
using Catalyst.Protocol.Deltas;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Node.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc />
    public class DeltaElector : IDeltaElector
    {
        private readonly IMemoryCache _candidatesCache;
        private readonly ILogger _logger;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;

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

                if (_candidatesCache.TryGetValue(candidateListKey, out ConcurrentDictionary<FavouriteDeltaBroadcast, bool> retrieved))
                {
                    retrieved.TryAdd(candidate, default);
                    return;
                }

                _candidatesCache.Set(candidateListKey,
                    new ConcurrentDictionary<FavouriteDeltaBroadcast, bool>(
                        new[] {new KeyValuePair<FavouriteDeltaBroadcast, bool>(candidate, false)}, FavouriteByHashAndVoterComparer.Default), _cacheEntryOptions);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to process favourite delta {0}", JsonConvert.SerializeObject(candidate));
            }
        }

        /// <inheritdoc />
        public CandidateDeltaBroadcast GetMostPopularCandidateDelta(byte[] previousDeltaDfsHash)
        {
            var candidateListCacheKey = GetCandidateListCacheKey(previousDeltaDfsHash);
            if (!_candidatesCache.TryGetValue(candidateListCacheKey,
                out ConcurrentDictionary<FavouriteDeltaBroadcast, bool> retrieved))
            {
                _logger.Debug("Failed to retrieve any favourite candidate with previous delta {0}",
                    previousDeltaDfsHash.ToHex());
                return null;
            }

            var favourites = retrieved.Keys.GroupBy(k => k.Candidate.Hash)
               .Select(g => new {Favourite = g.First(), TotalVotes = g.Count()})
               .OrderByDescending(h => h.TotalVotes)
               .ThenByDescending(h => h.Favourite.Candidate.Hash.ToByteArray(), 
                    ByteUtil.ByteListMinSizeComparer.Default);

            return favourites.First().Favourite.Candidate;
        }
    }
}
