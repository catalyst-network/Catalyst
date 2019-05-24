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
using Catalyst.Cli.Handlers;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Rpc;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;
using Nethereum.RLP;
using Catalyst.Common.Util;

namespace Catalyst.Cli.UnitTests
{
    /// <summary>
    /// Tests the CLI for peer blacklisting response
    /// </summary>
    public sealed class GetPeerBlackListingResponseHandlerTest : IDisposable
    {
        private readonly IUserOutput _output;
        private readonly IChannelHandlerContext _fakeContext;

        private readonly ILogger _logger;
        private PeerBlackListingResponseHandler _handler;
        private readonly IRpcCorrelationCache _subbedCorrelationCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPeerBlackListingResponseHandlerTest"/> class. </summary>
        public GetPeerBlackListingResponseHandlerTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _output = Substitute.For<IUserOutput>();
            _subbedCorrelationCache = Substitute.For<IRpcCorrelationCache>();
        }

        /// <summary>
        /// RPCs the client can handle get peer blacklisting response.
        /// </summary>
        /// <param name="rep">The rep.</param>
        [Theory]
        [InlineData("true", "192.200.200.22", "cne2+eRandomValuebeingusedherefprtestingIOp")]
        [InlineData("false", "207.112.46.22", "uebeingusedhere44j6jhdhdhandomValfprtestingItn")]
        public void RpcClient_Can_Handle_GetBlackListingResponse(bool blacklist, string publicKey, string ip)
        {
            TestGetBlackListResponse(blacklist, publicKey, ip);

            _output.Received(1).WriteLine($"Peer Blacklisting Successful : {blacklist}, {publicKey}, {ip}");
        }

        /// <summary>
        /// RPCs the client can handle get peer blacklisting response non existent peers.
        /// </summary>
        /// <param name="blacklist">The black list flag.</param>
        /// <param name="publicKey">The publicKey of the peer whose blacklist flag to change</param>
        /// <param name="ip">The IP Address of the peer whose blacklist flag to change</param
        [Fact]
        public void RpcClient_Can_Handle_GetBlackListingResponseNonExistentPeers()
        { 
            TestGetBlackListResponse(false, string.Empty, string.Empty);

            _output.Received(1).WriteLine("Peer not found");
        }

        private void TestGetBlackListResponse(bool blacklist, string publicKey, string ip)
        {
            var correlationCache = Substitute.For<IRpcCorrelationCache>();

            var response = new RpcMessageFactory(_subbedCorrelationCache).GetMessage(new MessageDto(
                    new SetPeerBlackListResponse
                    {
                        Blacklist = blacklist,
                        Ip = ip.ToBytesForRLPEncoding().ToByteString(),
                        PublicKey = publicKey.ToBytesForRLPEncoding().ToByteString()
                    },
                    MessageTypes.Ask,
                    PeerIdentifierHelper.GetPeerIdentifier("recipient"),
                    PeerIdentifierHelper.GetPeerIdentifier("sender")),
                Guid.NewGuid());

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_fakeContext, response);

            _handler = new PeerBlackListingResponseHandler(_output, correlationCache, _logger);
            _handler.StartObserving(messageStream);
        }

        public void Dispose()
        {
            _handler?.Dispose();
            _subbedCorrelationCache.Dispose();
        }
    }
}
