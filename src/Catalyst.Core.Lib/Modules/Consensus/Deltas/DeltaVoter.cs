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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Util;
using Catalyst.Protocol.Deltas;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Core.Lib.Modules.Consensus.Deltas
{
    public class DeltaVoter : IDeltaVoter
    {
        public static string GetCandidateCacheKey(CandidateDeltaBroadcast candidate) => 
            nameof(DeltaVoter) + "-" + candidate.Hash.AsMultihashBase64UrlString();

        public static string GetCandidateListCacheKey(CandidateDeltaBroadcast candidate) => 
            nameof(DeltaVoter) + "-" + candidate.PreviousDeltaDfsHash.AsMultihashBase64UrlString();

        public static string GetCandidateListCacheKey(byte[] previousDeltaHash) => 
            nameof(DeltaVoter) + "-" + previousDeltaHash.AsMultihashBase64UrlString();

        /// <summary>
        /// This cache is used to maintain the candidates with their scores, and for each previous delta hash we found,
        /// the list of candidates that we received.
        /// </summary>
        private readonly IMemoryCache _candidatesCache;

        private readonly IDeltaProducersProvider _deltaProducersProvider;
        private readonly ILogger _logger;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;

        public DeltaVoter(IMemoryCache candidatesCache,
            IDeltaProducersProvider deltaProducersProvider,
            ILogger logger)
        {
            _candidatesCache = candidatesCache;
            _deltaProducersProvider = deltaProducersProvider;
            _cacheEntryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(3)).Token));
            _logger = logger;
        }

        public void OnCompleted()
        {
            _logger.Information("End of {0} stream.", nameof(CandidateDeltaBroadcast));
        }

        public void OnError(Exception error)
        {
            _logger.Error(error, "Error occured in {0} stream.", nameof(CandidateDeltaBroadcast));
        }

        public void OnNext(CandidateDeltaBroadcast candidate)
        {
            try
            {
                Guard.Argument(candidate, nameof(candidate)).NotNull().Require(c => c.IsValid());

                var rankingFactor = GetProducerRankFactor(candidate);

                var candidateCacheKey = GetCandidateCacheKey(candidate);
                if (_candidatesCache.TryGetValue<IScoredCandidateDelta>(candidateCacheKey, out var retrievedScoredDelta))
                {
                    retrievedScoredDelta.IncreasePopularity(1);
                    return;
                }
                
                AddCandidateToCandidateHashLookup(candidate, rankingFactor, candidateCacheKey);
                AddCandidateToPreviousHashLookup(candidate, candidateCacheKey);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to Vote on the candidate delta {0}", JsonConvert.SerializeObject(candidate));
            }
        }

        private void AddCandidateToCandidateHashLookup(CandidateDeltaBroadcast candidate,
            int rankingFactor,
            string candidateCacheKey)
        {
            var scoredDelta = new ScoredCandidateDelta(candidate, 100 * rankingFactor + 1);
            _candidatesCache.Set(candidateCacheKey, scoredDelta, _cacheEntryOptions);
        }

        private void AddCandidateToPreviousHashLookup(CandidateDeltaBroadcast candidate, string candidateCacheKey)
        {
            var candidatesByPreviousHash =
                _candidatesCache.GetOrCreate(GetCandidateListCacheKey(candidate),
                    c =>
                    {
                        c.SetOptions(_cacheEntryOptions);
                        return new ConcurrentBag<string>();
                    });
            candidatesByPreviousHash.Add(candidateCacheKey);
        }

        public bool TryGetFavouriteDelta(byte[] previousDeltaDfsHash, out CandidateDeltaBroadcast favourite)
        {
            Guard.Argument(previousDeltaDfsHash, nameof(previousDeltaDfsHash)).NotNull().NotEmpty();
            Log.Debug("Retrieving favourite candidate delta for the successor of delta {0}", 
                previousDeltaDfsHash.AsMultihashBase64UrlString());

            var cacheKey = GetCandidateListCacheKey(previousDeltaDfsHash);
            if (!_candidatesCache.TryGetValue(cacheKey, out ConcurrentBag<string> candidates))
            {
                _logger.Debug("Failed to retrieve any scored candidate with previous delta {0}",
                    previousDeltaDfsHash.AsMultihashBase64UrlString());
                favourite = default;
                return false;
            }

            favourite = candidates.Select(c => _candidatesCache.Get(c) as IScoredCandidateDelta)
               .Where(c => c != null)
               .OrderByDescending(c => c.Score)
               .ThenBy(c => c.Candidate.Hash.ToByteArray(), ByteUtil.ByteListMinSizeComparer.Default)
               .First().Candidate;

            return true;
        }

        private int GetProducerRankFactor(CandidateDeltaBroadcast candidate)
        {
            var preferredProducers = _deltaProducersProvider
               .GetDeltaProducersFromPreviousDelta(candidate.PreviousDeltaDfsHash.ToByteArray());
            var ranking = preferredProducers.ToList()
               .FindIndex(p => p.PeerId.Equals(candidate.ProducerId));

            if (ranking == -1)
            {
                throw new KeyNotFoundException(
                    $"Producer {candidate.ProducerId} " +
                    $"should not be sending candidate deltas with previous hash " +
                    $"{candidate.PreviousDeltaDfsHash.AsMultihashBase64UrlString()}");
            }

            return preferredProducers.Count - ranking;
        }
    }
}
