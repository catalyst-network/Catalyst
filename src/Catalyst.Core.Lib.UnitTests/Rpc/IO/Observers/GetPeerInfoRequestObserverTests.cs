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
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Core.Lib.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.Rpc.IO.Observers
{
    /// <summary>
    /// Tests the get peer info calls
    /// </summary>
    public sealed class GetPeerInfoRequestObserverTests
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IRepository<Peer> _peerRepository;

        public GetPeerInfoRequestObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);

            var peers = GetPeerTestData();

            _peerRepository = Substitute.For<IRepository<Peer>>();
            _peerRepository.FindAll(Arg.Any<Expression<Func<Peer, bool>>>())
               .Returns(ci =>
                {
                    return peers.Where(p => ((Expression<Func<Peer, bool>>) ci[0]).Compile()(p));
                });
        }

        public IEnumerable<Peer> GetPeerTestData()
        {
            yield return new Peer 
            {
                PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"publickey-1", $"id-1", 1, IPAddress.Parse($"172.0.0.1"), 9090), LastSeen = DateTime.UtcNow, Created = DateTime.UtcNow
            };
            yield return new Peer
            {
                PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"publickey-2", $"id-2", 1, IPAddress.Parse($"172.0.0.2"), 9090), LastSeen = DateTime.UtcNow, Created = DateTime.UtcNow
            };
            yield return new Peer
            {
                PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"publickey-3", $"id-3", 1, IPAddress.Parse($"172.0.0.3"), 9090), LastSeen = DateTime.UtcNow, Created = DateTime.UtcNow
            };
            yield return new Peer
            {
                PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"publickey-3", $"id-4", 1, IPAddress.Parse($"172.0.0.3"), 9090), LastSeen = DateTime.UtcNow, Created = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Tests the get peer info request and response via RPC.
        /// Peer is expected to be found in this case
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        [Theory]
        [InlineData("publickey-1", "172.0.0.1")]
        [InlineData("publickey-2", "172.0.0.2")]
        public async Task TestGetPeerInfoRequestSingularResponse(string publicKey, string ipAddress)
        {
            var peerId = PeerIdHelper.GetPeerId(publicKey, "id-1", 1, ipAddress, 12345);
            var responseContent = await GetPeerInfoTest(peerId);
            responseContent.PeerInfo.Count().Should().Be(1);

            foreach (var peerInfo in responseContent.PeerInfo)
            {
                peerInfo.PeerId.Ip.ToByteArray().Should().BeEquivalentTo(peerId.Ip.ToByteArray());
                peerInfo.PeerId.PublicKey.ToByteArray().Should().BeEquivalentTo(peerId.PublicKey.ToByteArray());
            }
        }

        /// <summary>
        /// Tests the get peer info request and response via RPC.
        /// Peer is expected to be found in this case
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        [Theory]
        [InlineData("publickey-3", "172.0.0.3")]
        public async Task TestGetPeerInfoRequestRepeatedResponse(string publicKey, string ipAddress)
        {
            var peerId = PeerIdHelper.GetPeerId(publicKey, "id-1", 1, ipAddress, 12345);
            var responseContent = await GetPeerInfoTest(peerId);
            responseContent.PeerInfo.Count().Should().Be(2);

            foreach (var peerInfo in responseContent.PeerInfo)
            {
                peerInfo.PeerId.Ip.ToByteArray().Should().BeEquivalentTo(peerId.Ip.ToByteArray());
                peerInfo.PeerId.PublicKey.ToByteArray().Should().BeEquivalentTo(peerId.PublicKey.ToByteArray());
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
        [InlineData("this-pk-should-not-exist", "172.0.0.3")]
        [InlineData("publickey-1", "0.0.0.0")]
        [InlineData("publickey-3", "0.0.0.0")]
        public async Task TestGetPeerInfoRequestResponseForNonExistantPeers(string publicKey, string ipAddress)
        {
            var peerId = PeerIdHelper.GetPeerId(publicKey, "id-1", 1, ipAddress, 12345);
            var responseContent = await GetPeerInfoTest(peerId);
            responseContent.PeerInfo.Count.Should().Be(0);
        }

        /// <summary>
        /// Tests the data/communication through protobuf
        /// </summary>
        /// <returns></returns>
        private async Task<GetPeerInfoResponse> GetPeerInfoTest(PeerId peerId)
        {
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            var sendPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var messageFactory = new DtoFactory();
            var request = new GetPeerInfoRequest {PublicKey = peerId.PublicKey, Ip = peerId.Ip};

            var requestMessage = messageFactory.GetDto(
                request,
                sendPeerIdentifier,
                PeerIdentifierHelper.GetPeerIdentifier("recipient")
            );

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId));

            var handler = new GetPeerInfoRequestObserver(sendPeerIdentifier, _logger, _peerRepository);
            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls[0].GetArguments().Single();

            return sentResponseDto.FromIMessageDto().FromProtocolMessage<GetPeerInfoResponse>();
        }
    }
}
