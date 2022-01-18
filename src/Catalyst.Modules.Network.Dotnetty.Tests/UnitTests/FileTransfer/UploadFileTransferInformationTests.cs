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

using System.IO;
using System.Linq;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Modules.Network.Dotnetty.FileTransfer;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using FluentAssertions;
using NUnit.Framework;

namespace Catalyst.Modules.Network.Dotnetty.Tests.UnitTests.FileTransfer
{
    public sealed class UploadFileTransferInformationTests
    {
        [TestCase(2u)]
        [TestCase(3u)]
        public void Can_Get_Correct_Chunk_Transfer_File_Bytes_Request_Message(uint chunks)
        {
            int byteLengthForChunks = (int) (Constants.FileTransferChunkSize * chunks);
            byte[] expectedBytes =
                Enumerable.Range(0, byteLengthForChunks).Select(i => (byte) i).ToArray();

            using (MemoryStream ms = new(expectedBytes))
            {
                UploadFileTransferInformation uploadFileInformation = new(
                    ms,
                    MultiAddressHelper.GetAddress("test1"),
                    MultiAddressHelper.GetAddress("test2"),
                    null,
                    CorrelationId.GenerateCorrelationId());

                for (uint chunkToTest = 0; chunkToTest < chunks; chunkToTest++)
                {
                    var startIdx = (int) chunkToTest * Constants.FileTransferChunkSize;
                    var expectedChunk = expectedBytes.Skip(startIdx).Take(Constants.FileTransferChunkSize);

                    var uploadDto = uploadFileInformation.GetUploadMessageDto(chunkToTest);
                    var transferRequest = uploadDto.Content.FromProtocolMessage<TransferFileBytesRequest>();
                    transferRequest.ChunkBytes.ToArray()
                       .SequenceEqual(expectedChunk).Should().BeTrue();
                }
            }
        }

        [Test]
        public void Should_Not_Be_Able_To_Retry_After_Max_Retry()
        {
            using (MemoryStream memoryStream = new())
            {
                UploadFileTransferInformation uploadFileInformation = new(
                    memoryStream,
                    MultiAddressHelper.GetAddress("test1"),
                    MultiAddressHelper.GetAddress("test2"),
                    null,
                    CorrelationId.GenerateCorrelationId());
                uploadFileInformation.RetryCount += Constants.FileTransferMaxChunkRetryCount + 1;
                uploadFileInformation.CanRetry().Should().BeFalse();
            }
        }
    }
}
