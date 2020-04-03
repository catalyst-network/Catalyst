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
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Dto;
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

        public PeerChallengeRequestTests() : base(TestContext.CurrentContext)
        {

        }

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
            var recipientPeerId = PeerIdHelper.GetPeerId();
            await _peerChallengeRequest.ChallengePeerAsync(recipientPeerId).ConfigureAwait(false);
            var expectedDto = Substitute.For<IMessageDto<ProtocolMessage>>();
            expectedDto.RecipientPeerIdentifier.Returns(recipientPeerId);
            expectedDto.SenderPeerIdentifier.Returns(_testSettings.PeerId);
            _peerChallengeRequest.PeerClient.ReceivedWithAnyArgs(1).SendMessage(Arg.Is(expectedDto));
        }
        
        [Test]
        public async Task Can_Receive_Query_Response_On_Observer()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            var challengeResposne = new PeerChallengeResponse(recipientPeerId);

            _peerChallengeRequest.ChallengeResponseMessageStreamer.OnNext(challengeResposne);
            var response = await _peerChallengeRequest.ChallengePeerAsync(recipientPeerId).ConfigureAwait(false);
            response.Should().BeTrue();
        }

        [Test]
        public async Task No_Response_Timeout_And_Returns_False()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            _cancellationProvider.CancellationTokenSource.Cancel();
            var response = await _peerChallengeRequest.ChallengePeerAsync(recipientPeerId).ConfigureAwait(false);
            response.Should().BeFalse();
        }

        [Test]
        public async Task Exception_During_Query_Returns_Null()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            _cancellationProvider.Dispose(); //do summet nasty to force exception
            var response = await _peerChallengeRequest.ChallengePeerAsync(recipientPeerId).ConfigureAwait(false);
            response.Should().BeFalse();   
        }
    }
}
