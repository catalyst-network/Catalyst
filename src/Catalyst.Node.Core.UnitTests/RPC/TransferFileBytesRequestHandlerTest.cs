
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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC
{
    public class TransferFileBytesRequestHandlerTest
    {
        private readonly TransferFileBytesRequestHandler _handler;
        private readonly IDownloadFileTransferFactory _downloadFileTransferFactory;
        private readonly IChannelHandlerContext _context;

        public TransferFileBytesRequestHandlerTest()
        {
            _context = Substitute.For<IChannelHandlerContext>();
            _context.Channel.Returns(Substitute.For<IChannel>());
            _downloadFileTransferFactory = Substitute.For<IDownloadFileTransferFactory>();
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test");

            _handler = new TransferFileBytesRequestHandler(_downloadFileTransferFactory,
                peerIdentifier,
                Substitute.For<ILogger>(), 
                new MessageFactory());
        }

        [Fact(Skip = "This tests needs to mock downloadChunk() return correctly")]
        public void CanHandlerDownloadChunk()
        {
            var guid = Guid.NewGuid();
            var request = new TransferFileBytesRequest
            {
                ChunkBytes = ByteString.Empty,
                ChunkId = 1,
                CorrelationFileName = Guid.NewGuid().ToByteString()
            }.ToAnySigned(PeerIdHelper.GetPeerId("Test"), guid);
            request.SendToHandler(_context, _handler);

            _downloadFileTransferFactory.Received(1).DownloadChunk(Arg.Any<Guid>(), Arg.Any<uint>(), Arg.Any<byte[]>());
        }

        [Fact]
        public void HandlerCanSendErrorOnException()
        {
            var guid = Guid.NewGuid();
            var request = new TransferFileBytesRequest
            {
                ChunkBytes = ByteString.Empty,
                ChunkId = 1,
                CorrelationFileName = ByteString.Empty
            }.ToAnySigned(PeerIdHelper.GetPeerId("Test"), guid);
            request.SendToHandler(_context, _handler);
            _context.Channel.Received().WriteAndFlushAsync(
                Arg.Is<ProtocolMessage>(signed =>
                    signed.FromAnySigned<TransferFileBytesResponse>().ResponseCode[0] == 
                    (byte) FileTransferResponseCodes.Error));
        }
    }
}
