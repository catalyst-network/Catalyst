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
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Google.Protobuf;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Abstractions.P2P;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Observers
{
    public sealed class TransferFileBytesRequestObserverTests
    {
        private TransferFileBytesRequestObserver _observer;
        private IDownloadFileTransferFactory _downloadFileTransferFactory;
        private IChannelHandlerContext _context;
        private IPeerClient _peerClient;

        [SetUp]
        public void Init()
        {
            _peerClient = Substitute.For<IPeerClient>();
            _downloadFileTransferFactory = Substitute.For<IDownloadFileTransferFactory>();
            var peerSettings = MultiAddressHelper.GetAddress("Test").ToSubstitutedPeerSettings();
            _observer = new TransferFileBytesRequestObserver(_downloadFileTransferFactory,
                peerSettings,
                _peerClient,
                Substitute.For<ILogger>());
        }

        [Test]
        public void CanHandlerDownloadChunk()
        {
            var guid = CorrelationId.GenerateCorrelationId();
            var request = new TransferFileBytesRequest
            {
                ChunkBytes = ByteString.Empty,
                ChunkId = 1,
                CorrelationFileName = CorrelationId.GenerateCorrelationId().Id.ToByteString()
            }.ToProtocolMessage(MultiAddressHelper.GetAddress("Test"), guid);

            _downloadFileTransferFactory.DownloadChunk(Arg.Any<TransferFileBytesRequest>())
               .Returns(FileTransferResponseCodeTypes.Successful);

            request.SendToHandler(_observer);
            _downloadFileTransferFactory.Received(1).DownloadChunk(Arg.Any<TransferFileBytesRequest>());
        }

        [Test]
        public void HandlerCanSendErrorOnException()
        {
            var testScheduler = new TestScheduler();

            _downloadFileTransferFactory.DownloadChunk(Arg.Any<TransferFileBytesRequest>()).Returns(FileTransferResponseCodeTypes.Error);

            var sender = MultiAddressHelper.GetAddress("sender");
            var request = new TransferFileBytesRequest().ToProtocolMessage(sender);

            var messageStream = MessageStreamHelper.CreateStreamWithMessage(testScheduler, request);

            _observer.StartObserving(messageStream);

            testScheduler.Start();

            var receivedCalls = _peerClient.ReceivedCalls().ToList();
            receivedCalls.Count.Should().Be(1);
            var sentResponse = (ProtocolMessage) receivedCalls.Single().GetArguments().First();
            var transferFileBytesResponse = sentResponse.FromProtocolMessage<TransferFileBytesResponse>();
            transferFileBytesResponse.ResponseCode.Should().Equal((byte) FileTransferResponseCodeTypes.Error);
        }
    }
}
