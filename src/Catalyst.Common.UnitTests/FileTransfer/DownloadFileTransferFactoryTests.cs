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
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Util;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.FileTransfer
{
    public class DownloadFileTransferFactoryTests
    {
        private readonly IDownloadFileTransferFactory _downloadFileTransferFactory;

        public DownloadFileTransferFactoryTests()
        {
            _downloadFileTransferFactory = new DownloadFileTransferFactory(Substitute.For<ILogger>());
        }

        [Fact]
        public async Task Can_Cancel_Download()
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var token = cancellationTokenSource.Token;
                var downloadFileTransfer = SetupDownload(token);
                var correlationId = downloadFileTransfer.CorrelationId;

                cancellationTokenSource.Cancel();

                var cancelled = await TaskHelper.WaitForAsync(() =>
                {
                    try
                    {
                        _downloadFileTransferFactory.GetFileTransferInformation(correlationId).Should().BeNull();
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }, TimeSpan.FromSeconds(5));
                cancelled.Should().BeTrue();
            }
        }

        [Fact]
        public void Can_Download_Chunk()
        {
            var downloadFileTransfer = SetupDownload(CancellationToken.None);
            var correlationId = downloadFileTransfer.CorrelationId;

            _downloadFileTransferFactory.DownloadChunk(new TransferFileBytesRequest
            {
                ChunkBytes = new byte[1].ToByteString(),
                ChunkId = 1,
                CorrelationFileName = correlationId.Id.ToByteString()
            }).Should().Be(FileTransferResponseCodes.Successful);

            downloadFileTransfer.ReceivedWithAnyArgs().WriteToStream(default, default);
            downloadFileTransfer.Received(1).UpdateChunkIndicator(0, true);
        }

        [Fact]
        public void Can_Send_Error_On_Chunk_Bytes_Overflow()
        {
            var downloadFileTransfer = SetupDownload(CancellationToken.None);

            _downloadFileTransferFactory.DownloadChunk(new TransferFileBytesRequest
            {
                ChunkId = 0,
                ChunkBytes = new byte[Constants.FileTransferChunkSize + 1].ToByteString(),
                CorrelationFileName = downloadFileTransfer.CorrelationId.Id.ToByteString()
            }).Should().Be(FileTransferResponseCodes.Error);
        }

        [Fact]
        public void Can_Send_Error_Response_When_Downloading_Bad_Chunk()
        {
            _downloadFileTransferFactory.DownloadChunk(new TransferFileBytesRequest()).Should()
               .Be(FileTransferResponseCodes.Error);
        }

        public IDownloadFileInformation SetupDownload(CancellationToken token)
        {
            var correlationId = CorrelationId.GenerateCorrelationId();
            var downloadFileTransfer = Substitute.For<IDownloadFileInformation>();
            downloadFileTransfer.CorrelationId.Returns(correlationId);
            downloadFileTransfer.MaxChunk.Returns((uint) 1);
            _downloadFileTransferFactory.RegisterTransfer(downloadFileTransfer);
            _ = _downloadFileTransferFactory.FileTransferAsync(correlationId, token).ConfigureAwait(false);
            return downloadFileTransfer;
        }
    }
}
