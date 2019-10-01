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
using System.Diagnostics;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.P2P;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P
{
    public sealed class PeerQueryTipTests : SelfAwareTestBase
    {
        private readonly IPeerQueryTip _peerQueryTip;
        private readonly IPeerSettings _testSettings;
        private readonly CancellationTokenProvider _cancellationProvider;

        public PeerQueryTipTests(ITestOutputHelper output) : base(output)
        {
            var subbedPeerClient = Substitute.For<IPeerClient>();
            _testSettings = PeerSettingsHelper.TestPeerSettings();
            _cancellationProvider = new CancellationTokenProvider(TimeSpan.FromSeconds(15));
            
            _peerQueryTip = new PeerQueryTip(
                Substitute.For<ILogger>(),
                subbedPeerClient,
                _testSettings,
                _cancellationProvider
            );
        }

        [Fact]
        public async Task Can_Query_Expected_Peer()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            await _peerQueryTip.QueryPeerTipAsync(recipientPeerId);
            var expectedDto = Substitute.For<IMessageDto<ProtocolMessage>>();
            expectedDto.RecipientPeerIdentifier.Returns(recipientPeerId);
            expectedDto.SenderPeerIdentifier.Returns(_testSettings.PeerId);
            _peerQueryTip.PeerClient.ReceivedWithAnyArgs(1).SendMessage(Arg.Is(expectedDto));
        }

        [Fact]
        public async Task Can_Receive_Query_Response_On_Observer()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            var tipQueryResponse = new PeerQueryTipResponse(PeerIdHelper.GetPeerId(),
                Multihash.Sum<BLAKE2B_256>(ByteUtil.GenerateRandomByteArray(32)));

            _peerQueryTip.QueryTipResponseMessageStreamer.OnNext(tipQueryResponse);
            var response = await _peerQueryTip.QueryPeerTipAsync(recipientPeerId);
            response.Should().BeTrue();
        }

        [Fact]
        public async Task No_Response_Timeout_And_Returns_False()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            _cancellationProvider.CancellationTokenSource.Cancel();
            var response = await _peerQueryTip.QueryPeerTipAsync(recipientPeerId);
            response.Should().BeFalse();
        }

        [Fact]
        public async Task Exception_During_Query_Returns_Null()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            _cancellationProvider.Dispose(); //do summet nasty to force exception
            var response = await _peerQueryTip.QueryPeerTipAsync(recipientPeerId);
            response.Should().BeFalse();   
        }

        [Fact]
        public async Task Can_Dispose_Class()
        {
            using (_peerQueryTip)
            {
                Debug.Assert(!_peerQueryTip.Disposing); // Best not be disposed yet.
            }

            try
            {
                Debug.Assert(_peerQueryTip.Disposing); // Expecting an exception.
            }
            catch (Exception ex)
            {
                Debug.Assert(ex is ObjectDisposedException); // Better be the right one.
            }
        }
    }
}
