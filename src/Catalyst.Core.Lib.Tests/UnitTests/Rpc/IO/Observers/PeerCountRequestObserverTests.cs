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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Abstractions.P2P;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    /// <summary>
    ///     Tests the peer count CLI and RPC calls
    /// </summary>
    public sealed class PeerCountRequestObserverTests
    {
        private TestScheduler _testScheduler;

        /// <summary>The logger</summary>
        private ILogger _logger;

        private IPeerClient _peerClient;

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
            _testScheduler = new TestScheduler();
            _logger = Substitute.For<ILogger>();
            _peerClient = Substitute.For<IPeerClient>();
        }

        /// <summary>
        ///     Tests the peer count request and response.
        /// </summary>
        /// <param name="fakePeers">The peer count.</param>
        [TestCase(40)]
        [TestCase(20)]
        public void TestPeerListRequestResponse(int fakePeers)
        {
            var peerService = Substitute.For<IPeerRepository>();
            var peerList = new List<Peer>();

            for (var i = 0; i < fakePeers; i++)
            {
                peerList.Add(new Peer
                {
                    Reputation = 0,
                    LastSeen = DateTime.Now,
                    Address = MultiAddressHelper.GetAddress(i.ToString())
                });
            }

            peerService.GetAll().Returns(peerList);

            var sendPeerId = MultiAddressHelper.GetAddress("sender");

            var protocolMessage =
                new GetPeerCountRequest().ToProtocolMessage(MultiAddressHelper.GetAddress("sender"));
            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_testScheduler, protocolMessage);

            var peerSettings = sendPeerId.ToSubstitutedPeerSettings();
            var handler = new PeerCountRequestObserver(peerSettings, _peerClient, peerService, _logger);
            handler.StartObserving(messageStream);

            _testScheduler.Start();

            var receivedCalls = _peerClient.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);

            var sentResponse = (ProtocolMessage) receivedCalls[0].GetArguments().First();

            var responseContent = sentResponse.FromProtocolMessage<GetPeerCountResponse>();

            responseContent.PeerCount.Should().Be(fakePeers);
        }
    }
}
