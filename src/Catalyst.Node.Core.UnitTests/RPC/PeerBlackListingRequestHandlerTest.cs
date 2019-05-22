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
using System.Net;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
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
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Rpc;
using Nethereum.RLP;
using Catalyst.Common.Util;

namespace Catalyst.Node.Core.UnitTest.RPC
{
    /// <summary>
    /// Tests the peer black listing calls
    /// </summary>
    public sealed class PeerBlackListingRequestHandlerTest
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        private readonly IRpcCorrelationCache _subbedCorrelationCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerBlackListingRequestHandlerTest"/> class.
        /// </summary>
        public PeerBlackListingRequestHandlerTest()
        {
            _subbedCorrelationCache = Substitute.For<IRpcCorrelationCache>();
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        /// <summary>
        /// Tests the peer black listing request and response via RPC.
        /// Peer is expected to be found in this case
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose black listing flag we wish to adjust</param>
        /// <param name="ipAddress">Ip address of the peer whose black listing flag we wish to adjust</param>
        /// <param name="blackList">Black listing flag</param>
        [Theory]
        [InlineData("highscored-125\0\0\0\0\0\0", "192.168.0.125", "true")]
        [InlineData("highscored-126\0\0\0\0\0\0", "192.168.0.126", "true")]
        public void TestPeerBlackListingRequestResponse(string publicKey, string ipAddress, string blackList)
        {
            var responseContent = ApplyBlackListingToPeerTest(publicKey, ipAddress, blackList);

            responseContent.Blacklist.Should().BeTrue();
        }

        /// <summary>
        /// Tests the peer black listing request and response via RPC.
        /// Peer is NOT expected to be found in this case, as they do not exist
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose black listing flag we wish to adjust</param>
        /// <param name="ipAddress">Ip address of the peer whose black listing flag we wish to adjust</param>
        /// <param name="blackList">Black listing flag</param>
        [Theory]
        [InlineData("cne2+eRandomValuebeingusedherefprtestingIOp", "192.200.200.22", "true")]
        [InlineData("cne2+e5gIfEdfhDWUxkUfr886YuiZnhEj3om5AXmWVXJK7d47/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIOp", "192.111.100.26", "true")]
        public void TestPeerBlackListingRequestResponseForNonExistantPeers(string publicKey, string ipAddress, string blackList)
        {
            var responseContent = ApplyBlackListingToPeerTest(publicKey, ipAddress, blackList);

            responseContent.Blacklist.Should().Be(false);
            responseContent.Ip.Should().BeNullOrEmpty();
            responseContent.PublicKey.Should().BeNullOrEmpty();
        }

        private GetPeerBlackListResponse ApplyBlackListingToPeerTest(string publicKey, string ipAddress, string blacklist)
        {
            var peerRepository = Substitute.For<IRepository<Peer>>();

            var fakePeers = Enumerable.Range(0, 5).Select(i => new Peer
            {
                Reputation = 0, PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"iamgroot-{i}")
            }).ToList();

            //peers we are interested in
            fakePeers.AddRange(Enumerable.Range(125, 2).Select(i => new Peer
            {
                Reputation = 125, PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"highscored-{i}", "Tc", 1, IPAddress.Parse("192.168.0." + i))
            }));

            // Let peerRepository return the fake peer list
            peerRepository.GetAll().Returns(fakePeers.ToArray());

            // Build a fake remote endpoint
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            var sendPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var rpcMessageFactory = new RpcMessageFactory(_subbedCorrelationCache);
            var request = new GetPeerBlackListRequest
            {
                PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString(),
                Ip = ipAddress.ToBytesForRLPEncoding().ToByteString(),
                Blacklist = Convert.ToBoolean(blacklist)
            };

            var requestMessage = rpcMessageFactory.GetMessage(new MessageDto(
                request,
                MessageTypes.Ask,
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                sendPeerIdentifier
            ));

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage);
            var subbedCache = Substitute.For<IRpcCorrelationCache>();

            var handler = new PeerBlackListingRequestHandler(sendPeerIdentifier, _logger, subbedCache, peerRepository);
            handler.StartObserving(messageStream);

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponse = (AnySigned) receivedCalls[0].GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(GetPeerBlackListResponse.Descriptor.ShortenedFullName());

            return sentResponse.FromAnySigned<GetPeerBlackListResponse>();
        }
    }
}
