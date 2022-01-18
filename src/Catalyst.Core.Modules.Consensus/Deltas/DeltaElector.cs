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
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.P2P.ReputationSystem;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Wire;
using Dawn;
using Lib.P2P;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Nethermind.Core;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc />
    public class DeltaElector : IDeltaElector
    {
        private readonly IMemoryCache _candidatesCache;
        private readonly IDeltaProducersProvider _deltaProducersProvider;
        private readonly IReputationManager _reputationManager;
        private readonly ILogger _logger;
        private readonly Func<MemoryCacheEntryOptions> _cacheEntryOptions;

        public static string GetCandidateListCacheKey(FavouriteDeltaBroadcast candidate)
        {
            return nameof(DeltaElector) + "-" +
                candidate.Candidate.PreviousDeltaDfsHash.ToByteArray().ToCid();
        }

        public string GetCandidateListCacheKey(Cid previousDeltaHash)
        {
            return nameof(DeltaElector) + "-" + previousDeltaHash;
        }

        public DeltaElector(IMemoryCache candidatesCache,
            IDeltaProducersProvider deltaProducersProvider,
            IReputationManager reputationManager,
            ILogger logger)
        {
            _candidatesCache = candidatesCache;
            _deltaProducersProvider = deltaProducersProvider;
            _reputationManager = reputationManager;
            _cacheEntryOptions = () => new MemoryCacheEntryOptions()
               .AddExpirationToken(
                    new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token));

            _logger = logger;
        }

        public void OnCompleted() { _logger.Information("End of {0} stream.", nameof(FavouriteDeltaBroadcast)); }

        public void OnError(Exception error)
        {
            _logger.Error(error, "Error occured in {0} stream.", nameof(FavouriteDeltaBroadcast));
        }

        public void OnNext(FavouriteDeltaBroadcast candidate)
        {
            _logger.Verbose("Favourite candidate delta received {favourite}", candidate);
            try
            {
                Guard.Argument(candidate, nameof(candidate)).NotNull().Require(f => f.IsValid());

                var cid = candidate.Candidate.PreviousDeltaDfsHash.ToByteArray().ToCid();
                Address candidateAddress = new(candidate.Voter.ToByteArray());
                if (!_deltaProducersProvider
                   .GetDeltaProducersFromPreviousDelta(cid)
                   .Any(p => p.Equals(candidateAddress)))
                {
                    ReputationChange reputationChange = new(new Address(candidate.Voter.ToByteArray()), ReputationEventType.VoterIsNotProducer);
                    _reputationManager.OnNext(reputationChange);

                    _logger.Debug(
                        "Voter {voter} is not a producer for this cycle succeeding {deltaHash} and its vote has been discarded.",
                        candidate.Voter, candidate.Candidate.PreviousDeltaDfsHash);
                    return;
                }

                var candidateListKey = GetCandidateListCacheKey(candidate);

                if (_candidatesCache.TryGetValue(candidateListKey,
                    out ConcurrentDictionary<FavouriteDeltaBroadcast, bool> retrieved))
                {
                    retrieved.TryAdd(candidate, default);
                    return;
                }

                _candidatesCache.Set(candidateListKey,
                    new ConcurrentDictionary<FavouriteDeltaBroadcast, bool>(
                        new[] {new KeyValuePair<FavouriteDeltaBroadcast, bool>(candidate, false)},
                        FavouriteByHashAndVoterComparer.Default), _cacheEntryOptions());
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to process favourite delta {0}", candidate);
            }
        }

        /// <inheritdoc />
        public CandidateDeltaBroadcast GetMostPopularCandidateDelta(Cid previousDeltaDfsHash)
        {
            var candidateListCacheKey = GetCandidateListCacheKey(previousDeltaDfsHash);
            if (!_candidatesCache.TryGetValue(candidateListCacheKey,
                out ConcurrentDictionary<FavouriteDeltaBroadcast, bool> retrieved))
            {
                _logger.Debug("Failed to retrieve any favourite candidate with previous delta {0}",
                    previousDeltaDfsHash);
                return null;
            }

            var votesThreshold =
                _deltaProducersProvider.GetDeltaProducersFromPreviousDelta(previousDeltaDfsHash).Count / 3;
            var favourites = retrieved.Keys.GroupBy(k => k.Candidate.Hash)
               .Select(g => new {Favourite = g.First(), TotalVotes = g.Count()})
               .Where(f => f.TotalVotes >= votesThreshold)
               .OrderByDescending(h => h.TotalVotes)
               .ThenByDescending(h => h.Favourite.Candidate.Hash.ToByteArray(),
                    ByteUtil.ByteListMinSizeComparer.Default)
               .ToList();

            _logger.Debug("Found {candidates} popular candidates suitable for confirmation.", favourites.Count);

            return favourites.FirstOrDefault()?.Favourite.Candidate;
        }
    }
}
