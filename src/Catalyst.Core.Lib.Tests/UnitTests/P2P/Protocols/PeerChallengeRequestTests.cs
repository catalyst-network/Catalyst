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

using System;
using System.Threading.Tasks;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Protocols;
using Catalyst.Core.Lib.P2P.Protocols;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using NUnit.Framework;


namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.Protocols
{
    public class PeerChallengeRequestTests : SelfAwareTestBase
    {
        private IPeerChallengeRequest _peerChallengeRequest;
        private IPeerSettings _testSettings;
        private CancellationTokenProvider _cancellationProvider;

        [SetUp]
        public void Init()
        {
            var subbedPeerClient = Substitute.For<IPeerClient>();
            _testSettings = PeerSettingsHelper.TestPeerSettings();
            _cancellationProvider = new CancellationTokenProvider(TimeSpan.FromSeconds(10));

            _peerChallengeRequest = new PeerChallengeRequest(
                Substitute.For<ILogger>(),
                subbedPeerClient,
                _testSettings,
                10
            );
        }

        [Test]
        public async Task Can_Challenge_Peer()
        {
            var recipientAddress = MultiAddressHelper.GetAddress();
            await _peerChallengeRequest.ChallengePeerAsync(recipientAddress).ConfigureAwait(false);
            await _peerChallengeRequest.PeerClient.ReceivedWithAnyArgs(1).SendMessageAsync(Arg.Is<ProtocolMessage>(x => x.Address == _testSettings.Address), Arg.Is(recipientAddress));
        }

        [Test]
        public async Task Can_Receive_Query_Response_On_Observer()
        {
            var recipientAddress = MultiAddressHelper.GetAddress();
            var challengeResposne = new PeerChallengeResponse(recipientAddress);

            _peerChallengeRequest.ChallengeResponseMessageStreamer.OnNext(challengeResposne);
            var response = await _peerChallengeRequest.ChallengePeerAsync(recipientAddress).ConfigureAwait(false);
            response.Should().BeTrue();
        }

        [Test]
        public async Task No_Response_Timeout_And_Returns_False()
        {
            var recipientAddress = MultiAddressHelper.GetAddress();
            _cancellationProvider.CancellationTokenSource.Cancel();
            var response = await _peerChallengeRequest.ChallengePeerAsync(recipientAddress).ConfigureAwait(false);
            response.Should().BeFalse();
        }

        [Test]
        public async Task Exception_During_Query_Returns_Null()
        {
            var recipientAddress = MultiAddressHelper.GetAddress();
            _cancellationProvider.Dispose(); //do summet nasty to force exception
            var response = await _peerChallengeRequest.ChallengePeerAsync(recipientAddress).ConfigureAwait(false);
            response.Should().BeFalse();
        }
    }
}
