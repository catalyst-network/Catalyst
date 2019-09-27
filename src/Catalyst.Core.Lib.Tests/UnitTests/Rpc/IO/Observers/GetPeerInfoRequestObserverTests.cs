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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Network;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    /// <summary>
    ///     Tests the get peer info calls
    /// </summary>
    public sealed class GetPeerInfoRequestObserverTests
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IPeerRepository _peerRepository;

        public GetPeerInfoRequestObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);

            var peers = GetPeerTestData();

            _peerRepository = Substitute.For<IPeerRepository>();
            _peerRepository.FindAll(Arg.Any<Expression<Func<Peer, bool>>>())
               .Returns(ci => { return peers.Where(p => ((Expression<Func<Peer, bool>>) ci[0]).Compile()(p)); });
        }

        public IEnumerable<Peer> GetPeerTestData()
        {
            yield return new Peer
            {
                PeerId =
                    PeerIdHelper.GetPeerId("publickey-1", IPAddress.Parse("172.0.0.1"), 9090),
                LastSeen = DateTime.UtcNow, Created = DateTime.UtcNow
            };
            yield return new Peer
            {
                PeerId =
                    PeerIdHelper.GetPeerId("publickey-2", IPAddress.Parse("172.0.0.2"), 9090),
                LastSeen = DateTime.UtcNow, Created = DateTime.UtcNow
            };
            yield return new Peer
            {
                PeerId =
                    PeerIdHelper.GetPeerId("publickey-3", IPAddress.Parse("172.0.0.3"), 9090),
                LastSeen = DateTime.UtcNow, Created = DateTime.UtcNow
            };
            yield return new Peer
            {
                PeerId =
                    PeerIdHelper.GetPeerId("publickey-3", IPAddress.Parse("172.0.0.3"), 9090),
                LastSeen = DateTime.UtcNow, Created = DateTime.UtcNow
            };
        }

        /// <summary>
        ///     Tests the get peer info request and response via RPC.
        ///     Peer is expected to be found in this case
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        [Theory]
        [InlineData("publickey-1", "172.0.0.1")]
        [InlineData("publickey-2", "172.0.0.2")]
        public void TestGetPeerInfoRequestSingularResponse(string publicKey, string ipAddress)
        {
            var peerId = PeerIdHelper.GetPeerId(publicKey, ipAddress, 12345);
            var responseContent = GetPeerInfoTest(peerId);
            responseContent.PeerInfo.Count().Should().Be(1);

            foreach (var peerInfo in responseContent.PeerInfo)
            {
                peerInfo.PeerId.Ip.ToByteArray().Should().BeEquivalentTo(peerId.Ip.ToByteArray());
                peerInfo.PeerId.PublicKey.ToByteArray().Should().BeEquivalentTo(peerId.PublicKey.ToByteArray());
            }
        }

        /// <summary>
        ///     Tests the get peer info request and response via RPC.
        ///     Peer is expected to be found in this case
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        [Theory]
        [InlineData("publickey-3", "172.0.0.3")]
        public void TestGetPeerInfoRequestRepeatedResponse(string publicKey, string ipAddress)
        {
            var peerId = PeerIdHelper.GetPeerId(publicKey, ipAddress, 12345);
            var responseContent = GetPeerInfoTest(peerId);
            responseContent.PeerInfo.Count().Should().Be(2);

            foreach (var peerInfo in responseContent.PeerInfo)
            {
                peerInfo.PeerId.Ip.ToByteArray().Should().BeEquivalentTo(peerId.Ip.ToByteArray());
                peerInfo.PeerId.PublicKey.ToByteArray().Should().BeEquivalentTo(peerId.PublicKey.ToByteArray());
            }
        }

        /// <summary>
        ///     Tests the get peer info request and response via RPC.
        ///     Peer is NOT expected to be found in this case, as they do not exist
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        [Theory]
        [InlineData("this-pk-should-not-exist", "172.0.0.1")]
        [InlineData("this-pk-should-not-exist", "172.0.0.3")]
        [InlineData("publickey-1", "0.0.0.0")]
        [InlineData("publickey-3", "0.0.0.0")]
        public void TestGetPeerInfoRequestResponseForNonExistantPeers(string publicKey, string ipAddress)
        {
            var peerId = PeerIdHelper.GetPeerId(publicKey, ipAddress, 12345);
            var responseContent = GetPeerInfoTest(peerId);
            responseContent.PeerInfo.Count.Should().Be(0);
        }

        /// <summary>
        ///     Tests the data/communication through protobuf
        /// </summary>
        /// <returns></returns>
        private GetPeerInfoResponse GetPeerInfoTest(PeerId peerId)
        {
            var testScheduler = new TestScheduler();

            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            var senderPeerIdentifier = PeerIdHelper.GetPeerId("sender");
            var getPeerInfoRequest = new GetPeerInfoRequest {PublicKey = peerId.PublicKey, Ip = peerId.Ip};

            var protocolMessage =
                getPeerInfoRequest.ToProtocolMessage(senderPeerIdentifier);

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, testScheduler, protocolMessage);

            var peerSettings = senderPeerIdentifier.ToSubstitutedPeerSettings();
            var handler = new GetPeerInfoRequestObserver(peerSettings, _logger, _peerRepository);

            handler.StartObserving(messageStream);

            testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls[0].GetArguments().Single();

            return sentResponseDto.Content.FromProtocolMessage<GetPeerInfoResponse>();
        }
    }
}
