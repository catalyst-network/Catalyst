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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FizzWare.NBuilder;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.IO.Observers
{
    /// <summary>
    /// Tests the CLI for get peer info response
    /// </summary>
    public sealed class GetPeerInfoResponseObserverTest : IDisposable
    {
        private readonly IUserOutput _output;
        private readonly IChannelHandlerContext _fakeContext;

        private readonly ILogger _logger;
        private GetPeerInfoResponseObserver _observer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPeerInfoResponseObserverTest"/> class. </summary>
        public GetPeerInfoResponseObserverTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        private PeerInfo ConstructSamplePeerInfo(string publicKey, string ipAddress)
        {
            var peerInfo = new PeerInfo();
            peerInfo.Reputation = 0;
            peerInfo.BlackListed = false;
            peerInfo.PeerId = new PeerId
            {
                Ip = ipAddress.ToBytesForRLPEncoding().ToByteString(),
                PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString()
            };
            peerInfo.InactiveFor = TimeSpan.FromSeconds(100).ToDuration();
            peerInfo.LastSeen = DateTime.UtcNow.ToTimestamp();
            peerInfo.Modified = DateTime.UtcNow.ToTimestamp();
            peerInfo.Created = DateTime.UtcNow.ToTimestamp();
            return peerInfo;
        }

        /// <summary>
        /// RPCs the client can handle get peer info response.
        /// </summary>
        [Theory]
        [InlineData("publickey-10", "172.0.0.10")]
        public async Task RpcClient_Can_Handle_GetPeerInfoResponse(string publicKey, string ipAddress)
        {
            var peerInfo = ConstructSamplePeerInfo(publicKey, ipAddress);

            await TestGetPeerInfoResponse(peerInfo).ConfigureAwait(false);

            var repeatedPeerInfo = new RepeatedField<PeerInfo> { peerInfo };
            _output.Received(1).WriteLine(CommandFormatHelper.FormatRepeatedPeerInfoResponse(repeatedPeerInfo));
        }

        /// <summary>
        /// RPCs the client can handle get peer info response.
        /// </summary>
        [Theory]
        [InlineData("publickey-10", "172.0.0.10")]
        public async Task RpcClient_Can_Handle_Null_Modified_GetPeerInfoResponse(string publicKey, string ipAddress)
        {
            var peerInfo = ConstructSamplePeerInfo(publicKey, ipAddress);
            peerInfo.Modified = null;

            await TestGetPeerInfoResponse(peerInfo).ConfigureAwait(false);

            var repeatedPeerInfo = new RepeatedField<PeerInfo> { peerInfo };
            _output.Received(1).WriteLine(CommandFormatHelper.FormatRepeatedPeerInfoResponse(repeatedPeerInfo));
        }

        /// <summary>
        /// RPCs the client can handle get peer info response non existent peers.
        /// </summary>
        [Fact]
        public async Task RpcClient_Can_Handle_GetPeerInfoResponseNonExistentPeers()
        {
            await TestGetPeerInfoResponse(null).ConfigureAwait(false);

            _output.Received(1).WriteLine("Peer(s) not found");
        }

        private async Task TestGetPeerInfoResponse(PeerInfo peerInfo)
        {
            var getPeerInfoResponse = new GetPeerInfoResponse();
            if (peerInfo != null)
            {
                getPeerInfoResponse.PeerInfo.Add(peerInfo);
            }

            var response = new DtoFactory().GetDto(getPeerInfoResponse,
                PeerIdentifierHelper.GetPeerIdentifier("sender"),
                PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                CorrelationId.GenerateCorrelationId());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext,
                response.Content.ToProtocolMessage(PeerIdentifierHelper.GetPeerIdentifier("sender").PeerId,
                    response.CorrelationId)
            );

            _observer = new GetPeerInfoResponseObserver(_output, _logger);
            _observer.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();
        }

        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}
