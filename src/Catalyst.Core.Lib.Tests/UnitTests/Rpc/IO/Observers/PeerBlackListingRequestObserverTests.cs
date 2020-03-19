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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Network;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    /// <summary>
    ///     Tests the peer black listing calls
    /// </summary>
    public sealed class PeerBlackListingRequestObserverTests
    {
        private readonly ILogger _logger;

        private readonly IChannelHandlerContext _fakeContext;

        private readonly TestScheduler _testScheduler;
        private readonly PeerId _senderId;
        private readonly IPeerRepository _peerRepository;

        public PeerBlackListingRequestObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();

            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            _testScheduler = new TestScheduler();
            _peerRepository = Substitute.For<IPeerRepository>();

            var fakePeers = PreparePeerRepositoryContent();
            _peerRepository.GetAll().Returns(fakePeers);

            _senderId = PeerIdHelper.GetPeerId("sender");
        }

        private static Peer[] PreparePeerRepositoryContent()
        {
            var blacklistedPeers = Enumerable.Range(0, 5).Select(i => new Peer
            {
                Reputation = 0,
                PeerId = PeerIdHelper.GetPeerId($"blacklisted-{i}"),
                BlackListed = true
            });
            var goodPeers = Enumerable.Range(0, 23).Select(i => new Peer
            {
                Reputation = 125,
                PeerId = PeerIdHelper.GetPeerId($"good-{i}")
            });

            var fakePeers = blacklistedPeers.Concat(goodPeers).ToArray();
            return fakePeers;
        }

        [Theory]
        [TestCase("good-14", true)]
        [TestCase("good-22", false)]
        [TestCase("blacklisted-1", true)]
        [TestCase("blacklisted-3", false)]
        public void PeerBlackListingRequestObserver_should_set_Blacklist_flag_on_known_peers(string publicKeySeed,
            bool blacklist)
        {
            var targetedId = PeerIdHelper.GetPeerId(publicKeySeed);
            var request = new SetPeerBlacklistRequest
            {
                PublicKey = targetedId.PublicKey,
                Ip = targetedId.Ip,
                Blacklist = blacklist
            };

            var responseContent = GetSetPeerBlacklistRequest(request);

            responseContent.Blacklist.Should().Be(blacklist);
            responseContent.Ip.Should().BeEquivalentTo(targetedId.Ip);
            responseContent.PublicKey.Should().BeEquivalentTo(targetedId.PublicKey);
        }

        [Theory]
        [TestCase("unknown-1", false)]
        [TestCase("unknown-2", false)]
        public void PeerBlackListingRequestObserver_should_not_set_Blacklist_flag_on_unknown_peers(string publicKeySeed,
            bool blacklist)
        {
            var targetedId = PeerIdHelper.GetPeerId(publicKeySeed);
            var request = new SetPeerBlacklistRequest
            {
                PublicKey = targetedId.PublicKey,
                Ip = targetedId.Ip,
                Blacklist = blacklist
            };

            var responseContent = GetSetPeerBlacklistRequest(request);

            responseContent.Ip.Should().BeNullOrEmpty();
            responseContent.PublicKey.Should().BeNullOrEmpty();
        }

        private SetPeerBlacklistResponse GetSetPeerBlacklistRequest(SetPeerBlacklistRequest request)
        {
            var protocolMessage = request.ToProtocolMessage(_senderId);
            var messageStream =
                MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler, protocolMessage);

            var peerSettings = _senderId.ToSubstitutedPeerSettings();
            var handler = new PeerBlackListingRequestObserver(peerSettings, _logger, _peerRepository);
            handler.StartObserving(messageStream);

            _testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();

            return sentResponseDto.Content.FromProtocolMessage<SetPeerBlacklistResponse>();
        }
    }
}
