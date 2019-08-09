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
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Modules.Consensus.Cycle;
using Catalyst.Protocol.Deltas;
using Serilog;

namespace Catalyst.Core.Lib.Modules.Consensus
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
        private readonly IDeltaBuilder _deltaBuilder;
        private readonly IDeltaHub _deltaHub;
        private readonly IDeltaCache _deltaCache;

        public Consensus(IDeltaBuilder deltaBuilder,
            IDeltaVoter deltaVoter,
            IDeltaElector deltaElector,
            IDeltaCache deltaCache,
            IDeltaHub deltaHub,
            IDeltaHashProvider deltaHashProvider,
            ICycleEventsProvider cycleEventsProvider,
            ILogger logger)
        {
            _deltaVoter = deltaVoter;
            _deltaElector = deltaElector;
            _cycleEventsProvider = cycleEventsProvider;
            _deltaBuilder = deltaBuilder;
            _deltaHub = deltaHub;
            _deltaCache = deltaCache;
            logger.Information("Consensus service initialised.");
        }

        public void StartProducing()
        {
            _constructionProducingSubscription = _cycleEventsProvider.PhaseChanges
               .Where(p => p.Name == PhaseName.Construction && p.Status == PhaseStatus.Producing)
               .Select(p => _deltaBuilder.BuildCandidateDelta(p.PreviousDeltaDfsHash))
               .Subscribe(_deltaHub.BroadcastCandidate);

            _campaigningProductionSubscription = _cycleEventsProvider.PhaseChanges
               .Where(p => p.Name == PhaseName.Campaigning && p.Status == PhaseStatus.Producing)
               .Select(p =>
                {
                    _deltaVoter.TryGetFavouriteDelta(p.PreviousDeltaDfsHash, out var favourite);
                    return favourite;
                })
               .Where(f => f != null)
               .Subscribe(_deltaHub.BroadcastFavouriteCandidateDelta);

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
               .SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(d => _deltaHub.PublishDeltaToDfsAsync(d));
        }
        
        public void Dispose()
        {
            _constructionProducingSubscription?.Dispose();
            _campaigningProductionSubscription?.Dispose();
            _votingProductionSubscription?.Dispose();
        }
    }
}
