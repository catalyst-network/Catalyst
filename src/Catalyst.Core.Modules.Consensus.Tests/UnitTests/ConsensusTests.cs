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
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Consensus.Cycle;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using MultiFormats.Registry;
using NSubstitute;
using PeerTalk;
using Serilog;
using Xunit;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests
{
    public class ConsensusTests : IDisposable
    {
        private readonly IHashProvider _hashProvider;
        private readonly IDeltaBuilder _deltaBuilder;
        private readonly IDeltaVoter _deltaVoter;
        private readonly IDeltaElector _deltaElector;
        private readonly IDeltaCache _deltaCache;
        private readonly IDeltaHub _deltaHub;
        private readonly TestCycleEventProvider _cycleEventProvider;
        private readonly Consensus _consensus;

        public ConsensusTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _cycleEventProvider = new TestCycleEventProvider();
            _deltaBuilder = Substitute.For<IDeltaBuilder>();
            _deltaVoter = Substitute.For<IDeltaVoter>();
            _deltaElector = Substitute.For<IDeltaElector>();
            _deltaCache = Substitute.For<IDeltaCache>();
            _deltaHub = Substitute.For<IDeltaHub>();
            var deltaHashProvider = Substitute.For<IDeltaHashProvider>();
            var logger = Substitute.For<ILogger>();
            _consensus = new Consensus(
                _deltaBuilder,
                _deltaVoter,
                _deltaElector,
                _deltaCache,
                _deltaHub,
                _cycleEventProvider,
                deltaHashProvider,
                logger);

            _consensus.StartProducing();
        }

        private Cid PreviousDeltaBytes => _cycleEventProvider.CurrentPhase.PreviousDeltaDfsHash;

        [Fact]
        public void
            ConstructionProducingSubscription_Should_Trigger_BuildDeltaCandidate_On_Construction_Producing_Phase()
        {
            var builtCandidate = DeltaHelper.GetCandidateDelta(_hashProvider);
            _deltaBuilder.BuildCandidateDelta(Arg.Any<Cid>()).Returns(builtCandidate);

            _cycleEventProvider.MovePastNextPhase(PhaseName.Construction);

            _deltaBuilder.Received(1).BuildCandidateDelta(
                Arg.Is<Cid>(b => b == PreviousDeltaBytes));
            _deltaHub.Received(1).BroadcastCandidate(Arg.Is(builtCandidate));
        }

        [Fact]
        public void
            CampaigningProductionSubscription_Should_Trigger_TryGetFavouriteDelta_On_Campaigning_Producing_Phase()
        {
            var favourite = DeltaHelper.GetFavouriteDelta(_hashProvider);
            _deltaVoter.TryGetFavouriteDelta(Arg.Any<Cid>(), out Arg.Any<FavouriteDeltaBroadcast>())
               .Returns(ci =>
                {
                    ci[1] = favourite;
                    return true;
                });

            _cycleEventProvider.MovePastNextPhase(PhaseName.Campaigning);

            _deltaVoter.Received(1).TryGetFavouriteDelta(
                Arg.Is<Cid>(b => b == PreviousDeltaBytes),
                out Arg.Any<FavouriteDeltaBroadcast>());
            _deltaHub.Received(1).BroadcastFavouriteCandidateDelta(Arg.Is(favourite));
        }

        [Fact]
        public void CampaigningProductionSubscription_Should_Not_Broadcast_On_TryGetFavouriteDelta_Null()
        {
            _deltaVoter.TryGetFavouriteDelta(Arg.Any<Cid>(), out Arg.Any<FavouriteDeltaBroadcast>())
               .Returns(ci =>
                {
                    ci[1] = null;
                    return false;
                });

            _cycleEventProvider.MovePastNextPhase(PhaseName.Campaigning);

            _deltaVoter.Received(1).TryGetFavouriteDelta(
                Arg.Is<Cid>(b => b == PreviousDeltaBytes),
                out Arg.Any<FavouriteDeltaBroadcast>());

            _deltaHub.DidNotReceiveWithAnyArgs().BroadcastFavouriteCandidateDelta(default);
        }

        [Fact]
        public void VotingProductionSubscription_Should_Hit_Cache_And_Publish_To_Dfs()
        {
            var popularCandidate = DeltaHelper.GetCandidateDelta(_hashProvider);
            var localDelta = DeltaHelper.GetDelta(_hashProvider);

            _deltaElector.GetMostPopularCandidateDelta(Arg.Any<Cid>())
               .Returns(popularCandidate);
            _deltaCache.TryGetLocalDelta(Arg.Any<CandidateDeltaBroadcast>(), out Arg.Any<Delta>())
               .Returns(ci =>
                {
                    ci[1] = localDelta;
                    return true;
                });

            _deltaHub.PublishDeltaToDfsAndBroadcastAddressAsync(default)
               .ReturnsForAnyArgs((Cid) CidHelper.CreateCid(
                    _hashProvider.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(1000))
                ));

            _cycleEventProvider.MovePastNextPhase(PhaseName.Voting);
            _cycleEventProvider.Scheduler.Stop();

            _deltaElector.Received(1).GetMostPopularCandidateDelta(
                Arg.Is<Cid>(b => b == PreviousDeltaBytes));

            _deltaCache.Received(1).TryGetLocalDelta(popularCandidate, out _);
            _deltaHub.Received(1)?.PublishDeltaToDfsAndBroadcastAddressAsync(localDelta);
        }

        [Fact]
        public void
            VotingProductionSubscription_Should_Not_Hit_Cache_Or_Publish_To_Dfs_On_GetMostPopularCandidateDelta_Null()
        {
            _deltaElector.GetMostPopularCandidateDelta(Arg.Any<Cid>())
               .Returns((CandidateDeltaBroadcast) null);

            _cycleEventProvider.MovePastNextPhase(PhaseName.Voting);
            _cycleEventProvider.Scheduler.Stop();

            _deltaElector.Received(1).GetMostPopularCandidateDelta(
                Arg.Is<Cid>(b => b == PreviousDeltaBytes));

            _deltaCache.DidNotReceiveWithAnyArgs().TryGetLocalDelta(default, out _);
            _deltaHub.DidNotReceiveWithAnyArgs()?.PublishDeltaToDfsAndBroadcastAddressAsync(default);
        }

        [Fact]
        public void VotingProductionSubscription_Should_Not_Publish_To_Dfs_On_GetMostPopularCandidateDelta_Null()
        {
            var popularCandidate = DeltaHelper.GetCandidateDelta(_hashProvider);

            _deltaElector.GetMostPopularCandidateDelta(Arg.Any<Cid>())
               .Returns(popularCandidate);
            _deltaCache.TryGetLocalDelta(Arg.Any<CandidateDeltaBroadcast>(), out Arg.Any<Delta>())
               .Returns(ci =>
                {
                    ci[1] = null;
                    return false;
                });

            _cycleEventProvider.MovePastNextPhase(PhaseName.Voting);
            _cycleEventProvider.Scheduler.Stop();

            _deltaElector.Received(1).GetMostPopularCandidateDelta(
                Arg.Is<Cid>(b => b == PreviousDeltaBytes));

            _deltaCache.Received(1).TryGetLocalDelta(popularCandidate, out _);
            _deltaHub.DidNotReceiveWithAnyArgs()?.PublishDeltaToDfsAndBroadcastAddressAsync(default);
        }

        public void Dispose()
        {
            _cycleEventProvider?.Dispose();
            _consensus?.Dispose();
        }
    }
}
