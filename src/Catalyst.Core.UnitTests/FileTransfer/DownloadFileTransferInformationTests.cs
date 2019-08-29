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
using System.Linq;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Core.Config;
using Catalyst.Core.FileTransfer;
using Catalyst.Core.IO.Messaging.Correlation;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.FileTransfer
{
    public sealed class DownloadFileTransferInformationTests : IDisposable
    {
        private readonly IDownloadFileInformation _downloadFileInformation;

        public DownloadFileTransferInformationTests()
        {
            _downloadFileInformation =
                new DownloadFileTransferInformation(null,
                    null,
                    null,
                    CorrelationId.GenerateCorrelationId(),
                    "",
                    10);
        }

        [Fact]
        public void Can_Write_To_File_When_Received_Chunk()
        {
            var bytes = Enumerable.Range(1, 10).Select(e => (byte) e).ToArray();
            _downloadFileInformation.WriteToStream(1, bytes);
            _downloadFileInformation.Dispose();
            var writtenBytes = File.ReadAllBytes(_downloadFileInformation.TempPath);
            writtenBytes.SequenceEqual(bytes).Should().BeTrue();
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public void Can_Set_File_Length(uint chunkAmount)
        {
            var byteLenForChunkAmount = (ulong) (Constants.FileTransferChunkSize * chunkAmount);
            _downloadFileInformation.SetLength(byteLenForChunkAmount);
            _downloadFileInformation.Dispose();
            var fileBytes = File.ReadAllBytes(_downloadFileInformation.TempPath);
            fileBytes.LongLength.Should().Be((long) byteLenForChunkAmount);
            _downloadFileInformation.MaxChunk.Should().Be(chunkAmount);
        }

        public void Dispose()
        {
            _downloadFileInformation.Delete();
        }
    }
}
