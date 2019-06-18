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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Broadcast;
using Catalyst.Common.Util;
using Catalyst.Node.Core.Modules.Consensus.Delta;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Delta;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Delta
{
    public sealed class DeltaHubTests : IDisposable
    {
        private readonly IBroadcastManager _broadcastManager;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IDeltaVoter _deltaVoter;
        private readonly DeltaHub _hub;
        private readonly IDfs _dfs;

        public DeltaHubTests()
        {
            _broadcastManager = Substitute.For<IBroadcastManager>();
            var logger = Substitute.For<ILogger>();
            _peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("me");
            _deltaVoter = Substitute.For<IDeltaVoter>();
            var deltaElector = Substitute.For<IDeltaElector>();
            var dfs = Substitute.For<IDfs>();
            _dfs = dfs;
            _hub = new DeltaHub(_broadcastManager, _peerIdentifier, _deltaVoter, deltaElector, _dfs, logger);
        }

        [Fact]
        public void BroadcastCandidate_should_not_broadcast_candidates_from_other_nodes()
        {
            var notMyCandidate = DeltaHelper.GetCandidateDelta(
                producerId: PeerIdHelper.GetPeerId("not me"));

            _hub.BroadcastCandidate(notMyCandidate);
            _broadcastManager.DidNotReceiveWithAnyArgs().BroadcastAsync(default);
        }

        [Fact]
        public void BroadcastCandidate_should_allow_broadcasting_candidate_from_this_node()
        {
            var myCandidate = DeltaHelper.GetCandidateDelta(
                producerId: _peerIdentifier.PeerId);

            _hub.BroadcastCandidate(myCandidate);
            _broadcastManager.Received(1).BroadcastAsync(Arg.Is<ProtocolMessage>(
                m => IsExpectedCandidateMessage(m, myCandidate, _peerIdentifier.PeerId)));
        }

        [Fact]
        public void BroadcastFavouriteCandidateDelta_should_not_broadcast_if_not_found()
        {
            _deltaVoter.TryGetFavouriteDelta(Arg.Any<byte[]>(), 
                out Arg.Any<CandidateDeltaBroadcast>()).Returns(false);

            _hub.BroadcastFavouriteCandidateDelta(ByteUtil.GenerateRandomByteArray(32));
            _broadcastManager.DidNotReceiveWithAnyArgs().BroadcastAsync(default);
        }

        [Fact]
        public void BroadcastFavouriteCandidateDelta_should_broadcast_if_found()
        {
            var someCandidate = DeltaHelper.GetCandidateDelta();

            _deltaVoter.TryGetFavouriteDelta(Arg.Any<byte[]>(),
                out Arg.Any<CandidateDeltaBroadcast>()).Returns(ci =>
            {
                ci[1] = someCandidate;
                return true;
            });

            _hub.BroadcastFavouriteCandidateDelta(ByteUtil.GenerateRandomByteArray(32));
            _broadcastManager.Received(1).BroadcastAsync(Arg.Is<ProtocolMessage>(
                c => IsExpectedCandidateMessage(c, someCandidate, _peerIdentifier.PeerId)));
        }

        [Fact]
        public async Task PublishDeltaToIpfsAsync_should_return_ipfs_address()
        {
            var delta = DeltaHelper.GetDelta();
            var dfsHash = "lskdjaslkjfweoho";
            var cancellationToken = new CancellationToken();

            _dfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), cancellationToken).Returns(dfsHash);

            var deltaHash = await _hub.PublishDeltaToIpfsAsync(delta, cancellationToken);
            deltaHash.Should().NotBeNullOrEmpty();
            deltaHash.Should().Be(dfsHash);
        }

        [Fact]
        public async Task PublishDeltaToIpfsAsync_should_retry_then_return_ipfs_address()
        {
            var delta = DeltaHelper.GetDelta();
            var dfsHash = "success";

            var dfsResults = new SubstituteResults<string>(() => throw new Exception("this one failed"))
               .Then(() => throw new Exception("this one failed too"))
               .Then(dfsHash);

            _dfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), default)
               .Returns(ci => dfsResults.Next());

            var deltaHash = await _hub.PublishDeltaToIpfsAsync(delta);
            deltaHash.Should().NotBeNullOrEmpty();
            deltaHash.Should().Be(dfsHash);

            await _dfs.ReceivedWithAnyArgs(3).AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), default);
        }

        [Fact]
        public async Task PublishDeltaToIpfsAsync_should_retry_until_cancelled()
        {
            var delta = DeltaHelper.GetDelta();
            var dfsHash = "success";
            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;

            var dfsResults = new SubstituteResults<string>(() => throw new Exception("this one failed"))
               .Then(() => throw new Exception("this one failed again"))
               .Then(() => throw new Exception("this one failed again"))
               .Then(() => throw new Exception("this one failed again"))
               .Then(() =>
                {
                    cancellationSource.Cancel();
                    throw new Exception("this one failed too");
                })
               .Then(dfsHash);

            _dfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), cancellationToken)
               .Returns(ci => dfsResults.Next());

            new Action(() => _hub.PublishDeltaToIpfsAsync(delta, cancellationToken).GetAwaiter().GetResult())
               .Should().Throw<TaskCanceledException>();

            await _dfs.ReceivedWithAnyArgs(5).AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), default);
        }

        [Theory]
        [ClassData(typeof(BadDeltas))]
        public async Task PublishDeltaToIpfsAsync_should_not_send_invalid_deltas(Protocol.Delta.Delta badDelta)
        {
            new Action(() => _hub.PublishDeltaToIpfsAsync(badDelta).GetAwaiter().GetResult())
               .Should().Throw<ArgumentException>();
            await _dfs.DidNotReceiveWithAnyArgs().AddAsync(default);
        }

        public class BadDeltas : TheoryData<Protocol.Delta.Delta>
        {
            public BadDeltas()
            {
                var noPreviousHash = DeltaHelper.GetDelta(previousDeltaHash: new byte[0]);
                var noMerkleRoot = DeltaHelper.GetDelta(merkleRoot: new byte[0]);
                
                AddRow(noMerkleRoot);
                AddRow(noPreviousHash);
                AddRow(null as Protocol.Delta.Delta);
            }
        }
        
        private bool IsExpectedCandidateMessage(ProtocolMessage protocolMessage,
            CandidateDeltaBroadcast expected, 
            PeerId senderId)
        {
            var hasExpectedSender = protocolMessage.PeerId.Equals(senderId);
            var candidate = protocolMessage.FromProtocolMessage<CandidateDeltaBroadcast>();
            var hasExpectedCandidate = candidate.Equals(expected);
            return hasExpectedSender && hasExpectedCandidate;
        }

        public void Dispose() { _hub?.Dispose(); }
    }
}
