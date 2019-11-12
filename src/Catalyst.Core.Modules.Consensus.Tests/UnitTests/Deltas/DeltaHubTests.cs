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
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Consensus.Deltas;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Polly;
using Polly.Retry;
using Serilog;
using Xunit;
using LibP2P;
using TheDotNetLeague.MultiFormats.MultiHash;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public sealed class DeltaHubTests
    {
        private readonly IHashProvider _hashProvider;
        private readonly IBroadcastManager _broadcastManager;
        private readonly PeerId _peerId;
        private readonly DeltaHub _hub;
        private readonly IDfs _dfs;

        internal sealed class DeltaHubWithFastRetryPolicy : DeltaHub
        {
            public DeltaHubWithFastRetryPolicy(IBroadcastManager broadcastManager,
                IPeerSettings peerSettings,
                IDfs dfs, 
                ILogger logger) : base(broadcastManager, peerSettings, dfs, logger) { }

            protected override AsyncRetryPolicy<Cid> DfsRetryPolicy =>
                Polly.Policy<Cid>.Handle<Exception>()
                   .WaitAndRetryAsync(4, retryAttempt => 
                        TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)));
        }

        public DeltaHubTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _broadcastManager = Substitute.For<IBroadcastManager>();
            var logger = Substitute.For<ILogger>();
            _peerId = PeerIdHelper.GetPeerId("me");
            _dfs = Substitute.For<IDfs>();
            _hub = new DeltaHubWithFastRetryPolicy(_broadcastManager, _peerId.ToSubstitutedPeerSettings(), _dfs, logger);
        }

        [Fact]
        public void BroadcastCandidate_should_not_broadcast_candidates_from_other_nodes()
        {
            var notMyCandidate = DeltaHelper.GetCandidateDelta(_hashProvider,
                producerId: PeerIdHelper.GetPeerId("not me"));

            _hub.BroadcastCandidate(notMyCandidate);
            _broadcastManager.DidNotReceiveWithAnyArgs().BroadcastAsync(default);
        }

        [Fact]
        public void BroadcastCandidate_should_allow_broadcasting_candidate_from_this_node()
        {
            var myCandidate = DeltaHelper.GetCandidateDelta(_hashProvider,
                producerId: _peerId);

            _hub.BroadcastCandidate(myCandidate);
            _broadcastManager.Received(1)?.BroadcastAsync(Arg.Is<ProtocolMessage>(
                m => IsExpectedCandidateMessage(m, myCandidate, _peerId)));
        }
        
        [Fact]
        public void BroadcastFavouriteCandidateDelta_Should_Broadcast()
        {
            var favourite = new FavouriteDeltaBroadcast
            {
                Candidate = DeltaHelper.GetCandidateDelta(_hashProvider),
                VoterId = _peerId
            };

            _hub.BroadcastFavouriteCandidateDelta(favourite);
            _broadcastManager.Received(1)?.BroadcastAsync(Arg.Is<ProtocolMessage>(
                c => IsExpectedCandidateMessage(c, favourite, _peerId)));
        }

        [Fact]
        public async Task PublishDeltaToIpfsAsync_should_return_ipfs_address()
        {
            var delta = DeltaHelper.GetDelta(_hashProvider);
            var cid = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("lskdjaslkjfweoho"));
            var cancellationToken = new CancellationToken();

            _dfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), cancellationToken).Returns(cid);

            var deltaCid = await _hub.PublishDeltaToDfsAndBroadcastAddressAsync(delta, cancellationToken);
            deltaCid.Should().NotBeNull();
            deltaCid.Should().Be(cid);
        }

        [Fact]
        public async Task PublishDeltaToIpfsAsync_should_retry_then_return_ipfs_address()
        {
            var delta = DeltaHelper.GetDelta(_hashProvider);
            var cid = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("success"));

            var dfsResults = new SubstituteResults<Cid>(() => throw new Exception("this one failed"))
               .Then(() => throw new Exception("this one failed too"))
               .Then(cid);

            _dfs.AddAsync(Arg.Any<Stream>(), Arg.Any<string>())
               .Returns(ci => dfsResults.Next());

            var deltaCid = await _hub.PublishDeltaToDfsAndBroadcastAddressAsync(delta);
            deltaCid.Should().NotBeNull();
            deltaCid.Should().Be(cid);

            await _dfs.ReceivedWithAnyArgs(3).AddAsync(Arg.Any<Stream>(), Arg.Any<string>());
        }

        [Fact]
        public async Task PublishDeltaToIpfsAsync_should_retry_until_cancelled()
        {
            var delta = DeltaHelper.GetDelta(_hashProvider);
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
                var hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
                var noPreviousHash = new Delta {PreviousDeltaDfsHash = (new byte[0]).ToByteString()};
                var noMerkleRoot = DeltaHelper.GetDelta(hashProvider, merkleRoot: new byte[0]);
                
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
