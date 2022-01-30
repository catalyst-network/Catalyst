#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using MultiFormats;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Network;

namespace Catalyst.Core.Modules.Rpc.Server.Tests.UnitTests.IO.Observers
{
    public sealed class PeerReputationRequestObserverTests
    {
        private ILogger _logger;
        private IChannelHandlerContext _fakeContext;
        private TestScheduler _testScheduler;
        private IPeerRepository _peerRepository;
        private MultiAddress _senderId;

        [SetUp]
        public void Init()
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

            _senderId = MultiAddressHelper.GetAddress("sender");
        }

        private static Peer[] PreparePeerRepositoryContent()
        {
            var knownPeers = Enumerable.Range(0, 5).Select(i => new Peer
            {
                Reputation = i,
                Address = MultiAddressHelper.GetAddress($"peer-{i}")
            });

            var fakePeers = knownPeers.ToArray();
            return fakePeers;
        }

        [TestCase("peer-1", 1)]
        [TestCase("peer-4", 4)]
        [TestCase("unknown", int.MinValue)]
        public void TestPeerReputationRequestResponse(string publicKeySeed, int expectedReputations)
        {
            var peerId = MultiAddressHelper.GetAddress(publicKeySeed);

            var request = new GetPeerReputationRequest { Address = peerId.ToString() };

            var responseContent = GetGetPeerReputationResponse(request);

            responseContent.Reputation.Should().Be(expectedReputations);
        }

        private GetPeerReputationResponse GetGetPeerReputationResponse(GetPeerReputationRequest request)
        {
            var protocolMessage = request.ToProtocolMessage(_senderId);
            var messageStream =
                MessageStreamHelper.CreateStreamWithMessage(_fakeContext, _testScheduler, protocolMessage);

            var peerSettings = _senderId.ToSubstitutedPeerSettings();
            PeerReputationRequestObserver handler = new(peerSettings, _logger, _peerRepository);
            handler.StartObserving(messageStream);

            _testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();

            return sentResponseDto.Content.FromProtocolMessage<GetPeerReputationResponse>();
        }
    }
}
