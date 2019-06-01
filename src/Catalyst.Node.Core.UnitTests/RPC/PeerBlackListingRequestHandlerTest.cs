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
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC
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
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PeerBlackListingRequestHandlerTest"/> class.
        /// </summary>
        public PeerBlackListingRequestHandlerTest()
        {
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
        [InlineData("highscored-14\0\0\0\0\0\0\0", "198.51.100.14", "true")]
        [InlineData("highscored-22\0\0\0\0\0\0\0", "198.51.100.22", "true")]
        public void TestPeerBlackListingRequestResponse(string publicKey, string ipAddress, string blackList)
        {
            var responseContent = ApplyBlackListingToPeerTest(publicKey, ipAddress, blackList);

            responseContent.Blacklist.Should().BeTrue();
            responseContent.Ip.ToStringUtf8().Should().Be(ipAddress);
            responseContent.PublicKey.ToStringUtf8().Should().Be(publicKey);
        }

        /// <summary>
        /// Tests the peer black listing request and response via RPC.
        /// Peer is NOT expected to be found in this case, as they do not exist
        /// </summary>
        /// <param name="publicKey">Public key of the peer whose black listing flag we wish to adjust</param>
        /// <param name="ipAddress">Ip address of the peer whose black listing flag we wish to adjust</param>
        /// <param name="blackList">Black listing flag</param>
        [Theory]
        [InlineData("cne2+eRandomValuebeingusedherefprtestingIOp", "198.51.100.11", "true")]
        [InlineData("cne2+e5gIfEdfhDWUxkUfr886YuiZnhEj3om5AXmWVXJK7d47/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIOp", "198.51.100.5", "true")]
        public void TestPeerBlackListingRequestResponseForNonExistantPeers(string publicKey, string ipAddress, string blackList)
        {
            var responseContent = ApplyBlackListingToPeerTest(publicKey, ipAddress, blackList);

            responseContent.Blacklist.Should().Be(false);
            responseContent.Ip.Should().BeNullOrEmpty();
            responseContent.PublicKey.Should().BeNullOrEmpty();
        }

        private SetPeerBlackListResponse ApplyBlackListingToPeerTest(string publicKey, string ipAddress, string blacklist)
        {
            var peerRepository = Substitute.For<IRepository<Peer>>();

            var fakePeers = Enumerable.Range(0, 5).Select(i => new Peer
            {
                Reputation = 0, PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"iamgroot-{i}")
            }).ToList();

            //peers we are interested in
            fakePeers.AddRange(Enumerable.Range(0, 23).Select(i => new Peer
            {
                Reputation = 125, PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"highscored-{i}", "Tc", 1, IPAddress.Parse("198.51.100." + i))
            }));

            // Let peerRepository return the fake peer list
            peerRepository.GetAll().Returns(fakePeers.ToArray());

            // Build a fake remote endpoint
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            var sendPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var messageFactory = new MessageFactory();
            var request = new SetPeerBlackListRequest
            {
                PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString(),
                Ip = ipAddress.ToBytesForRLPEncoding().ToByteString(),
                Blacklist = Convert.ToBoolean(blacklist)
            };

            var requestMessage = messageFactory.GetMessage(new MessageDto(
                request,
                MessageTypes.Ask,
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                sendPeerIdentifier
            ));

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage);

            var handler = new PeerBlackListingRequestHandler(sendPeerIdentifier, _logger, peerRepository);
            handler.StartObserving(messageStream);

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponse = (AnySigned) receivedCalls[0].GetArguments().Single();
            sentResponse.TypeUrl.Should().Be(SetPeerBlackListResponse.Descriptor.ShortenedFullName());

            return sentResponse.FromAnySigned<SetPeerBlackListResponse>();
        }
    }
}
