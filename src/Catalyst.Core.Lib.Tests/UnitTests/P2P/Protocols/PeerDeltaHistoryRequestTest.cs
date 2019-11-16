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
using System.Diagnostics;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Protocols;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.P2P.Protocols;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using TheDotNetLeague.MultiFormats.MultiHash;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.Protocols
{
    public sealed class PeerDeltaHistoryRequestTest : SelfAwareTestBase
    {
        private readonly IPeerDeltaHistoryRequest _peerDeltaHistoryRequest;
        private readonly IPeerSettings _testSettings;
        private readonly CancellationTokenProvider _cancellationProvider;

        public PeerDeltaHistoryRequestTest(ITestOutputHelper output) : base(output)
        {
            var subbedPeerClient = Substitute.For<IPeerClient>();
            _testSettings = PeerSettingsHelper.TestPeerSettings();
            _cancellationProvider = new CancellationTokenProvider(TimeSpan.FromSeconds(15));
            
            _peerDeltaHistoryRequest = new PeerDeltaHistoryRequest(
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
            await _peerDeltaHistoryRequest.DeltaHistoryAsync(recipientPeerId).ConfigureAwait(false);
            var expectedDto = Substitute.For<IMessageDto<ProtocolMessage>>();
            expectedDto.RecipientPeerIdentifier.Returns(recipientPeerId);
            expectedDto.SenderPeerIdentifier.Returns(_testSettings.PeerId);
            _peerDeltaHistoryRequest.PeerClient.ReceivedWithAnyArgs(1).SendMessage(Arg.Is(expectedDto));
        }

        [Fact]
        public async Task Can_Receive_Query_Response_On_Observer()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            
            var hp = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            var lastDeltaHash = hp.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32));
            
            var collection = new List<DeltaIndex>();

            //// this matches the fake mock 
            for (uint x = 0; x < 10; x++)
            {
                var delta = new Delta
                {
                    PreviousDeltaDfsHash = lastDeltaHash.Digest.ToByteString()
                };

                var index = new DeltaIndex
                {
                    Height = 10,
                    Cid = delta.ToByteString()
                };

                collection.Add(index);
                lastDeltaHash = hp.ComputeMultiHash(ByteUtil.GenerateRandomByteArray(32));
            }
            
            var deltaHistoryResponse = new PeerDeltaHistoryResponse(recipientPeerId, collection);

            _peerDeltaHistoryRequest.DeltaHistoryResponseMessageStreamer.OnNext(deltaHistoryResponse);
            var response = await _peerDeltaHistoryRequest.DeltaHistoryAsync(recipientPeerId).ConfigureAwait(false);
            response.DeltaCid.Count.Should().Be(10);
        }

        [Fact]
        public async Task No_Response_Timeout_And_Returns_False()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            _cancellationProvider.CancellationTokenSource.Cancel();
            var response = await _peerDeltaHistoryRequest.DeltaHistoryAsync(recipientPeerId).ConfigureAwait(false);
            response.Should().BeNull();
        }

        [Fact]
        public async Task Exception_During_Query_Returns_Null()
        {
            var recipientPeerId = PeerIdHelper.GetPeerId();
            _cancellationProvider.Dispose(); //do summet nasty to force exception
            var response = await _peerDeltaHistoryRequest.DeltaHistoryAsync(recipientPeerId).ConfigureAwait(false);
            response.Should().BeNull();   
        }

        // [Fact]
        // public async Task Can_Dispose_Class()
        // {
        //     using (_peerQueryTipRequest)
        //     {
        //         Debug.Assert(!_peerQueryTipRequest.Disposing); // Best not be disposed yet.
        //     }
        //
        //     try
        //     {
        //         Debug.Assert(_peerQueryTipRequest.Disposing); // Expecting an exception.
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.Assert(ex is ObjectDisposedException); // Better be the right one.
        //     }
        // }
    }
}
