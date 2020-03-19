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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Network;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    /// <summary>
    ///     Tests remove peer CLI and RPC calls
    /// </summary>
    public sealed class RemovePeerRequestObserverTests
    {
        /// <summary>The logger</summary>
        private readonly ILogger _logger;

        /// <summary>The fake channel context</summary>
        private readonly IChannelHandlerContext _fakeContext;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see>
        ///         <cref>RemovePeerRequestObserverTest</cref>
        ///     </see>
        ///     class.
        /// </summary>
        public RemovePeerRequestObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var fakeChannel = Substitute.For<IChannel>();
            _fakeContext.Channel.Returns(fakeChannel);
        }

        /// <summary>
        ///     Tests the peer list request and response.
        /// </summary>
        /// <param name="fakePeers">The fake peers.</param>
        [Theory]
        [TestCase("FakePeer1", "FakePeer2")]
        [TestCase("FakePeer1002", "FakePeer6000", "FakePeerSataoshi")]
        public void TestRemovePeer(params string[] fakePeers) { ExecuteTestCase(fakePeers, true); }

        /// <summary>
        ///     Tests peer removal via IP only.
        /// </summary>
        /// <param name="fakePeers">The fake peers.</param>
        [Theory]
        [TestCase("Fake1Peer1", "Fake2Peer2")]
        [TestCase("Fake1Peer1002", "Fake2Peer6000", "FakePeer3Sataoshi")]
        public void TestRemovePeerWithoutPublicKey(params string[] fakePeers) { ExecuteTestCase(fakePeers, false); }

        /// <summary>Executes the test case.</summary>
        /// <param name="fakePeers">The fake peers.</param>
        /// <param name="withPublicKey">if set to <c>true</c> [send message to handler with the public key].</param>
        private void ExecuteTestCase(IReadOnlyCollection<string> fakePeers, bool withPublicKey)
        {
            var testScheduler = new TestScheduler();
            IPeerRepository peerRepository = new PeerRepository(new InMemoryRepository<Peer, string>());
            var fakePeerList = fakePeers.ToList().Select(fakePeer =>
            {
                return new Peer
                {
                    Reputation = 0,
                    LastSeen = DateTime.Now.Subtract(TimeSpan.FromSeconds(fakePeers.ToList().IndexOf(fakePeer))),
                    PeerId = PeerIdHelper.GetPeerId(fakePeer)
                };
            }).ToList();

            var targetPeerToDelete = fakePeerList[0];

            peerRepository.Add(fakePeerList);

            // Build a fake remote endpoint
            _fakeContext.Channel.RemoteAddress.Returns(EndpointBuilder.BuildNewEndPoint("192.0.0.1", 42042));

            var peerId = PeerIdHelper.GetPeerId("sender");

            var removePeerRequest = new RemovePeerRequest
            {
                PeerIp = targetPeerToDelete.PeerId.Ip,
                PublicKey = withPublicKey ? targetPeerToDelete.PeerId.PublicKey : ByteString.Empty
            };

            var protocolMessage = removePeerRequest.ToProtocolMessage(peerId);

            var messageStream =
                MessageStreamHelper.CreateStreamWithMessage(_fakeContext, testScheduler, protocolMessage);

            var peerSettings = peerId.ToSubstitutedPeerSettings();
            var handler = new RemovePeerRequestObserver(peerSettings, peerRepository, _logger);
            handler.StartObserving(messageStream);

            testScheduler.Start();

            var receivedCalls = _fakeContext.Channel.ReceivedCalls().ToList();
            receivedCalls.Count().Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls[0].GetArguments().Single();

            var signResponseMessage = sentResponseDto.Content.FromProtocolMessage<RemovePeerResponse>();

            signResponseMessage.DeletedCount.Should().Be(withPublicKey ? 1 : (uint) fakePeers.Count);
        }
    }
}
