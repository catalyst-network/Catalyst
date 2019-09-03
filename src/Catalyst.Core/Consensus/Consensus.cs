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
using System.Reactive.Linq;
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Cycle;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Core.Consensus.Cycle;
using Catalyst.Core.Extensions;
using Multiformats.Hash;
using Serilog;

namespace Catalyst.Core.Consensus
{
    /// <inheritdoc cref="IDisposable"/>
    /// <inheritdoc cref="IConsensus"/>
    public sealed class Consensus : IConsensus, IDisposable
    {
        private readonly IDeltaVoter _deltaVoter;
        private readonly IDeltaElector _deltaElector;
        private IDisposable _constructionProducingSubscription;
        private IDisposable _campaigningProductionSubscription;
        private IDisposable _votingProductionSubscription;

        private readonly ICycleEventsProvider _cycleEventsProvider;
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IDeltaBuilder _deltaBuilder;
        private readonly IDeltaHub _deltaHub;
        private readonly IDeltaCache _deltaCache;
        private readonly ILogger _logger;

        public Consensus(IDeltaBuilder deltaBuilder,
            IDeltaVoter deltaVoter,
            IDeltaElector deltaElector,
            IDeltaCache deltaCache,
            IDeltaHub deltaHub,
            ICycleEventsProvider cycleEventsProvider,
            IDeltaHashProvider deltaHashProvider,
            ILogger logger)
        {
            _deltaVoter = deltaVoter;
            _deltaElector = deltaElector;
            _cycleEventsProvider = cycleEventsProvider;
            _deltaHashProvider = deltaHashProvider;
            _deltaBuilder = deltaBuilder;
            _deltaHub = deltaHub;
            _deltaCache = deltaCache;
            _logger = logger;
            logger.Information("Consensus service initialised.");
        }

        public void StartProducing()
        {
            _constructionProducingSubscription = _cycleEventsProvider.PhaseChanges
               .Where(p => p.Name == PhaseName.Construction && p.Status == PhaseStatus.Producing)
               .Select(p => _deltaBuilder.BuildCandidateDelta(p.PreviousDeltaDfsHash))
               .Subscribe(c =>
                {
                    _deltaVoter.OnNext(c);
                    _deltaHub.BroadcastCandidate(c);
                });

            _campaigningProductionSubscription = _cycleEventsProvider.PhaseChanges
               .Where(p => p.Name == PhaseName.Campaigning && p.Status == PhaseStatus.Producing)
               .Select(p =>
                {
                    _deltaVoter.TryGetFavouriteDelta(p.PreviousDeltaDfsHash, out var favourite);
                    return favourite;
                })
               .Where(f => f != null)
               .Subscribe(f =>
                {
                    _deltaHub.BroadcastFavouriteCandidateDelta(f);
                    _deltaElector.OnNext(f);
                });

            _votingProductionSubscription = _cycleEventsProvider.PhaseChanges
               .Where(p => p.Name == PhaseName.Voting && p.Status == PhaseStatus.Producing)
               .Select(p => _deltaElector.GetMostPopularCandidateDelta(p.PreviousDeltaDfsHash))
               .Where(c => c != null)
               .Select(c =>
                {
                    _deltaCache.TryGetLocalDelta(c, out var delta);
                    return delta;
                })
               .Where(d => d != null)
               .Subscribe(d =>
                {
                    _logger.Information("New Delta following {deltaHash} published", 
                        d.PreviousDeltaDfsHash.AsBase32Address());

                    var newHash = _deltaHub.PublishDeltaToDfsAndBroadcastAddressAsync(d)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
                    var newMultiHash = Multihash.Parse(newHash);
                    var previousHash = Multihash.Cast(d.PreviousDeltaDfsHash.ToByteArray());

                    _deltaHashProvider.TryUpdateLatestHash(previousHash, newMultiHash);
                });
        }
        
        public void Dispose()
        {
            _constructionProducingSubscription?.Dispose();
            _campaigningProductionSubscription?.Dispose();
            _votingProductionSubscription?.Dispose();
        }
    }
}
