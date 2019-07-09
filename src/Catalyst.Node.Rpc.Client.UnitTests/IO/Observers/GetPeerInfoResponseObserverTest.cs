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

        /// <summary>
        /// RPCs the client can handle get peer info response.
        /// </summary>
        /// <param name="publicKey">The publicKey of the peer whose info we want</param>
        /// <param name="ip">The IP Address of the peer whose info we want</param>
        [Theory]
        [InlineData("publickey-10", "172.0.0.10")]
        [InlineData("publickey-15", "172.0.0.15")]
        public async Task RpcClient_Can_Handle_GetPeerInfoResponse(string publicKey, string ip)
        {
            await TestGetPeerInfoResponse(publicKey, ip).ConfigureAwait(false);

            _output.Received(1).WriteLine($"GetPeerInfo Successful");
        }

        /// <summary>
        /// RPCs the client can handle get peer info response non existent peers.
        /// </summary>
        [Fact]
        public async Task RpcClient_Can_Handle_GetPeerInfoResponseNonExistentPeers()
        {
            await TestGetPeerInfoResponse(string.Empty, string.Empty);

            _output.Received(1).WriteLine("Peer not found");
        }

        private async Task TestGetPeerInfoResponse(string publicKey, string ip)
        {
            var peerInfo = new PeerInfo();

            var getPeerInfoResponse = new GetPeerInfoResponse();
            if (!string.IsNullOrEmpty(publicKey) && !string.IsNullOrEmpty(ip))
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
