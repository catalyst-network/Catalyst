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
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Node.Core.RPC.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FizzWare.NBuilder;
using FluentAssertions;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC.IO.Observers
{
    /// <summary>
    /// Tests the get peer info calls
    /// </summary>
    public sealed class GetPeerInfoRequestObserverTest
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPeerInfoRequestObserverTest"/> class.
        /// </summary>
        public GetPeerInfoRequestObserverTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        /// <summary>
        /// Tests the get peer info request and response via RPC.
        /// Peer is expected to be found in this case
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        [Theory]
        [InlineData("publickey-10", "172.0.0.10")]
        [InlineData("publickey-15", "172.0.0.15")]
        public async Task TestGetPeerInfoRequestResponse(string publicKey, string ipAddress)
        {
            publicKey = TestDataHelper.AppendPadding(publicKey, 32, '\0');
            var responseContent = await GetPeerInfoTest(publicKey, ipAddress);

            for (var i = 0; i < responseContent.PeerInfo.Count; i++)
            {
                var peerInfo = responseContent.PeerInfo[i];
                var peerIdentifier = new PeerIdentifier(peerInfo.PeerId);
                ipAddress.Should().Be(peerIdentifier.Ip.ToString());
                publicKey.Should().Be(peerIdentifier.PublicKey.ToStringFromRLPDecoded());
            }
        }

        /// <summary>
        /// Tests the get peer info request and response via RPC.
        /// Peer is NOT expected to be found in this case, as they do not exist
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        [Theory]
        [InlineData("this-pk-should-not-exist", "172.0.0.1")]
        [InlineData("publickey-5", "0.0.0.0")]
        public async Task TestGetPeerInfoRequestResponseForNonExistantPeers(string publicKey, string ipAddress)
        {
            publicKey = TestDataHelper.AppendPadding(publicKey, 32, '\0');
            var responseContent = await GetPeerInfoTest(publicKey, ipAddress);
            responseContent.PeerInfo.Count.Should().Be(0);
        }

        /// <summary>
        /// Assign the peer identifiers to the peers.
        /// </summary>
        /// <param name="peerIdentifiers">All the peerIdentifiers we will assign to peers</param>
        /// <param name="peers">All the peers we will update</param>
        private void AssignPeerIdentifiersToPeers(IList<IPeerIdentifier> peerIdentifiers, IList<Peer> peers)
        {
            var minCount = Math.Min(peerIdentifiers.Count(), peers.Count);
            for (var i = 0; i < peers.Count(); i++)
            {
                var peerPosition = i % minCount;
                peers[i].PeerIdentifier = peerIdentifiers[peerPosition];
            }
        }

        /// <summary>
        /// ProtoBuff requires all dates in UTC
        /// </summary>
        /// <param name="peers">All the peers we will update</param>
        private void SetPeerDateTimeValuesToUTC(IList<Peer> peers)
        {
            foreach (var peer in peers)
            {
                peer.LastSeen = DateTime.SpecifyKind(peer.LastSeen, DateTimeKind.Utc);
                if (peer.Modified.HasValue)
                    peer.Modified = DateTime.SpecifyKind(peer.Modified.Value, DateTimeKind.Utc);
                peer.Created = DateTime.SpecifyKind(peer.Created, DateTimeKind.Utc);
            }
        }

        /// <summary>
        /// Tests the data/communication through protobuf
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        /// <returns></returns>
        private async Task<GetPeerInfoResponse> GetPeerInfoTest(string publicKey, string ipAddress)
        {
            var peerIdentifiers = Enumerable.Range(0, 30).Select(i =>
                PeerIdentifierHelper.GetPeerIdentifier($"publickey-{i}", $"id{i}", 1, IPAddress.Parse($"172.0.0.{i}"), 9090)
            ).ToList();
            var peers = Builder<Peer>.CreateListOfSize(100).All().Build();

            AssignPeerIdentifiersToPeers(peerIdentifiers, peers);
            SetPeerDateTimeValuesToUTC(peers);

            //The query GetPeerInfo is expected to run
            var queryPeers = peers.Where(m => m.PeerIdentifier.Ip.ToString() == ipAddress && m.PeerIdentifier.PublicKey.ToStringFromRLPDecoded() == publicKey).ToList();

            var peerRepository = Substitute.For<IRepository<Peer>>();
            peerRepository.FindAll(Arg.Any<Expression<Func<Peer, bool>>>())
            .Returns(queryPeers);

            //Build a fake remote endpoint
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            var sendPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var messageFactory = new DtoFactory();
            var request = new GetPeerInfoRequest
            {
                PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString(),
                Ip = ipAddress.ToBytesForRLPEncoding().ToByteString()
            };

            var requestMessage = messageFactory.GetDto(
                request,
                sendPeerIdentifier,
                PeerIdentifierHelper.GetPeerIdentifier("recipient")
            );

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId));

            var handler = new GetPeerInfoRequestObserver(sendPeerIdentifier, _logger, peerRepository);
            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>)receivedCalls[0].GetArguments().Single();

            return sentResponseDto.FromIMessageDto().FromProtocolMessage<GetPeerInfoResponse>();
        }
    }
}
