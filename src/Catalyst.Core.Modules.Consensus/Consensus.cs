#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Consensus.Cycle;
using Serilog;
using Catalyst.Core.Lib.Service;
using System.Threading.Tasks;

namespace Catalyst.Core.Modules.Consensus
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
            logger.Information("Consensus repository initialised.");
        }

        public void StartProducing()
        {
            Task.Delay(_cycleEventsProvider.GetTimeSpanUntilNextCycleStart()).Wait();

            _constructionProducingSubscription = _cycleEventsProvider.PhaseChanges
               .Where(p => p.Name.Equals(PhaseName.Construction) && p.Status.Equals(PhaseStatus.Producing))
               .Select(p => _deltaBuilder.BuildCandidateDelta(p.PreviousDeltaDfsHash))
               .Where(c => c != null)
               .Subscribe(c =>
                {
                    _deltaVoter.OnNext(c);
                    _deltaHub.BroadcastCandidate(c);
                });

            _campaigningProductionSubscription = _cycleEventsProvider.PhaseChanges
               .Where(p => p.Name.Equals(PhaseName.Campaigning) && p.Status.Equals(PhaseStatus.Producing))
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
               .Where(p => p.Name.Equals(PhaseName.Voting) && p.Status.Equals(PhaseStatus.Producing))
               .Select(p => _deltaElector.GetMostPopularCandidateDelta(p.PreviousDeltaDfsHash))
               .Where(c => c != null)
               .Select(c =>
                {
                    _deltaCache.TryGetLocalDelta(c, out var delta);
                    return delta;
                })
               .Where(d => d != null)
               .Subscribe(async d =>
                {
                    // here were some importnt changes for Web3 so need to have a look if I can delete the comments
                    // <<<<<<< HEAD
                    //                     var newCid = _deltaHub.PublishDeltaToDfsAndBroadcastAddressAsync(d)
                    //                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    //                     _deltaCache.AddLocalDelta(newCid, d);
                    //                     
                    //                     var previousHash = d.PreviousDeltaDfsHash.ToByteArray().ToCid();
                    //                     
                    //                     _logger.Information("New Delta following {deltaHash} published with new cid {newCid}", 
                    //                         d.PreviousDeltaDfsHash, newCid);
                    //
                    //                     _deltaHashProvider.TryUpdateLatestHash(previousHash, newCid);
                    // =======

                    _logger.Information("New Delta following {deltaHash} published",
                        d.PreviousDeltaDfsHash);

                    var newHashCid = _deltaHub.PublishDeltaToDfsAndBroadcastAddressAsync(d)
                       .ConfigureAwait(false).GetAwaiter().GetResult();
                    var previousHashCid = d.PreviousDeltaDfsHash.ToByteArray().ToCid();

                    _deltaHashProvider.TryUpdateLatestHash(previousHashCid, newHashCid);
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
