#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Modules.Network.Dotnetty.Abstractions.FileTransfer;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.IO.Messaging.Dto;

namespace Catalyst.Core.Modules.Rpc.Server.Tests.UnitTests.IO.Observers
{
    public sealed class GetFileFromDfsRequestObserverTests : IDisposable
    {
        private IHashProvider _hashProvider;
        private IUploadFileTransferFactory _fileTransferFactory;
        private IDfsService _dfsService;
        private GetFileFromDfsRequestObserver _observer;

        [SetUp]
        public void Init()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _fileTransferFactory = Substitute.For<IUploadFileTransferFactory>();
            _dfsService = Substitute.For<IDfsService>();
            var peerSettings = MultiAddressHelper.GetAddress("test").ToSubstitutedPeerSettings();
            _observer = new GetFileFromDfsRequestObserver(_dfsService, peerSettings, _fileTransferFactory,
                Substitute.For<ILogger>());
        }

        [Test]
        public void GetFileRequestHandlerInitializesFileUpload()
        {
            using (GetFakeDfsStream(FileTransferResponseCodeTypes.Successful))
            {
                _observer.OnNext(GetFileFromDfsRequestMessage());
                _dfsService.UnixFsApi.Received(1)?.ReadFileAsync(Arg.Any<string>());
                _fileTransferFactory.Received(1)?
                   .FileTransferAsync(Arg.Any<ICorrelationId>(), Arg.Any<CancellationToken>());
            }
        }

        [Test]
        public void FileTransferStreamIsDisposedOnError()
        {
            using var fakeStream = GetFakeDfsStream(FileTransferResponseCodeTypes.Error);
            var message = GetFileFromDfsRequestMessage();
            _observer.OnNext(message);
            Assert.False(fakeStream.CanRead);
        }

        private MemoryStream GetFakeDfsStream(FileTransferResponseCodeTypes fakeResponse)
        {
            MemoryStream fakeStream = new();
            fakeStream.Write(new byte[50]);
            _dfsService.UnixFsApi.ReadFileAsync(Arg.Any<string>()).Returns(fakeStream);
            _fileTransferFactory.RegisterTransfer(Arg.Any<IUploadFileInformation>()).Returns(fakeResponse);
            return fakeStream;
        }

        private IObserverDto<ProtocolMessage> GetFileFromDfsRequestMessage()
        {
            var getFileFromDfsRequestMessage = new GetFileFromDfsRequest
            {
                DfsHash = _hashProvider.ComputeUtf8MultiHash("test").ToCid()
            };
            var protocolMessage = getFileFromDfsRequestMessage
               .ToProtocolMessage(MultiAddressHelper.GetAddress("TestMan"), CorrelationId.GenerateCorrelationId());
            return new ObserverDto(Substitute.For<IChannelHandlerContext>(), protocolMessage);
        }

        public void Dispose() { _observer?.Dispose(); }
    }
}
