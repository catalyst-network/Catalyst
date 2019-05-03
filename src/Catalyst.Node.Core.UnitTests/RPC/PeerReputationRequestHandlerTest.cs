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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;
using SharpRepository.Repository;
using Catalyst.Common.P2P;
using Catalyst.Common.Network;
using System.Collections.Generic;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using System.Net;
using Nethereum.RLP;
using Catalyst.Common.Util;




namespace Catalyst.Node.Core.UnitTest.RPC
{
    /// <summary>
    /// Tests the peer reputation calls
    /// </summary>
    public sealed class PeerReputationRequestHandlerTest
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerListRequestHandlerTest"/> class.
        /// </summary>
        public PeerReputationRequestHandlerTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        /// <summary>
        /// Tests the peer reputation request and response via RPC.
        /// Peer is expected to be found in this case
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        [Theory]
        [InlineData("highscored-125\0\0\0\0\0\0", "192.168.0.125")]
        [InlineData("highscored-126\0\0\0\0\0\0", "192.168.0.126")]
        public void TestPeerReputationRequestResponse(string publicKey, string ipAddress)
        {
            var responseContent = GetPeerReputationTest(publicKey, ipAddress);

            responseContent.Reputation.Should().Be(125);
        }

        /// <summary>
        /// Tests the peer reputation request and response via RPC.
        /// Peer is NOT expected to be found in this case, as they do not exist
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose reputation is of interest</param>
        /// <param name="ipAddress">Ip address of the peer whose reputation is of interest</param>
        [Theory]
        [InlineData("cne2+eRandomValuebeingusedherefprtestingIOp", "192.200.200.22")]
        [InlineData("cne2+e5gIfEdfhDWUxkUfr886YuiZnhEj3om5AXmWVXJK7d47/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIOp", "192.111.100.26")]
        public void TestPeerReputationRequestResponseForNonExistantPeers(string publicKey, string ipAddress)
        {
            var responseContent = GetPeerReputationTest(publicKey, ipAddress);

            responseContent.Reputation.Should().Be(int.MinValue);
        }

        private GetPeerReputationResponse GetPeerReputationTest(string publicKey, string ipAddress)
        {
            var peerRepository = Substitute.For<IRepository<Peer>>();

            var fakePeers = Enumerable.Range(0, 5).Select(i => new Peer() { Reputation = 0, PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"iamgroot-{i}") }).ToList();

            //peers we are interested in
            fakePeers.AddRange(Enumerable.Range(125, 2).Select(i => new Peer() { Reputation = 125, PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"highscored-{i}", "Tc", 1, IPAddress.Parse("192.168.0." + i), 12345) }));

            // Let peerRepository return the fake peer list
            peerRepository.GetAll().Returns(fakePeers.ToArray());

            // Build a fake remote endpoint
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            var peerDiscovery = Substitute.For<IPeerDiscovery>();
            peerDiscovery.PeerRepository.Returns(peerRepository);

            var sendPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var rpcMessageFactory = new RpcMessageFactory<GetPeerReputationRequest, RpcMessages>();
            var request = new GetPeerReputationRequest
            {
                PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString(),
                Ip = ipAddress.ToBytesForRLPEncoding().ToByteString()
            };

            var requestMessage = rpcMessageFactory.GetMessage(new MessageDto<GetPeerReputationRequest, RpcMessages>
            (
                type: RpcMessages.GetPeerReputationRequest,
                message: request,
                recipient: PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                sender: sendPeerIdentifier
            ));

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage);
            var subbedCache = Substitute.For<IMessageCorrelationCache>();

            var handler = new PeerReputationRequestHandler(sendPeerIdentifier, _logger, subbedCache, peerDiscovery);
            handler.StartObserving(messageStream);


            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count().Should().Be(1);

            var sentResponse = (AnySigned)receivedCalls[0].GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(GetPeerReputationResponse.Descriptor.ShortenedFullName());

            return sentResponse.FromAnySigned<GetPeerReputationResponse>();
        }
    }
}
