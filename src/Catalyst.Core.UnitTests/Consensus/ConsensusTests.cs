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
using System.Linq;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Core.Consensus.Cycle;
using Catalyst.Core.Extensions;
using Catalyst.Core.Util;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Consensus
{
    public class ConsensusTests : IDisposable
    {
        private readonly IDeltaBuilder _deltaBuilder;
        private readonly IDeltaVoter _deltaVoter;
        private readonly IDeltaElector _deltaElector;
        private readonly IDeltaCache _deltaCache;
        private readonly IDeltaHub _deltaHub;
        private readonly TestCycleEventProvider _cycleEventProvider;
        private readonly Core.Consensus.Consensus _consensus;

        public ConsensusTests()
        {
            _cycleEventProvider = new TestCycleEventProvider();
            _deltaBuilder = Substitute.For<IDeltaBuilder>();
            _deltaVoter = Substitute.For<IDeltaVoter>();
            _deltaElector = Substitute.For<IDeltaElector>();
            _deltaCache = Substitute.For<IDeltaCache>();
            _deltaHub = Substitute.For<IDeltaHub>();
            var hashProvider = Substitute.For<IDeltaHashProvider>();
            var logger = Substitute.For<ILogger>();
            _consensus = new Core.Consensus.Consensus(
                _deltaBuilder, 
                _deltaVoter, 
                _deltaElector,
                _deltaCache,
                _deltaHub, 
                _cycleEventProvider,
                hashProvider,
                logger);

            _consensus.StartProducing();
        }

        private byte[] PreviousDeltaBytes => _cycleEventProvider.CurrentPhase.PreviousDeltaDfsHash.ToBytes();

        [Fact]
        public void ConstructionProducingSubscription_Should_Trigger_BuildDeltaCandidate_On_Construction_Producing_Phase()
        {
            var builtCandidate = DeltaHelper.GetCandidateDelta();
            _deltaBuilder.BuildCandidateDelta(Arg.Any<byte[]>()).Returns(builtCandidate);
            
            _cycleEventProvider.MovePastNextPhase(PhaseName.Construction);

            _deltaBuilder.Received(1).BuildCandidateDelta(
                Arg.Is<byte[]>(b => b.SequenceEqual(PreviousDeltaBytes)));
            _deltaHub.Received(1).BroadcastCandidate(Arg.Is(builtCandidate));
        }

        [Fact]
        public void CampaigningProductionSubscription_Should_Trigger_TryGetFavouriteDelta_On_Campaigning_Producing_Phase()
        {
            var favourite = DeltaHelper.GetFavouriteDelta();
            _deltaVoter.TryGetFavouriteDelta(Arg.Any<byte[]>(), out Arg.Any<FavouriteDeltaBroadcast>())
               .Returns(ci =>
                {
                    ci[1] = favourite;
                    return true;
                });

            _cycleEventProvider.MovePastNextPhase(PhaseName.Campaigning);

            _deltaVoter.Received(1).TryGetFavouriteDelta(
                Arg.Is<byte[]>(b => b.SequenceEqual(PreviousDeltaBytes)), 
                out Arg.Any<FavouriteDeltaBroadcast>());
            _deltaHub.Received(1).BroadcastFavouriteCandidateDelta(Arg.Is(favourite));
        }

        [Fact]
        public void CampaigningProductionSubscription_Should_Not_Broadcast_On_TryGetFavouriteDelta_Null()
        {
            _deltaVoter.TryGetFavouriteDelta(Arg.Any<byte[]>(), out Arg.Any<FavouriteDeltaBroadcast>())
               .Returns(ci =>
                {
                    ci[1] = null;
                    return false;
                });

            _cycleEventProvider.MovePastNextPhase(PhaseName.Campaigning);

            _deltaVoter.Received(1).TryGetFavouriteDelta(
                Arg.Is<byte[]>(b => b.SequenceEqual(PreviousDeltaBytes)),
                out Arg.Any<FavouriteDeltaBroadcast>());

            _deltaHub.DidNotReceiveWithAnyArgs().BroadcastFavouriteCandidateDelta(default);
        }

        [Fact]
        public void VotingProductionSubscription_Should_Hit_Cache_And_Publish_To_Dfs()
        {
            var popularCandidate = DeltaHelper.GetCandidateDelta();
            var localDelta = DeltaHelper.GetDelta();

            _deltaElector.GetMostPopularCandidateDelta(Arg.Any<byte[]>())
               .Returns(popularCandidate);
            _deltaCache.TryGetLocalDelta(Arg.Any<CandidateDeltaBroadcast>(), out Arg.Any<Delta>())
               .Returns(ci =>
                {
                    ci[1] = localDelta;
                    return true;
                });

            _deltaHub.PublishDeltaToDfsAndBroadcastAddressAsync(default, default)
               .ReturnsForAnyArgs(ByteUtil.GenerateRandomByteArray(1000).ComputeMultihash(new BLAKE2B_256())
                   .AsBase32Address());

            _cycleEventProvider.MovePastNextPhase(PhaseName.Voting);
            _cycleEventProvider.Scheduler.Stop();

            _deltaElector.Received(1).GetMostPopularCandidateDelta(
                Arg.Is<byte[]>(b => b.SequenceEqual(PreviousDeltaBytes)));

            _deltaCache.Received(1).TryGetLocalDelta(popularCandidate, out _);
            _deltaHub.Received(1).PublishDeltaToDfsAndBroadcastAddressAsync(localDelta);
        }

        [Fact]
        public void VotingProductionSubscription_Should_Not_Hit_Cache_Or_Publish_To_Dfs_On_GetMostPopularCandidateDelta_Null()
        {
            _deltaElector.GetMostPopularCandidateDelta(Arg.Any<byte[]>())
               .Returns((CandidateDeltaBroadcast) null);

            _cycleEventProvider.MovePastNextPhase(PhaseName.Voting);
            _cycleEventProvider.Scheduler.Stop();

            _deltaElector.Received(1).GetMostPopularCandidateDelta(
                Arg.Is<byte[]>(b => b.SequenceEqual(PreviousDeltaBytes)));

            _deltaCache.DidNotReceiveWithAnyArgs().TryGetLocalDelta(default, out _);
            _deltaHub.DidNotReceiveWithAnyArgs().PublishDeltaToDfsAndBroadcastAddressAsync(default);
        }

        [Fact]
        public void VotingProductionSubscription_Should_Not_Publish_To_Dfs_On_GetMostPopularCandidateDelta_Null()
        {
            var popularCandidate = DeltaHelper.GetCandidateDelta();

            _deltaElector.GetMostPopularCandidateDelta(Arg.Any<byte[]>())
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
                Arg.Is<byte[]>(b => b.SequenceEqual(PreviousDeltaBytes)));

            _deltaCache.Received(1).TryGetLocalDelta(popularCandidate, out _);
            _deltaHub.DidNotReceiveWithAnyArgs().PublishDeltaToDfsAndBroadcastAddressAsync(default);
        }

        public void Dispose()
        {
            _cycleEventProvider?.Dispose();
            _consensus?.Dispose();
        }
    }
}
