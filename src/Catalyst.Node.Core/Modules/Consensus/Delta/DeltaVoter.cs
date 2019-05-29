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
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Catalyst.Protocol.Delta;
using Dawn;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Node.Core.Modules.Consensus.Delta
{
    public class DeltaVoter : IDeltaVoter
    {
        public static string GetCacheKey(string rawKey) => nameof(DeltaVoter) + "-" + rawKey;

        private readonly IMemoryCache _scoredDeltasByHash;
        private readonly IDeltaProducersProvider _deltaProducersProvider;
        private readonly ILogger _logger;
        private readonly MemoryCacheEntryOptions _cacheEntryOptions;

        public DeltaVoter(IMemoryCache scoredDeltasByPreviousHash,
            IDeltaProducersProvider deltaProducersProvider,
            ILogger logger)
        {
            _scoredDeltasByHash = scoredDeltasByPreviousHash;
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
                Guard.Argument(candidate, nameof(candidate)).NotNull()
                   .Require(c => c.ProducerId != null, c => $"{nameof(candidate.ProducerId)} cannot be null")
                   .Require(c => c.PreviousDeltaDfsHash != null && !c.PreviousDeltaDfsHash.IsEmpty,
                        c => $"{nameof(candidate.PreviousDeltaDfsHash)} cannot be null or empty")
                   .Require(c => c.Hash != null && !c.Hash.IsEmpty, 
                        c => $"{nameof(candidate.Hash)} cannot be null or empty");

                var rankingFactor = GetProducerRankFactor(candidate);

                var cacheKey = GetCacheKey(candidate.Hash.ToByteArray().ToHex());
                if (_scoredDeltasByHash.TryGetValue<IScoredCandidateDelta>(cacheKey, out var retrievedScoredDelta))
                {
                    retrievedScoredDelta.IncreasePopularity(1);
                    return;
                }

                var scoredDelta = new ScoredCandidateDelta(candidate, 100 * rankingFactor + 1);

                _scoredDeltasByHash.Set(cacheKey, scoredDelta, _cacheEntryOptions);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to Vote on the candidate delta {0}", JsonConvert.SerializeObject(candidate));
            }
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
                    $"{candidate.PreviousDeltaDfsHash.ToByteArray().ToHex()}");
            }

            return preferredProducers.Count - ranking;
        }
    }
}
