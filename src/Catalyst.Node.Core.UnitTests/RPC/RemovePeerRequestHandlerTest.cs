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
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Util;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC
{
    /// <summary>
    /// Tests remove peer CLI and RPC calls
    /// </summary>
    public sealed class RemovePeerRequestHandlerTest
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemovePeerRequestHandlerTest"/> class.
        /// </summary>
        public RemovePeerRequestHandlerTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        /// <summary>
        /// Tests the peer list request and response.
        /// </summary>
        /// <param name="fakePeers">The fake peers.</param>
        [Theory]
        [InlineData("FakePeer1", "FakePeer2")]
        [InlineData("FakePeer1002", "FakePeer6000", "FakePeerSataoshi")]
        public async Task TestRemovePeer(params string[] fakePeers) { await ExecuteTestCase(fakePeers, true); }

        /// <summary>
        /// Tests peer removal via IP only.
        /// </summary>
        /// <param name="fakePeers">The fake peers.</param>
        [Theory]
        [InlineData("FakePeer1", "FakePeer2")]
        [InlineData("FakePeer1002", "FakePeer6000", "FakePeerSataoshi")]
        public async Task TestRemovePeerWithoutPublicKey(params string[] fakePeers) { await ExecuteTestCase(fakePeers, false); }

        /// <summary>Executes the test case.</summary>
        /// <param name="fakePeers">The fake peers.</param>
        /// <param name="withPublicKey">if set to <c>true</c> [send message to handler with the public key].</param>
        private async Task ExecuteTestCase(IReadOnlyCollection<string> fakePeers, bool withPublicKey)
        {
            var peerRepository = new InMemoryRepository<Peer>();

            fakePeers.ToList().ForEach(fakePeer =>
            {
                var peer = new Peer
                {
                    Reputation = 0,
                    LastSeen = DateTime.Now.Subtract(TimeSpan.FromSeconds(fakePeers.ToList().IndexOf(fakePeer))),
                    PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier(fakePeer)
                };

                peerRepository.Add(peer);
            });

            peerRepository.GetAll().Count().Should().Be(fakePeers.Count);

            // Build a fake remote endpoint
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            var messageFactory = new MessageFactory();
            var sendPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var peerToDelete = peerRepository.Get(1);
            var requestMessage = messageFactory.GetMessage(new MessageDto(
                new RemovePeerRequest
                {
                    PeerIp = peerToDelete.PeerIdentifier.Ip.To16Bytes().ToByteString(),
                    PublicKey = withPublicKey ? peerToDelete.PeerIdentifier.PublicKey.ToByteString() : ByteString.Empty
                },
                MessageTypes.Ask,
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                sendPeerIdentifier
            ));

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage);

            var handler = new RemovePeerRequestHandler(sendPeerIdentifier, peerRepository, _logger, messageFactory);
            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count().Should().Be(1);

            var sentResponse = (ProtocolMessage) receivedCalls[0].GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(RemovePeerResponse.Descriptor.ShortenedFullName());

            var responseContent = sentResponse.FromAnySigned<RemovePeerResponse>();

            responseContent.DeletedCount.Should().Be(withPublicKey ? 1 : (uint) fakePeers.Count);
        }
    }
}
