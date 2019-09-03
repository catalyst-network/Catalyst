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
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Extensions;
using Catalyst.Core.FileTransfer;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.IntegrationTests.Rpc.IO.Observers
{
    public sealed class GetFileFromDfsObserverHandlerTests : FileSystemBasedTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IDownloadFileTransferFactory _fileDownloadFactory;
        private readonly IDfs _dfs;

        public GetFileFromDfsObserverHandlerTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _fileDownloadFactory = new DownloadFileTransferFactory(_logger);
            _logger = Substitute.For<ILogger>();
            _dfs = Substitute.For<IDfs>();
        }

        [Theory]
        [InlineData(1000L)]
        [InlineData(82000L)]
        [InlineData(100000L)]
        [InlineData(800000L)]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Get_File_Rpc(long byteSize)
        {
            var addedIpfsHash = AddFileToDfs(byteSize, out var crcValue, out var stream);
            Stream fileStream = null;

            try
            {
                var nodePeerId = PeerIdHelper.GetPeerId("sender");
                var rpcPeerId = PeerIdHelper.GetPeerId("recipient");
                var nodePeer = new PeerIdentifier(nodePeerId);
                var rpcPeer = new PeerIdentifier(rpcPeerId);
                var correlationId = CorrelationId.GenerateCorrelationId();
                var fakeFileOutputPath = Path.GetTempFileName();
                IDownloadFileInformation fileDownloadInformation = new DownloadFileTransferInformation(rpcPeer,
                    nodePeer,
                    _fakeContext.Channel, correlationId, fakeFileOutputPath, 0);
                var getFileFromDfsResponseHandler =
                    new GetFileFromDfsResponseObserver(_logger, _fileDownloadFactory);
                var transferBytesHandler =
                    new TransferFileBytesRequestObserver(_fileDownloadFactory, rpcPeer, _logger);

                _fileDownloadFactory.RegisterTransfer(fileDownloadInformation);

                var getFileResponse = new GetFileFromDfsResponse
                {
                    FileSize = (ulong) byteSize,
                    ResponseCode = ByteString.CopyFrom((byte) FileTransferResponseCodeTypes.Successful.Id)
                }.ToProtocolMessage(nodePeer.PeerId, correlationId);

                getFileResponse.SendToHandler(_fakeContext, getFileFromDfsResponseHandler);

                fileStream = await _dfs.ReadAsync(addedIpfsHash);
                IUploadFileInformation fileUploadInformation = new UploadFileTransferInformation(
                    fileStream,
                    rpcPeer,
                    nodePeer,
                    _fakeContext.Channel,
                    correlationId);

                for (uint i = 0; i < fileUploadInformation.MaxChunk; i++)
                {
                    var transferMessage = fileUploadInformation
                       .GetUploadMessageDto(i);
                    transferMessage.Content.SendToHandler(_fakeContext, transferBytesHandler);
                }

                await TaskHelper.WaitForAsync(() => fileDownloadInformation.IsCompleted, TimeSpan.FromSeconds(10));

                Assert.Equal(crcValue, FileHelper.GetCrcValue(fileDownloadInformation.TempPath));
            }
            finally
            {
                stream.Close();
                fileStream?.Close();
            }
        }

        private string AddFileToDfs(long byteSize, out long crcValue, out Stream stream)
        {
            var fileToTransfer = FileHelper.CreateRandomTempFile(byteSize);
            var fakeId = CorrelationId.GenerateCorrelationId().ToString();
            crcValue = FileHelper.GetCrcValue(fileToTransfer);
            stream = new MemoryStream(File.ReadAllBytes(fileToTransfer));
            _dfs.ReadAsync(fakeId).Returns(stream);
            return fakeId;
        }
    }
}
