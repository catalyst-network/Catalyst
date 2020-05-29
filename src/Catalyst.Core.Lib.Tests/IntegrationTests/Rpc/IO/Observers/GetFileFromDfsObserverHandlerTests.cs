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
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.FileTransfer;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Core.Modules.Rpc.Client.IO.Observers;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using MultiFormats;
using MultiFormats.Registry;
using NSubstitute;
using Serilog;
using NUnit.Framework;


namespace Catalyst.Core.Lib.Tests.IntegrationTests.Rpc.IO.Observers
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public sealed class GetFileFromDfsObserverHandlerTests : FileSystemBasedTest
    {
        private ILogger _logger;
        private IChannelHandlerContext _fakeContext;
        private IDownloadFileTransferFactory _fileDownloadFactory;
        private IDfsService _dfsService;
        private IHashProvider _hashProvider;

        [SetUp]
        public void Init()
        {
            Setup(TestContext.CurrentContext);

            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _fileDownloadFactory = new DownloadFileTransferFactory(_logger);
            _logger = Substitute.For<ILogger>();
            _dfsService = Substitute.For<IDfsService>();
        }

        [TestCase(1000L)]
        [TestCase(82000L)]
        [TestCase(100000L)]
        [TestCase(800000L)]
        public async Task Get_File_Rpc(long byteSize)
        {
            var addedIpfsHash = AddFileToDfs(byteSize, out var crcValue, out var stream);
            Stream fileStream = null;

            try
            {
                var nodePeerId = PeerIdHelper.GetPeerId("sender");
                var rpcPeerId = PeerIdHelper.GetPeerId("recipient");
                var peerSettings = Substitute.For<IPeerSettings>();
                peerSettings.Address.Returns(rpcPeerId);
                var nodePeer = nodePeerId;
                var rpcPeer = rpcPeerId;
                var correlationId = CorrelationId.GenerateCorrelationId();
                var fakeFileOutputPath = Path.GetTempFileName();
                IDownloadFileInformation fileDownloadInformation = new DownloadFileTransferInformation(rpcPeer,
                    nodePeer,
                    _fakeContext.Channel, correlationId, fakeFileOutputPath, 0);
                var getFileFromDfsResponseHandler =
                    new GetFileFromDfsResponseObserver(_logger, _fileDownloadFactory);
                var transferBytesHandler =
                    new TransferFileBytesRequestObserver(_fileDownloadFactory, peerSettings, _logger);

                _fileDownloadFactory.RegisterTransfer(fileDownloadInformation);

                var getFileResponse = new GetFileFromDfsResponse
                {
                    FileSize = (ulong) byteSize,
                    ResponseCode = ByteString.CopyFrom((byte) FileTransferResponseCodeTypes.Successful.Id)
                }.ToProtocolMessage(nodePeer, correlationId);

                getFileResponse.SendToHandler(_fakeContext, getFileFromDfsResponseHandler);

                fileStream = await _dfsService.UnixFsApi.ReadFileAsync(addedIpfsHash.ToString());
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

                Assert.AreEqual(crcValue, FileHelper.GetCrcValue(fileDownloadInformation.TempPath));
            }
            finally
            {
                stream.Close();
                fileStream?.Close();
            }
        }

        private MultiHash AddFileToDfs(long byteSize, out long crcValue, out Stream stream)
        {
            var fileToTransfer = FileHelper.CreateRandomTempFile(byteSize);
            var fakeId = _hashProvider.ComputeUtf8MultiHash(CorrelationId.GenerateCorrelationId().ToString());
            crcValue = FileHelper.GetCrcValue(fileToTransfer);
            stream = new MemoryStream(File.ReadAllBytes(fileToTransfer));
            _dfsService.UnixFsApi.ReadFileAsync(fakeId.ToString()).Returns(stream);
            return fakeId;
        }
    }
}
