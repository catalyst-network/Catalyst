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

using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Modules.Consensus.Cycle;
using Serilog;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Catalyst.Protocol.Deltas;
using Google.Protobuf.WellKnownTypes;
using GraphQL.Introspection;
using Multiformats.Hash;

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
                    try
                    {
                        _deltaVoter.OnNext(c);
                        _deltaHub.BroadcastCandidate(c);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "failed to vote");
                    }
                });

            _campaigningProductionSubscription = _cycleEventsProvider.PhaseChanges
               .Where(p => p.Name == PhaseName.Campaigning && p.Status == PhaseStatus.Producing)
               .Select(p =>
                {
                    try
                    {
                        _deltaVoter.TryGetFavouriteDelta(p.PreviousDeltaDfsHash, out var favourite);
                        return favourite;
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "failed to get favourite");
                        return null;
                    }
                })
               .Where(f => f != null)
               .Subscribe(f =>
                {
                    try
                    {
                        _deltaHub.BroadcastFavouriteCandidateDelta(f);
                        _deltaElector.OnNext(f);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "failed to broadcast favourite");
                    }
                });

            _votingProductionSubscription = _cycleEventsProvider.PhaseChanges
               .Where(p => p.Name == PhaseName.Voting && p.Status == PhaseStatus.Producing)
               .Select(p => _deltaElector.GetMostPopularCandidateDelta(p.PreviousDeltaDfsHash))
               .Where(c => c != null)
               .Select(c =>
                {
                    Delta delta = null;
                    try
                    {
                        _deltaCache.TryGetLocalDelta(c, out delta);
                    }
                    catch (Exception exception)
                    {
                        _logger.Error(exception, "failed");
                    }

                    _logger.Debug("Local Delta found {delta} Key {key}", delta, c);
                    return delta;
                })
               .Where(d => d != null)
               .Subscribe(d =>
                {
                    _logger.Debug("New Delta getting published");
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
