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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Core.Rpc.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.Rpc.IO.Observers
{
    public sealed class GetFileFromDfsRequestObserverTests : IDisposable
    {
        private readonly IUploadFileTransferFactory _fileTransferFactory;
        private readonly IDfs _dfs;
        private readonly GetFileFromDfsRequestObserver _observer;

        public GetFileFromDfsRequestObserverTests()
        {
            var messageFactory = Substitute.For<IDtoFactory>();
            _fileTransferFactory = Substitute.For<IUploadFileTransferFactory>();
            _dfs = Substitute.For<IDfs>();
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("test");
            _observer = new GetFileFromDfsRequestObserver(_dfs, peerIdentifier, _fileTransferFactory, messageFactory, Substitute.For<ILogger>());
        }

        [Fact]
        public void GetFileRequestHandlerInitializesFileUpload()
        {
            using (GetFakeDfsStream(FileTransferResponseCodes.Successful))
            {
                _observer.OnNext(GetFileFromDfsRequestMessage());
                _dfs.Received(1).ReadAsync(Arg.Any<string>());
                _fileTransferFactory.Received(1).FileTransferAsync(Arg.Any<ICorrelationId>(), Arg.Any<CancellationToken>());
            }
        }

        [Fact]
        public void FileTransferStreamIsDisposedOnError()
        {
            using (var fakeStream = GetFakeDfsStream(FileTransferResponseCodes.Error))
            {
                var message = GetFileFromDfsRequestMessage();
                _observer.OnNext(message);
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

        private IObserverDto<ProtocolMessage> GetFileFromDfsRequestMessage()
        {
            var getFileFromDfsRequestMessage = new GetFileFromDfsRequest
            {
                DfsHash = "test"
            };
            var protocolMessage = getFileFromDfsRequestMessage
               .ToProtocolMessage(PeerIdHelper.GetPeerId("TestMan"), CorrelationId.GenerateCorrelationId());
            return new ObserverDto(Substitute.For<IChannelHandlerContext>(), protocolMessage);
        }
        
        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}
