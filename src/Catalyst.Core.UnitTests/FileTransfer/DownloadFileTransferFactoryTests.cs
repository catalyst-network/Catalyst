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
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.FileTransfer;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Util;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.FileTransfer
{
    public sealed class DownloadFileTransferFactoryTests : IDisposable
    {
        private readonly IDownloadFileTransferFactory _downloadFileTransferFactory;
        private IDownloadFileInformation _downloadFileInformation;

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
                SetupDownload(token);
                var correlationId = _downloadFileInformation.CorrelationId;

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
            SetupDownload(CancellationToken.None);
            var correlationId = _downloadFileInformation.CorrelationId;

            _downloadFileTransferFactory.DownloadChunk(new TransferFileBytesRequest
            {
                ChunkBytes = new byte[1].ToByteString(),
                ChunkId = 1,
                CorrelationFileName = correlationId.Id.ToByteString()
            }).Should().Be(FileTransferResponseCodeTypes.Successful);

            _downloadFileInformation.ReceivedWithAnyArgs().WriteToStream(default, default);
            _downloadFileInformation.Received(1).UpdateChunkIndicator(0, true);
        }

        [Fact]
        public void Can_Send_Error_On_Chunk_Bytes_Overflow()
        {
            SetupDownload(CancellationToken.None);

            _downloadFileTransferFactory.DownloadChunk(new TransferFileBytesRequest
            {
                ChunkId = 0,
                ChunkBytes = new byte[Constants.FileTransferChunkSize + 1].ToByteString(),
                CorrelationFileName = _downloadFileInformation.CorrelationId.Id.ToByteString()
            }).Should().Be(FileTransferResponseCodeTypes.Error);
        }

        [Fact]
        public void Can_Send_Error_Response_When_Downloading_Bad_Chunk()
        {
            _downloadFileTransferFactory.DownloadChunk(new TransferFileBytesRequest()).Should()
               .Be(FileTransferResponseCodeTypes.Error);
        }

        public void SetupDownload(CancellationToken token)
        {
            var correlationId = CorrelationId.GenerateCorrelationId();
            _downloadFileInformation = Substitute.For<IDownloadFileInformation>();
            _downloadFileInformation.CorrelationId.Returns(correlationId);
            _downloadFileInformation.MaxChunk.Returns((uint) 1);
            _downloadFileTransferFactory.RegisterTransfer(_downloadFileInformation);
            _ = _downloadFileTransferFactory.FileTransferAsync(correlationId, token).ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Stops file transfer thread
            _downloadFileInformation?.IsExpired().Returns(true);
        }
    }
}
