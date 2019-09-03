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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Extensions;
using Catalyst.Core.P2P;
using Catalyst.Core.Util;
using Catalyst.Protocol.Deltas;
using Dawn;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Core.Consensus.Deltas
{
    public class DeltaVoter : IDeltaVoter
    {
        public static string GetCandidateCacheKey(CandidateDeltaBroadcast candidate) => 
            nameof(DeltaVoter) + "-" + candidate.Hash.AsBase32Address();

        public static string GetCandidateListCacheKey(CandidateDeltaBroadcast candidate) => 
            nameof(DeltaVoter) + "-" + candidate.PreviousDeltaDfsHash.AsBase32Address();

        public static string GetCandidateListCacheKey(byte[] previousDeltaHash) => 
            nameof(DeltaVoter) + "-" + previousDeltaHash.AsBase32Address();

        /// <summary>
        /// This cache is used to maintain the candidates with their scores, and for each previous delta hash we found,
        /// the list of candidates that we received.
        /// </summary>
        private readonly IMemoryCache _candidatesCache;

        private readonly IDeltaProducersProvider _deltaProducersProvider;
        private readonly IPeerIdentifier _localPeerIdentifier;
        private readonly ILogger _logger;
        private readonly Func<MemoryCacheEntryOptions> _cacheEntryOptions;

        public DeltaVoter(IMemoryCache candidatesCache,
            IDeltaProducersProvider deltaProducersProvider,
            IPeerIdentifier localPeerIdentifier,
            ILogger logger)
        {
            _candidatesCache = candidatesCache;
            _deltaProducersProvider = deltaProducersProvider;
            _localPeerIdentifier = localPeerIdentifier;
            _cacheEntryOptions = () => new MemoryCacheEntryOptions()
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
                var rankingFactor = GetProducerRankFactor(candidate);

                var candidateCacheKey = GetCandidateCacheKey(candidate);
                if (_candidatesCache.TryGetValue<IScoredCandidateDelta>(candidateCacheKey, out var retrievedScoredDelta))
                {
                    retrievedScoredDelta.IncreasePopularity(1);
                    _logger.Debug("Candidate {candidate} increased popularity to {score}", candidate, retrievedScoredDelta.Score);
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
            _candidatesCache.Set(candidateCacheKey, scoredDelta, _cacheEntryOptions());
            _logger.Verbose("Candidate {hash} with previous hash {previousHash} has score {scored}", 
                candidate.Hash.AsBase32Address(), 
                candidate.PreviousDeltaDfsHash.AsBase32Address(),
                scoredDelta.Score);
        }

        private void AddCandidateToPreviousHashLookup(CandidateDeltaBroadcast candidate, string candidateCacheKey)
        {
            var candidatesForPreviousHash =
                _candidatesCache.GetOrCreate(GetCandidateListCacheKey(candidate),
                    c =>
                    {
                        c.SetOptions(_cacheEntryOptions());
                        return new ConcurrentBag<string>();
                    });
            candidatesForPreviousHash.Add(candidateCacheKey);
            _logger.Verbose("Candidates for previous hash {previousHash} are {candidates}",
                candidate.PreviousDeltaDfsHash.AsBase32Address(), candidatesForPreviousHash);
        }

        public bool TryGetFavouriteDelta(byte[] previousDeltaDfsHash, out FavouriteDeltaBroadcast favourite)
        {
            Guard.Argument(previousDeltaDfsHash, nameof(previousDeltaDfsHash)).NotNull().NotEmpty();
            _logger.Debug("Retrieving favourite candidate delta for the successor of delta {0}", 
                previousDeltaDfsHash.AsBase32Address());

            var cacheKey = GetCandidateListCacheKey(previousDeltaDfsHash);
            if (!_candidatesCache.TryGetValue(cacheKey, out ConcurrentBag<string> candidates))
            {
                _logger.Debug("Failed to retrieve any scored candidate with previous delta {0}",
                    previousDeltaDfsHash.AsBase32Address());
                favourite = default;
                return false;
            }

            var bestCandidate = candidates.Select(c => _candidatesCache.Get(c) as IScoredCandidateDelta)
               .Where(c => c != null)
               .OrderByDescending(c => c.Score)
               .ThenBy(c => c.Candidate.Hash.ToByteArray(), ByteUtil.ByteListMinSizeComparer.Default)
               .First().Candidate;

            favourite = new FavouriteDeltaBroadcast
            {
                Candidate = bestCandidate,
                VoterId = _localPeerIdentifier.PeerId
            };

            _logger.Debug("Retrieved favourite candidate delta {candidate} for the successor of delta {previousDelta}",
                bestCandidate.Hash.AsBase32Address(),
                previousDeltaDfsHash.AsBase32Address());

            return true;
        }

        private int GetProducerRankFactor(CandidateDeltaBroadcast candidate)
        {
            var preferredProducers = _deltaProducersProvider
               .GetDeltaProducersFromPreviousDelta(candidate.PreviousDeltaDfsHash.ToByteArray());
            var ranking = preferredProducers.ToList()
               .FindIndex(p => p.PeerId.Equals(candidate.ProducerId));

            var identifier = new PeerIdentifier(candidate.ProducerId);
            _logger.Verbose("ranking for block produced by {producerId} = {ranking}",
                identifier, ranking);

            if (ranking == -1)
            {
                throw new KeyNotFoundException(
                    $"Producer {candidate.ProducerId} " +
                    $"should not be sending candidate deltas with previous hash " +
                    $"{candidate.PreviousDeltaDfsHash.AsBase32Address()}");
            }

            return preferredProducers.Count - ranking;
        }
    }
}
