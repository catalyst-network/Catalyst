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
using System.IO;
using System.Threading;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.RPC
{
    public sealed class GetFileFromDfsRequestHandlerTests
    {
        private readonly IUploadFileTransferFactory _fileTransferFactory;
        private readonly IDfs _dfs;
        private readonly GetFileFromDfsRequestHandler _handler;

        public GetFileFromDfsRequestHandlerTests()
        {
            var messageFactory = Substitute.For<IMessageFactory>();
            _fileTransferFactory = Substitute.For<IUploadFileTransferFactory>();
            _dfs = Substitute.For<IDfs>();
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("test");
            _handler = new GetFileFromDfsRequestHandler(_dfs, peerIdentifier, _fileTransferFactory, messageFactory, Substitute.For<ILogger>());
        }

        [Fact]
        public void GetFileRequestHandlerInitializesFileUpload()
        {
            using (GetFakeDfsStream(FileTransferResponseCodes.Successful))
            {
                _handler.HandleMessage(GetFileFromDfsRequestMessage());
                _dfs.Received(1).ReadAsync(Arg.Any<string>());
                _fileTransferFactory.Received(1).FileTransferAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
            }
        }

        [Fact]
        public void FileTransferStreamIsDisposedOnError()
        {
            using (var fakeStream = GetFakeDfsStream(FileTransferResponseCodes.Error))
            {
                var message = GetFileFromDfsRequestMessage();
                _handler.HandleMessage(message);
                Assert.False(fakeStream.CanRead);
            }
        }
        
        private MemoryStream GetFakeDfsStream(FileTransferResponseCodes fakeResponse)
        {
            var fakeStream = new MemoryStream();
            fakeStream.Write(new byte[50]);
            _dfs.ReadAsync(Arg.Any<string>()).Returns(fakeStream);
            _fileTransferFactory.RegisterTransfer(Arg.Any<IUploadFileInformation>()).Returns(fakeResponse);
            return fakeStream;
        }

        private IChanneledMessage<ProtocolMessage> GetFileFromDfsRequestMessage()
        {
            var getFileFromDfsRequestMessage = new GetFileFromDfsRequest
            {
                DfsHash = "test"
            };
            var protocolMessage = getFileFromDfsRequestMessage
               .ToAnySigned(PeerIdHelper.GetPeerId("TestMan"), Guid.NewGuid());
            return new ProtocolMessageDto(Substitute.For<IChannelHandlerContext>(), protocolMessage);
        }
    }
}
