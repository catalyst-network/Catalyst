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

using System.Linq;
using System.Threading.Tasks;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Observers
{
    public sealed class TransferFileBytesRequestObserverTests
    {
        private readonly TransferFileBytesRequestObserver _observer;
        private readonly IDownloadFileTransferFactory _downloadFileTransferFactory;
        private readonly IChannelHandlerContext _context;

        public TransferFileBytesRequestObserverTests()
        {
            _context = Substitute.For<IChannelHandlerContext>();
            _context.Channel.Returns(Substitute.For<IChannel>());
            _downloadFileTransferFactory = Substitute.For<IDownloadFileTransferFactory>();
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test");
            _observer = new TransferFileBytesRequestObserver(_downloadFileTransferFactory,
                peerIdentifier,
                Substitute.For<ILogger>());
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
               .Returns(FileTransferResponseCodeTypes.Successful);

            request.SendToHandler(_context, _observer);
            _downloadFileTransferFactory.Received(1).DownloadChunk(Arg.Any<TransferFileBytesRequest>());
        }

        [Fact]
#pragma warning disable 1998
        public async Task HandlerCanSendErrorOnException()
#pragma warning restore 1998
        {
            var testScheduler = new TestScheduler();

            _downloadFileTransferFactory.DownloadChunk(Arg.Any<TransferFileBytesRequest>()).Returns(FileTransferResponseCodeTypes.Error);

            var sender = PeerIdentifierHelper.GetPeerIdentifier("sender");
            var requestDto = new MessageDto(new TransferFileBytesRequest().ToProtocolMessage(sender.PeerId)
              , PeerIdentifierHelper.GetPeerIdentifier("recipient"));

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(_context, testScheduler, requestDto.Content);

            _observer.StartObserving(messageStream);

            testScheduler.Start();

            var receivedCalls = _context.Channel.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);
            var sentResponseDto = (IMessageDto<ProtocolMessage>) receivedCalls.Single().GetArguments().Single();
            var transferFileBytesResponse = sentResponseDto.Content.FromProtocolMessage<TransferFileBytesResponse>();
            transferFileBytesResponse.ResponseCode.Should().Equal((byte) FileTransferResponseCodeTypes.Error);
        }
    }
}
