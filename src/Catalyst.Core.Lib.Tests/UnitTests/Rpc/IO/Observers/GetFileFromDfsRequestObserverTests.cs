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
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using LibP2P;
using NSubstitute;
using Serilog;
using TheDotNetLeague.MultiFormats.MultiHash;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    public sealed class GetFileFromDfsRequestObserverTests : IDisposable
    {
        private readonly IHashProvider _hashProvider;
        private readonly IUploadFileTransferFactory _fileTransferFactory;
        private readonly IDfs _dfs;
        private readonly GetFileFromDfsRequestObserver _observer;

        public GetFileFromDfsRequestObserverTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _fileTransferFactory = Substitute.For<IUploadFileTransferFactory>();
            _dfs = Substitute.For<IDfs>();
            var peerSettings = PeerIdHelper.GetPeerId("test").ToSubstitutedPeerSettings();
            _observer = new GetFileFromDfsRequestObserver(_dfs, peerSettings, _fileTransferFactory,
                Substitute.For<ILogger>());
        }

        [Fact]
        public void GetFileRequestHandlerInitializesFileUpload()
        {
            using (GetFakeDfsStream(FileTransferResponseCodeTypes.Successful))
            {
                _observer.OnNext(GetFileFromDfsRequestMessage());
                _dfs.Received(1).ReadAsync(Arg.Any<Cid>());
                _fileTransferFactory.Received(1)
                   .FileTransferAsync(Arg.Any<ICorrelationId>(), Arg.Any<CancellationToken>());
            }
        }

        [Fact]
        public void FileTransferStreamIsDisposedOnError()
        {
            using (var fakeStream = GetFakeDfsStream(FileTransferResponseCodeTypes.Error))
            {
                var message = GetFileFromDfsRequestMessage();
                _observer.OnNext(message);
                Assert.False(fakeStream.CanRead);
            }
        }

        private MemoryStream GetFakeDfsStream(FileTransferResponseCodeTypes fakeResponse)
        {
            var fakeStream = new MemoryStream();
            fakeStream.Write(new byte[50]);
            _dfs.ReadAsync(Arg.Any<Cid>()).Returns(fakeStream);
            _fileTransferFactory.RegisterTransfer(Arg.Any<IUploadFileInformation>()).Returns(fakeResponse);
            return fakeStream;
        }

        private IObserverDto<ProtocolMessage> GetFileFromDfsRequestMessage()
        {
            var getFileFromDfsRequestMessage = new GetFileFromDfsRequest
            {
                DfsHash = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("test"))
            };
            var protocolMessage = getFileFromDfsRequestMessage
               .ToProtocolMessage(PeerIdHelper.GetPeerId("TestMan"), CorrelationId.GenerateCorrelationId());
            return new ObserverDto(Substitute.For<IChannelHandlerContext>(), protocolMessage);
        }

        public void Dispose() { _observer?.Dispose(); }
    }
}
