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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.IO.Observers
{
    public sealed class AddFileToDfsResponseObserverTests
    {
        private readonly IUserOutput _userOutput;
        private readonly IUploadFileTransferFactory _uploadFileTransferFactory;
        private readonly AddFileToDfsResponseObserver _addFileToDfsResponseObserver;
        private readonly IChannelHandlerContext _channelHandlerContext;

        public AddFileToDfsResponseObserverTests()
        {
            _userOutput = Substitute.For<IUserOutput>();
            _uploadFileTransferFactory = Substitute.For<IUploadFileTransferFactory>();
            _channelHandlerContext = Substitute.For<IChannelHandlerContext>();

            _addFileToDfsResponseObserver = new AddFileToDfsResponseObserver(
                Substitute.For<ILogger>(),
                _uploadFileTransferFactory,
                _userOutput
            );
        }

        [Fact]
        public void AddFileToDfsResponseHandlerPrintsMessageOnFailureOrSuccess()
        {
            _addFileToDfsResponseObserver.OnNext(GetAddFileToDfsResponse(FileTransferResponseCodes.Finished));
            _addFileToDfsResponseObserver.OnNext(GetAddFileToDfsResponse(FileTransferResponseCodes.Failed));
            _userOutput.Received(Quantity.Exactly(2)).WriteLine(Arg.Any<string>());
        }

        [Fact]
        public void InitializesFileTransferOnSuccessResponse()
        {
            _addFileToDfsResponseObserver.OnNext(GetAddFileToDfsResponse(FileTransferResponseCodes.Successful));
            _uploadFileTransferFactory.Received(Quantity.Exactly(1))
               .FileTransferAsync(Arg.Any<ICorrelationId>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public void HandlerRemovesFileTransferOnError()
        {
            _addFileToDfsResponseObserver.OnNext(GetAddFileToDfsResponse(FileTransferResponseCodes.Error));
            _uploadFileTransferFactory.Received(Quantity.Exactly(1)).Remove(Arg.Any<ICorrelationId>());
        }

        private IObserverDto<ProtocolMessage> GetAddFileToDfsResponse(FileTransferResponseCodes responseCode)
        {
            AddFileToDfsResponse addFileResponse = new AddFileToDfsResponse
            {
                DfsHash = "Test",
                ResponseCode = ByteString.CopyFrom((byte) responseCode)
            };

            var protocolMessage = addFileResponse.ToProtocolMessage(PeerIdHelper.GetPeerId("Test"), CorrelationId.GenerateCorrelationId());
            return new ObserverDto(_channelHandlerContext, protocolMessage);
        }
    }
}
