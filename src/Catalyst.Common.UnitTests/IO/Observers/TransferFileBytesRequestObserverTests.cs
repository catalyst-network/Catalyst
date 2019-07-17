
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

using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Core.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Observers
{
    public sealed class TransferFileBytesRequestObserverTests
    {
        private readonly TransferFileBytesRequestObserver _observer;
        private readonly IDownloadFileTransferFactory _downloadFileTransferFactory;
        private readonly IChannelHandlerContext _context;
        private readonly ILogger _logger;

        public TransferFileBytesRequestObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _context = Substitute.For<IChannelHandlerContext>();
            _context.Channel.Returns(Substitute.For<IChannel>());
            _downloadFileTransferFactory = Substitute.For<IDownloadFileTransferFactory>();
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test");
            _observer = new TransferFileBytesRequestObserver(_downloadFileTransferFactory,
                peerIdentifier,
                _logger);
        }

        [Fact]
        public void CanHandlerDownloadChunk()
        {
            var guid = CorrelationId.GenerateCorrelationId();
            var request = new TransferFileBytesRequest
            {
                ChunkBytes = ByteString.Empty,
                ChunkId = 1,
                CorrelationFileName = CorrelationId.GenerateCorrelationId().Id.ToByteString()
            }.ToProtocolMessage(PeerIdHelper.GetPeerId("Test"), guid);

            _downloadFileTransferFactory.DownloadChunk(Arg.Any<TransferFileBytesRequest>())
               .Returns(FileTransferResponseCodes.Successful);

            request.SendToHandler(_context, _observer);
            _downloadFileTransferFactory.Received(1).DownloadChunk(Arg.Any<TransferFileBytesRequest>());
        }

        [Fact]
        public async Task HandlerCanSendErrorOnException()
        {
            _downloadFileTransferFactory.DownloadChunk(Arg.Any<TransferFileBytesRequest>()).Returns(FileTransferResponseCodes.Error);

            var sender = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var requestDto = new DtoFactory().GetDto(new TransferFileBytesRequest().ToProtocolMessage(sender.PeerId)
            , sender, PeerIdentifierHelper.GetPeerIdentifier("recipient"));

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_context, requestDto.Content);

            _observer.StartObserving(messageStream);

            await messageStream.WaitForEndOfDelayedStreamOnTaskPoolSchedulerAsync();

            var receivedCalls = _context.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);
            var sentResponseDto = (IMessageDto<ProtocolMessage>)receivedCalls.Single().GetArguments().Single();
            var transferFileBytesResponse = sentResponseDto.Content.FromProtocolMessage<TransferFileBytesResponse>();
            transferFileBytesResponse.ResponseCode.Should().Equal((byte)FileTransferResponseCodes.Error);
        }
    }
}
