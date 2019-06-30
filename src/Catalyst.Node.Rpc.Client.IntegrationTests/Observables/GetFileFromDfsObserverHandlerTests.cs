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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.RPC.IO.Observables;
using Catalyst.Node.Rpc.Client.IO.Observables;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Polly;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using TransferFileBytesRequestObserver = Catalyst.Node.Core.RPC.IO.Observables.TransferFileBytesRequestObserver;

namespace Catalyst.Node.Rpc.Client.IntegrationTests.Observables
{
    public sealed class GetFileFromDfsObserverHandlerTests : FileSystemBasedTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IDownloadFileTransferFactory _fileDownloadFactory;
        private readonly IDfs _dfs;

        public GetFileFromDfsObserverHandlerTests(ITestOutputHelper testOutput) : base(testOutput)
        {
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.SeedServers.Returns(new List<string>
            {
                "catalyst.seedserver01.com",
                "catalyst.seedserver02.com"
            });

            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _fileDownloadFactory = new DownloadFileTransferFactory();
            _logger = Substitute.For<ILogger>();
            _dfs = Substitute.For<IDfs>();
        }

        [Theory]
        [InlineData(1000L)]
        [InlineData(82000L)]
        [InlineData(100000L)]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void Get_File_Rpc(long byteSize)
        {
            string addedIpfsHash = AddFileToDfs(byteSize, out var crcValue, out var stream);
            Stream fileStream = null;

            try
            {
                var nodePeerId = PeerIdHelper.GetPeerId("sender");
                var rpcPeerId = PeerIdHelper.GetPeerId("recipient");
                var nodePeer = new PeerIdentifier(nodePeerId);
                var rpcPeer = new PeerIdentifier(rpcPeerId);
                var correlationGuid = Guid.NewGuid();
                var fakeFileOutputPath = Path.GetTempFileName();
                IDownloadFileInformation fileDownloadInformation = new DownloadFileTransferInformation(rpcPeer,
                    nodePeer,
                    _fakeContext.Channel, correlationGuid, fakeFileOutputPath, 0);
                var getFileFromDfsResponseHandler =
                    new GetFileFromDfsResponseObserver(_logger, _fileDownloadFactory);
                var transferBytesHandler =
                    new TransferFileBytesRequestObserver(_fileDownloadFactory, rpcPeer, _logger);

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var linearBackOffRetryPolicy = Policy.Handle<TaskCanceledException>()
                   .WaitAndRetryAsync(5, retryAttempt =>
                    {
                        var timeSpan = TimeSpan.FromSeconds(retryAttempt + 5);
                        cts = new CancellationTokenSource(timeSpan);
                        return timeSpan;
                    });

                _fileDownloadFactory.RegisterTransfer(fileDownloadInformation);

                var getFileResponse = new GetFileFromDfsResponse
                {
                    FileSize = (ulong) byteSize,
                    ResponseCode = ByteString.CopyFrom((byte) FileTransferResponseCodes.Successful.Id)
                }.ToProtocolMessage(nodePeer.PeerId, correlationGuid);

                getFileResponse.SendToHandler(_fakeContext, getFileFromDfsResponseHandler);

                fileStream = _dfs.ReadAsync(addedIpfsHash).GetAwaiter().GetResult();
                IUploadFileInformation fileUploadInformation = new UploadFileTransferInformation(
                    fileStream,
                    rpcPeer,
                    nodePeer,
                    _fakeContext.Channel,
                    correlationGuid,
                    new DtoFactory());

                for (uint i = 0; i < fileUploadInformation.MaxChunk; i++)
                {
                    var transferMessage = fileUploadInformation
                       .GetUploadMessageDto(i);
                    transferMessage.Message.ToProtocolMessage(rpcPeerId).SendToHandler(_fakeContext, transferBytesHandler);
                }

                linearBackOffRetryPolicy.ExecuteAsync(() =>
                {
                    return Task.Run(() =>
                    {
                        while (!fileDownloadInformation.IsCompleted)
                        {
                            Task.Delay(1000, cts.Token).GetAwaiter().GetResult();
                        }
                    }, cts.Token);
                }).GetAwaiter().GetResult();

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
            var fakeId = Guid.NewGuid().ToString();
            crcValue = FileHelper.GetCrcValue(fileToTransfer);
            stream = new MemoryStream(File.ReadAllBytes(fileToTransfer));
            _dfs.ReadAsync(fakeId).Returns(stream);
            return fakeId;
        }
    }
}
