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

using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Node.Core.RPC.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC.Observables
{
    /// <summary>
    /// Tests the peer reputation calls
    /// </summary>
    public sealed class PeerReputationRequestObserverTest
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerListRequestObserverTest"/> class.
        /// </summary>
        public PeerReputationRequestObserverTest()
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
        [InlineData("highscored-125\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "192.168.0.125")]
        [InlineData("highscored-126\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "192.168.0.126")]
        public async Task TestPeerReputationRequestResponse(string publicKey, string ipAddress)
        {
            var responseContent = await GetPeerReputationTest(publicKey, ipAddress);

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
        public async Task TestPeerReputationRequestResponseForNonExistantPeers(string publicKey, string ipAddress)
        {
            var responseContent = await GetPeerReputationTest(publicKey, ipAddress);

            responseContent.Reputation.Should().Be(int.MinValue);
        }

        private async Task<GetPeerReputationResponse> GetPeerReputationTest(string publicKey, string ipAddress)
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

            var messageFactory = new DtoFactory();
            var request = new GetPeerReputationRequest
            {
                PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString(),
                Ip = ipAddress.ToBytesForRLPEncoding().ToByteString()
            };

            var requestMessage = messageFactory.GetDto(
                request,
                sendPeerIdentifier,
                PeerIdentifierHelper.GetPeerIdentifier("recipient")
            );

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, requestMessage.Message.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId));

            var handler = new PeerReputationRequestObserver(sendPeerIdentifier, _logger, peerRepository);
            handler.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto) receivedCalls[0].GetArguments().Single();
            sentResponseDto.Message.Descriptor.ShortenedFullName().Should().Be(GetPeerReputationResponse.Descriptor.ShortenedFullName());

            return sentResponseDto.FromIMessageDto<GetPeerReputationResponse>();
        }
    }
}
