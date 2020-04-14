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
using System.Threading.Tasks;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Protocols;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Modules.POA.P2P.Discovery;
using Catalyst.TestUtils;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using FluentAssertions;

namespace Catalyst.Modules.POA.P2P.Tests.UnitTests
{
    public sealed class PeerHeartbeatCheckerTests : IDisposable
    {
        private const int PeerHeartbeatCheckSeconds = 3;
        private const int PeerChallengeTimeoutSeconds = 1;
        private IHealthChecker _peerHeartbeatChecker;
        private IPeerClient _peerClient;
        private IPeerRepository _peerRepository;
        private Peer _testPeer;

        [SetUp]
        public void Init()
        {
            _peerRepository = Substitute.For<IPeerRepository>();
            _peerClient = Substitute.For<IPeerClient>();
            _testPeer = new Peer
            {
                PeerId = PeerIdHelper.GetPeerId("TestPeer")
            };
        }

        //[Fact]
        //public async Task Can_Remove_Peer_On_Non_Responsive_Heartbeat()
        //{
        //    await RunHeartbeatChecker().ConfigureAwait(false);
        //    _peerRepository.Received().Delete(_testPeer.DocumentId);
        //}

        //[Fact]
        //public async Task Can_Keep_Peer_On_Valid_Heartbeat_Response()
        //{
        //    await RunHeartbeatChecker(true).ConfigureAwait(false);
        //    _peerRepository.DidNotReceive().Delete(_testPeer.DocumentId);
        //}

        [Test]
        public async Task Can_Set_Peer_Awol_To_False_On_Non_Responsive_Heartbeat()
        {
            await RunHeartbeatChecker().ConfigureAwait(false);
            _testPeer.IsAwolPeer.Should().BeTrue();
        }

        [Test]
        public async Task Can_Keep_Peer_On_Valid_Heartbeat_Response()
        {
            await RunHeartbeatChecker(true).ConfigureAwait(false);
            _testPeer.IsAwolPeer.Should().BeFalse();
        }

        [Test]
        public async Task Can_Remove_Peer_On_Max_Counter()
        {
            await RunHeartbeatChecker(maxNonResponsiveCounter: 2).ConfigureAwait(false);
            _testPeer.IsAwolPeer.Should().BeTrue();
        }

        private async Task RunHeartbeatChecker(bool sendResponse = false, int maxNonResponsiveCounter = 1)
        {
            var peers = new List<Peer> {_testPeer};
            var peerSettings = _testPeer.PeerId.ToSubstitutedPeerSettings();
            var peerChallenger = new PeerChallengeRequest(
                Substitute.For<ILogger>(),
                _peerClient,
                peerSettings,
                PeerChallengeTimeoutSeconds);

            if (sendResponse)
            {
                peerChallenger.ChallengeResponseMessageStreamer.OnNext(new PeerChallengeResponse(_testPeer.PeerId));
            }

            _peerRepository.GetAll().Returns(peers);
            _peerHeartbeatChecker = new PeerHeartbeatChecker(
                Substitute.For<ILogger>(),
                _peerRepository,
                peerChallenger,
                PeerHeartbeatCheckSeconds,
                maxNonResponsiveCounter);

            _peerHeartbeatChecker.Run();
            await Task.Delay(TimeSpan.FromSeconds(PeerHeartbeatCheckSeconds * (maxNonResponsiveCounter + 1))
               .Add(TimeSpan.FromSeconds(1))).ConfigureAwait(false);
        }

        void IDisposable.Dispose() { _peerHeartbeatChecker?.Dispose(); }
    }
}
