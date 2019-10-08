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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Dawn;
using Ipfs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Serilog;
using CandidateDeltaBroadcast = Catalyst.Protocol.Wire.CandidateDeltaBroadcast;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    public class DeltaVoter : IDeltaVoter
    {
        public static string GetCandidateCacheKey(CandidateDeltaBroadcast candidate) => 
            nameof(DeltaVoter) + "-" + candidate.Hash.ToByteArray().ToBase32();

        public static string GetCandidateListCacheKey(CandidateDeltaBroadcast candidate) => 
            nameof(DeltaVoter) + "-" + candidate.PreviousDeltaDfsHash.ToByteArray().ToBase32();

        public static string GetCandidateListCacheKey(MultiHash previousDeltaHash) => 
            nameof(DeltaVoter) + "-" + previousDeltaHash.ToBase32();

        /// <summary>
        /// This cache is used to maintain the candidates with their scores, and for each previous delta hash we found,
        /// the list of candidates that we received.
        /// </summary>
        private readonly IMemoryCache _candidatesCache;

        private readonly IDeltaProducersProvider _deltaProducersProvider;
        private readonly PeerId _localPeerIdentifier;
        private readonly ILogger _logger;
        private readonly Func<MemoryCacheEntryOptions> _cacheEntryOptions;

        public DeltaVoter(IMemoryCache candidatesCache,
            IDeltaProducersProvider deltaProducersProvider,
            IPeerSettings peerSettings,
            ILogger logger)
        {
            _candidatesCache = candidatesCache;
            _deltaProducersProvider = deltaProducersProvider;
            _localPeerIdentifier = peerSettings.PeerId;
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
                _logger.Error(e, "Failed to Vote on the candidate delta {0}", candidate);
            }
        }

        private void AddCandidateToCandidateHashLookup(CandidateDeltaBroadcast candidate,
            int rankingFactor,
            string candidateCacheKey)
        {
            var scoredDelta = new ScoredCandidateDelta(candidate, 100 * rankingFactor + 1);
            _candidatesCache.Set(candidateCacheKey, scoredDelta, _cacheEntryOptions());
            _logger.Verbose("Candidate {hash} with previous hash {previousHash} has score {scored}", 
                candidate.Hash.ToByteArray().ToBase32(), 
                candidate.PreviousDeltaDfsHash.ToByteArray().ToBase32(),
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
                candidate.PreviousDeltaDfsHash.ToByteArray().ToBase32(), candidatesForPreviousHash);
        }

        public bool TryGetFavouriteDelta(MultiHash previousDeltaDfsHash, out FavouriteDeltaBroadcast favourite)
        {
            Guard.Argument(previousDeltaDfsHash, nameof(previousDeltaDfsHash)).NotNull();
            _logger.Debug("Retrieving favourite candidate delta for the successor of delta {0}", 
                previousDeltaDfsHash);

            var cacheKey = GetCandidateListCacheKey(previousDeltaDfsHash);
            if (!_candidatesCache.TryGetValue(cacheKey, out ConcurrentBag<string> candidates))
            {
                _logger.Debug("Failed to retrieve any scored candidate with previous delta {0}",
                    previousDeltaDfsHash.ToBase32());
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
                VoterId = _localPeerIdentifier
            };

            _logger.Debug("Retrieved favourite candidate delta {candidate} for the successor of delta {previousDelta}",
                bestCandidate.Hash.ToByteArray().ToBase32(),
                previousDeltaDfsHash.ToBase32());

            return true;
        }

        private int GetProducerRankFactor(CandidateDeltaBroadcast candidate)
        {
            var preferredProducers = _deltaProducersProvider
               .GetDeltaProducersFromPreviousDelta(new MultiHash(candidate.PreviousDeltaDfsHash.ToByteArray()));
            var ranking = preferredProducers.ToList()
               .FindIndex(p => p.Equals(candidate.ProducerId));

            var identifier = candidate.ProducerId;
            _logger.Verbose("ranking for block produced by {producerId} = {ranking}",
                identifier, ranking);

            if (ranking == -1)
            {
                throw new KeyNotFoundException(
                    $"Producer {candidate.ProducerId} " +
                    $"should not be sending candidate deltas with previous hash " +
                    $"{candidate.PreviousDeltaDfsHash.ToByteArray().ToBase32()}");
            }

            return preferredProducers.Count - ranking;
        }
    }
}
