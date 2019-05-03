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
using System.IO;
using Autofac;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    /// <summary>
    /// Tests the PeerReputationRequestHandler
    /// </summary>
    public sealed class PeerReputationRequestHandlerTest
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerReputationRequestHandlerTest"/> class.
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
        /// </summary>
        /// <param name="fakePeeers">The fake peers.</param>
        [Theory]
        [InlineData("mFRT+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7d19/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIFR", "192.251.12.45")]
        [InlineData("cve2+e5gIfEdfhDWUxkUfr886YuiZnhEj3om5AXmWVXJK7d47/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIOp", "192.104.100.36")]
        public void TestPeerReputationRequestResponse(string publicKey, string ipAddress)
        {
            var peerRepository = Substitute.For<IRepository<Peer>>();

            var fakePeers = new List<Peer>
            {
                new Peer {Reputation = 0, PeerIdentifierHelper.GetPeerIdentifier("im_a_random_string_for_public_key"), ), LastSeen = DateTime.Now},
                new Peer {Reputation = 0, PeerIdentifier = new PeerIdentifier("mL9Z+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIAw".ToUtf8ByteString().ToArray(), IPAddress.Parse("192.154.12.2"), 3542), LastSeen = DateTime.Now},
                new Peer {Reputation = 4, PeerIdentifier = new PeerIdentifier("mFRT+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7d19/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIFR".ToUtf8ByteString().ToArray(), IPAddress.Parse("192.251.12.45"), 1258), LastSeen = DateTime.Now},
                new Peer {Reputation = 2, PeerIdentifier = new PeerIdentifier("bL4Z+e0gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm7EPSjJ2YicTIcXvfzIAw".ToUtf8ByteString().ToArray(), IPAddress.Parse("192.154.12.2"), 1114), LastSeen = DateTime.Now},
                new Peer {Reputation = 0, PeerIdentifier = new PeerIdentifier("cdfZ+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7d25/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzISa".ToUtf8ByteString().ToArray(), IPAddress.Parse("209.154.12.2"), 56895), LastSeen = DateTime.Now},
                new Peer {Reputation = 1, PeerIdentifier = new PeerIdentifier("p8lg+e5gIfEdfhDWUxkUox886YuiZnhEj3om5AXmWVXJK7dl7/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIBn".ToUtf8ByteString().ToArray(), IPAddress.Parse("208.164.78.15"), 6985), LastSeen = DateTime.Now},
                new Peer {Reputation = 4, PeerIdentifier = new PeerIdentifier("cve2+e5gIfEdfhDWUxkUfr886YuiZnhEj3om5AXmWVXJK7d47/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIOp".ToUtf8ByteString().ToArray(), IPAddress.Parse("192.104.100.36"), 7482), LastSeen = DateTime.Now}
            };

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

            var sentResponse = (AnySigned) receivedCalls[0].GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(GetPeerReputationResponse.Descriptor.ShortenedFullName());

            var responseContent = sentResponse.FromAnySigned<GetPeerReputationResponse>();

            responseContent.Reputation.Should().Be(4);
        }
    }
}
