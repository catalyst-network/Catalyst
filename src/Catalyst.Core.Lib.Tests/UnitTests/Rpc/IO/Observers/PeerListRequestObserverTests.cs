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
using Catalyst.Abstractions.P2P;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    /// <summary>
    ///     Tests the peer list CLI and RPC calls
    /// </summary>
    public sealed class PeerListRequestObserverTests
    {
        /// <summary>The logger</summary>
        private ILogger _logger;

        /// <summary>The fake channel context</summary>
        private IChannelHandlerContext _fakeContext;

        private ILibP2PPeerClient _peerClient;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see>
        ///         <cref>PeerListRequestObserverTest</cref>
        ///     </see>
        ///     class.
        /// </summary>
        [SetUp]
        public void Init()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _peerClient = Substitute.For<ILibP2PPeerClient>();
        }

        /// <summary>
        ///     Tests the peer list request and response.
        /// </summary>
        /// <param name="fakePeers">The fake peers.</param>
        [TestCase("FakePeer1", "FakePeer2")]
        [TestCase("FakePeer1002", "FakePeer6000", "FakePeerSataoshi")]
        public void TestPeerListRequestResponse(params string[] fakePeers)
        {
            var testScheduler = new TestScheduler();
            var peerService = Substitute.For<IPeerRepository>();
            var peerList = new List<Peer>();

            fakePeers.ToList().ForEach(fakePeer =>
            {
                peerList.Add(new Peer
                {
                    Reputation = 0,
                    LastSeen = DateTime.Now,
                    Address = MultiAddressHelper.GetAddress(fakePeer)
                });
            });

            // Let peerRepository return the fake peer list
            peerService.GetAll().Returns(peerList.ToArray());

            var protocolMessage = new GetPeerListRequest().ToProtocolMessage(MultiAddressHelper.GetAddress("sender"));
            var messageStream =
                MessageStreamHelper.CreateStreamWithMessage(_fakeContext, testScheduler, protocolMessage);

            var peerSettings = MultiAddressHelper.GetAddress("sender").ToSubstitutedPeerSettings();
            var handler = new PeerListRequestObserver(peerSettings, _peerClient, _logger, peerService);
            handler.StartObserving(messageStream);

            testScheduler.Start();

            var receivedCalls = _peerClient.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls[0].GetArguments().Single();

            var responseContent = sentResponseDto.Content.FromProtocolMessage<GetPeerListResponse>();

            responseContent.Peers.Count.Should().Be(fakePeers.Length);
        }
    }
}
