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
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Consensus.Deltas;
using Catalyst.Core.Util;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Polly;
using Polly.Retry;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Consensus.Deltas
{
    public sealed class DeltaHubTests
    {
        private readonly IBroadcastManager _broadcastManager;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly DeltaHub _hub;
        private readonly IDfs _dfs;

        internal sealed class DeltaHubWithFastRetryPolicy : DeltaHub
        {
            public DeltaHubWithFastRetryPolicy(IBroadcastManager broadcastManager,
                IPeerIdentifier peerIdentifier,
                IDeltaVoter deltaVoter,
                IDeltaElector deltaElector,
                IDfs dfs,
                IDeltaHashProvider hashProvider,
                ILogger logger) : base(broadcastManager, peerIdentifier, dfs, logger) { }

            protected override AsyncRetryPolicy<string> DfsRetryPolicy => 
                Policy<string>.Handle<Exception>()
                   .WaitAndRetryAsync(4, retryAttempt => 
                        TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)));
        }

        public DeltaHubTests()
        {
            _broadcastManager = Substitute.For<IBroadcastManager>();
            var logger = Substitute.For<ILogger>();
            _peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("me");
            var deltaVoter = Substitute.For<IDeltaVoter>();
            var deltaElector = Substitute.For<IDeltaElector>();
            var dfs = Substitute.For<IDfs>();
            var hashProvider = Substitute.For<IDeltaHashProvider>();
            _dfs = dfs;
            _hub = new DeltaHubWithFastRetryPolicy(_broadcastManager, _peerIdentifier, deltaVoter, deltaElector, _dfs, hashProvider, logger);
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
                m => IsExpectedCandidateMessage<CandidateDeltaBroadcast>(m, myCandidate, _peerIdentifier.PeerId)));
        }
        
        [Fact]
        public void BroadcastFavouriteCandidateDelta_Should_Broadcast()
        {
            var favourite = new FavouriteDeltaBroadcast
            {
                Candidate = DeltaHelper.GetCandidateDelta(),
                VoterId = _peerIdentifier.PeerId
            };

            _hub.BroadcastFavouriteCandidateDelta(favourite);
            _broadcastManager.Received(1).BroadcastAsync(Arg.Is<ProtocolMessage>(
                c => IsExpectedCandidateMessage<FavouriteDeltaBroadcast>(c, favourite, _peerIdentifier.PeerId)));
        }

        [Fact]
        public async Task PublishDeltaToIpfsAsync_should_return_ipfs_address()
        {
            var delta = DeltaHelper.GetDelta();
            var dfsHash = "lskdjaslkjfweoho";
            var cancellationToken = new CancellationToken();

            _dfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), cancellationToken).Returns(dfsHash);

            var deltaHash = await _hub.PublishDeltaToDfsAndBroadcastAddressAsync(delta, cancellationToken);
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

            _dfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>())
               .Returns(ci => dfsResults.Next());

            var deltaHash = await _hub.PublishDeltaToDfsAndBroadcastAddressAsync(delta);
            deltaHash.Should().NotBeNullOrEmpty();
            deltaHash.Should().Be(dfsHash);

            await _dfs.ReceivedWithAnyArgs(3).AddAsync(Arg.Any<Stream>(), Arg.Any<string>());
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
               .Then(() =>
                {
                    cancellationSource.Cancel();
                    throw new Exception("this one failed too");
                })
               .Then(dfsHash);

            _dfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), cancellationToken)
               .Returns(ci => dfsResults.Next());

            new Action(() => _hub.PublishDeltaToDfsAndBroadcastAddressAsync(delta, cancellationToken).GetAwaiter().GetResult())
               .Should().NotThrow<TaskCanceledException>();

            await _dfs.ReceivedWithAnyArgs(3).AddAsync(Arg.Any<Stream>(), Arg.Any<string>());
        }

        public class BadDeltas : TheoryData<Delta>
        {
            public BadDeltas()
            {
                var noPreviousHash = new Delta {PreviousDeltaDfsHash = (new byte[0]).ToByteString()};
                var noMerkleRoot = DeltaHelper.GetDelta(merkleRoot: new byte[0]);
                
                AddRow(noMerkleRoot, typeof(InvalidDataException));
                AddRow(noPreviousHash, typeof(InvalidDataException));
                AddRow(null as Delta, typeof(ArgumentNullException));
            }
        }
        
        private bool IsExpectedCandidateMessage<T>(ProtocolMessage protocolMessage,
            T expected, 
            PeerId senderId) where T : IMessage<T>
        {
            var hasExpectedSender = protocolMessage.PeerId.Equals(senderId);
            var candidate = protocolMessage.FromProtocolMessage<T>();
            var hasExpectedCandidate = candidate.Equals(expected);
            return hasExpectedSender && hasExpectedCandidate;
        }
    }
}
