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

using System.Threading;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc.IO.Observers
{
    public sealed class AddFileToDfsResponseObserverTests
    {
        public AddFileToDfsResponseObserverTests()
        {
            _uploadFileTransferFactory = Substitute.For<IUploadFileTransferFactory>();
        }

        private readonly IUploadFileTransferFactory _uploadFileTransferFactory;

        [Fact]
        public void HandlerRemovesFileTransferOnError()
        {
            var channelHandlerContext = Substitute.For<IChannelHandlerContext>();
            var senderPeerIdentifier = Substitute.For<IPeerIdentifier>();
            var correlationId = CorrelationId.GenerateCorrelationId();

            var addFileResponse = new AddFileToDfsResponse
            {
                DfsHash = "Test",
                ResponseCode = ByteString.CopyFrom((byte) FileTransferResponseCodeTypes.Error)
            };

            var addFileToDfsResponseObserver = new AddFileToDfsResponseObserver(
                Substitute.For<ILogger>(),
                _uploadFileTransferFactory
            );
            addFileToDfsResponseObserver.HandleResponseObserver(addFileResponse, channelHandlerContext,
                senderPeerIdentifier, correlationId);
            _uploadFileTransferFactory.Received(1).Remove(Arg.Any<IUploadFileInformation>(), true);
        }

        [Fact]
        public void InitializesFileTransferOnSuccessResponse()
        {
            var channelHandlerContext = Substitute.For<IChannelHandlerContext>();
            var senderPeerIdentifier = Substitute.For<IPeerIdentifier>();
            var correlationId = CorrelationId.GenerateCorrelationId();

            var addFileResponse = new AddFileToDfsResponse
            {
                DfsHash = "Test",
                ResponseCode = ByteString.CopyFrom((byte) FileTransferResponseCodeTypes.Successful)
            };

            var addFileToDfsResponseObserver = new AddFileToDfsResponseObserver(
                Substitute.For<ILogger>(),
                _uploadFileTransferFactory
            );

            addFileToDfsResponseObserver.HandleResponseObserver(addFileResponse, channelHandlerContext,
                senderPeerIdentifier, correlationId);
            _uploadFileTransferFactory.Received(1)
               .FileTransferAsync(Arg.Any<ICorrelationId>(), Arg.Any<CancellationToken>());
        }
    }
}
