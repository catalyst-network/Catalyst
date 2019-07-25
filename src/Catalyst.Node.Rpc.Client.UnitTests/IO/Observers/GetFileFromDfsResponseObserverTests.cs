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
using System.Threading;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.IO.Observers
{
    public sealed class GetFileFromDfsResponseObserverTests
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IDownloadFileTransferFactory _fileDownloadFactory;
        private readonly ulong ExpectedFileSize = 10;

        public GetFileFromDfsResponseObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _fileDownloadFactory = Substitute.For<IDownloadFileTransferFactory>();
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void Can_Expire_Download_File_Transfer_On_Error()
        {
            var correlationId = SendResponseToHandler(FileTransferResponseCodes.Error);
            _fileDownloadFactory.GetFileTransferInformation(correlationId).Received(1).Expire();
        }

        [Fact]
        public void Can_Start_File_Download_On_Successful_Response()
        {
            var correlationId = SendResponseToHandler(FileTransferResponseCodes.Successful);
            _fileDownloadFactory.GetFileTransferInformation(correlationId).Received(1).SetLength(ExpectedFileSize);
            _fileDownloadFactory.Received(1).FileTransferAsync(correlationId, CancellationToken.None);
        }

        [Fact]
        public void Does_Nothing_If_File_Transfer_Does_Not_Exist()
        {
            var responseCode = FileTransferResponseCodes.Successful;
            var correlationId = CorrelationId.GenerateCorrelationId();
            _fileDownloadFactory.GetFileTransferInformation(correlationId).Returns(default(IDownloadFileInformation));

            var getFileFromDfsResponseHandler =
                new GetFileFromDfsResponseObserver(_logger, _fileDownloadFactory);
            var getFileResponse = GetResponseMessage(correlationId, responseCode);
            getFileResponse.SendToHandler(_fakeContext, getFileFromDfsResponseHandler);

            _fileDownloadFactory.DidNotReceiveWithAnyArgs().FileTransferAsync(default, default);
            _fakeContext.Channel.DidNotReceiveWithAnyArgs().WriteAndFlushAsync(default);
        }

        private ICorrelationId SendResponseToHandler(FileTransferResponseCodes responseCode)
        {
            var correlationId = CreateFakeDownloadFileTransfer();
            var getFileFromDfsResponseHandler =
                new GetFileFromDfsResponseObserver(_logger, _fileDownloadFactory);
            var getFileResponse = GetResponseMessage(correlationId, responseCode);
            getFileResponse.SendToHandler(_fakeContext, getFileFromDfsResponseHandler);
            return correlationId;
        }

        private ICorrelationId CreateFakeDownloadFileTransfer()
        {
            var fakeFileTransfer = Substitute.For<IDownloadFileInformation>();
            var guid = CorrelationId.GenerateCorrelationId();
            fakeFileTransfer.CorrelationId.Returns(guid);
            _fileDownloadFactory.GetFileTransferInformation(guid).Returns(fakeFileTransfer);
            return guid;
        }

        private ProtocolMessage GetResponseMessage(ICorrelationId correlationId, FileTransferResponseCodes responseCodes)
        {
            return new GetFileFromDfsResponse
            {
                FileSize = ExpectedFileSize,
                ResponseCode = ByteString.CopyFrom((byte) responseCodes.Id)
            }.ToProtocolMessage(PeerIdHelper.GetPeerId("Test"), correlationId);
        }
    }
}
