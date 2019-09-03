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
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Extensions;
using Catalyst.Core.Network;
using Catalyst.Core.P2P.Models;
using Catalyst.Core.P2P.Repository;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Core.Util;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc.IO.Observers
{
    /// <summary>
    /// Tests the peer black listing calls
    /// </summary>
    public sealed class PeerBlackListingRequestObserverTests
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        /// <summary>
        /// Initializes a new instance of the <see>
        ///     <cref>PeerBlackListingRequestObserverTest</cref>
        /// </see>
        /// class.
        /// </summary>
        public PeerBlackListingRequestObserverTests()
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
        [InlineData("highscored-14\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "198.51.100.14", "true")]
        [InlineData("highscored-22\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "198.51.100.22", "true")]
        public async Task TestPeerBlackListingRequestResponse(string publicKey, string ipAddress, string blackList)
        {
            var responseContent = await ApplyBlackListingToPeerTest(publicKey, ipAddress, blackList);

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
        [InlineData("cne2+eRandomValuebeingusedherefprtestingIOp", "198.51.100.11", "false")]
        [InlineData("cne2+e5gIfEdfhDWUxkUfr886YuiZnhEj3om5AXmWVXJK7d47/ESkjhbkJsrbzIbuWm8EPSjJ2YicTIcXvfzIOp", "198.51.100.5", "false")]
        public async Task TestPeerBlackListingRequestResponseForNonExistantPeers(string publicKey, string ipAddress, string blackList)
        {
            var responseContent = await ApplyBlackListingToPeerTest(publicKey, ipAddress, blackList);

            responseContent.Blacklist.Should().Be(false);
            responseContent.Ip.Should().BeNullOrEmpty();
            responseContent.PublicKey.Should().BeNullOrEmpty();
        }

#pragma warning disable 1998
        private async Task<SetPeerBlackListResponse> ApplyBlackListingToPeerTest(string publicKey, string ipAddress, string blacklist)
#pragma warning restore 1998
        {
            var testScheduler = new TestScheduler();
            var peerRepository = Substitute.For<IPeerRepository>();

            var fakePeers = Enumerable.Range(0, 5).Select(i => new Peer
            {
                Reputation = 0, PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"iamgroot-{i}"),
                BlackListed = Convert.ToBoolean(blacklist)
            }).ToList();

            //peers we are interested in
            fakePeers.AddRange(Enumerable.Range(0, 23).Select(i => new Peer
            {
                Reputation = 125, PeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier($"highscored-{i}",
                    IPAddress.Parse("198.51.100." + i)
                )
            }));

            // Let peerRepository return the fake peer list
            peerRepository.GetAll().Returns(fakePeers.ToArray());

            // Build a fake remote endpoint
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            var sendPeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("sender");

            var request = new SetPeerBlackListRequest
            {
                PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString(),
                Ip = ipAddress.ToBytesForRLPEncoding().ToByteString(),
                Blacklist = Convert.ToBoolean(blacklist)
            };

            var protocolMessage = request.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId);
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, testScheduler,
                protocolMessage
            );

            var handler = new PeerBlackListingRequestObserver(sendPeerIdentifier, _logger, peerRepository);
            handler.StartObserving(messageStream);

            testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);
            
            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();
            
            return sentResponseDto.Content.FromProtocolMessage<SetPeerBlackListResponse>();
        }
    }
}
