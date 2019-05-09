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
using System.Collections.Generic;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;
using Catalyst.Common.P2P;
using System.IO;
using System.Threading.Tasks;
using Catalyst.Cli.Handlers;
using Catalyst.Common.Config;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Node.Core.Rpc.Messaging;
using Google.Protobuf;
using Xunit.Abstractions;
using TransferFileBytesRequestHandler = Catalyst.Node.Core.RPC.Handlers.TransferFileBytesRequestHandler;

namespace Catalyst.Cli.UnitTests
{
    public sealed class FileTransferTest : FileSystemBasedTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IFileTransferFactory _rpcFileTransferFactory;
        private readonly IDfs _dfs;
        private readonly IMessageCorrelationCache _cache;

        public FileTransferTest(ITestOutputHelper testOutput) : base(testOutput)
        {
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.SeedServers.Returns(new List<string>
            {
                "catalyst.seedserver01.com",
                "catalyst.seedserver02.com"
            });

            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _cache = Substitute.For<IMessageCorrelationCache>();
            _rpcFileTransferFactory = new FileTransferFactory();
            _logger = Substitute.For<ILogger>();
            _dfs = Substitute.For<IDfs>();
        }

        [Theory]
        [InlineData(1000L)]
        [InlineData(82000L)]
        [InlineData(100000L)]
        public void Get_File_Rpc(long byteSize)
        {
            string addedIpfsHash = AddFileToDfs(byteSize, out var crcValue);

            var nodePeerId = PeerIdHelper.GetPeerId("sender");
            var rpcPeerId = PeerIdHelper.GetPeerId("recipient");
            var nodePeer = new PeerIdentifier(nodePeerId);
            var rpcPeer = new PeerIdentifier(rpcPeerId);

            Guid correlationGuid = Guid.NewGuid();
            var fileDownloadInformation = FileTransferInformation.BuildDownload(rpcPeer, nodePeer,
                _fakeContext.Channel, correlationGuid, "", 0);

            long successCrc = -1;

            fileDownloadInformation.AddSuccessCallback((fileDownload) =>
            {
                successCrc = FileHelper.GetCrcValue(fileDownloadInformation.TempPath);
            });
            _rpcFileTransferFactory.RegisterTransfer(fileDownloadInformation);

            var getFileResponse = new GetFileFromDfsResponse
            {
                FileSize = (ulong) byteSize,
                ResponseCode = ByteString.CopyFrom((byte) FileTransferResponseCodes.Successful.Id)
            }.ToAnySigned(nodePeer.PeerId, correlationGuid);

            var getFileHandler =
                new GetFileFromDfsResponseHandler(_cache, _logger, _rpcFileTransferFactory);

            var fileStream = _dfs.ReadAsync(addedIpfsHash).GetAwaiter().GetResult();
            var fileUploadInformation = FileTransferInformation.BuildUpload(
                fileStream,
                rpcPeer,
                nodePeer,
                _fakeContext.Channel,
                correlationGuid,
                new RpcMessageFactory<TransferFileBytesRequest>());

            List<AnySigned> chunkMessages = new List<AnySigned>();
            for (uint i = 0; i < fileUploadInformation.MaxChunk; i++)
            {
                var transferMessage = fileUploadInformation
                   .GetUploadMessageDto(i);
                chunkMessages.Add(transferMessage);
            }

            var messageStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, getFileResponse);
            getFileHandler.StartObserving(messageStream);

            Task fileTransferTask = _rpcFileTransferFactory.InitialiseFileTransferAsync(correlationGuid);

            var transferBytesHandler =
                new TransferFileBytesRequestHandler(_rpcFileTransferFactory, rpcPeer, _cache, _logger);
            messageStream = MessageStreamHelper.CreateStreamWithMessages(_fakeContext, chunkMessages.ToArray());
            transferBytesHandler.StartObserving(messageStream);
            fileTransferTask.GetAwaiter().GetResult();

            Assert.Equal(crcValue, successCrc);
        }

        private string AddFileToDfs(long byteSize, out long crcValue)
        {
            var fileToTransfer = FileHelper.CreateRandomTempFile(byteSize);
            var fakeId = Guid.NewGuid().ToString();
            crcValue = FileHelper.GetCrcValue(fileToTransfer);
            _dfs.ReadAsync(fakeId).Returns(new MemoryStream(File.ReadAllBytes(fileToTransfer)));
            return fakeId;
        }
    }
}
