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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.Util;
using Catalyst.Node.Core.RPC.Observables;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Nethereum.RLP;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.Observables
{
    /// <summary>
    /// Tests the CLI for peer blacklisting response
    /// </summary>
    public sealed class GetPeerBlackListingResponseObserverTest : IDisposable
    {
        private readonly IUserOutput _output;
        private readonly IChannelHandlerContext _fakeContext;

        private readonly ILogger _logger;
        private PeerBlackListingResponseObserver _observer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPeerBlackListingResponseObserverTest"/> class. </summary>
        public GetPeerBlackListingResponseObserverTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
        }

        /// <summary>
        /// RPCs the client can handle get peer blacklisting response.
        /// </summary>
        /// <param name="blacklist">The black list flag.</param>
        /// <param name="publicKey">The publicKey of the peer whose blacklist flag to change</param>
        /// <param name="ip">The IP Address of the peer whose blacklist flag to change</param>
        [Theory]
        [InlineData("true", "198.51.100.22", "cne2+eRandomValuebeingusedherefprtestingIOp")]
        [InlineData("false", "198.51.100.14", "uebeingusedhere44j6jhdhdhandomValfprtestingItn")]
        public async Task RpcClient_Can_Handle_GetBlackListingResponse(bool blacklist, string publicKey, string ip)
        {
            await TestGetBlackListResponse(blacklist, publicKey, ip);

            _output.Received(1).WriteLine($"Peer Blacklisting Successful : {blacklist}, {publicKey}, {ip}");
        }

        /// <summary>
        /// RPCs the client can handle get peer blacklisting response non existent peers.
        /// </summary>
        [Fact]
        public async Task RpcClient_Can_Handle_GetBlackListingResponseNonExistentPeers()
        { 
            await TestGetBlackListResponse(false, string.Empty, string.Empty);

            _output.Received(1).WriteLine("Peer not found");
        }

        private async Task TestGetBlackListResponse(bool blacklist, string publicKey, string ip)
        {
            var response = new MessageFactory().GetMessage(new MessageDto(
                    new SetPeerBlackListResponse
                    {
                        Blacklist = blacklist,
                        Ip = ip.ToBytesForRLPEncoding().ToByteString(),
                        PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString()
                    },
                    MessageTypes.Request,
                    PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                    PeerIdentifierHelper.GetPeerIdentifier("sender")),
                Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, response);

            _observer = new PeerBlackListingResponseObserver(_output, _logger);
            _observer.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();
        }

        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}
