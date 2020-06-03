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
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using MultiFormats;

namespace Catalyst.Core.Lib.FileTransfer
{
    public sealed class UploadFileTransferInformation : BaseFileTransferInformation, IUploadFileInformation
    {
        /// <summary>Initializes a new instance of the <see cref="UploadFileTransferInformation"/> class.</summary>
        /// <param name="stream">The stream.</param>
        /// <param name="address">The peer address.</param>
        /// <param name="recipientId">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        public UploadFileTransferInformation(Stream stream,
            MultiAddress address,
            MultiAddress recipient,
            IChannel recipientChannel,
            ICorrelationId correlationGuid) :
            base(address, recipient, recipientChannel,
                correlationGuid, string.Empty, (ulong) stream.Length)
        {
            RandomAccessStream = stream;
            RetryCount = 0;
        }

        /// <inheritdoc />
        public IMessageDto<ProtocolMessage> GetUploadMessageDto(uint index)
        {
            var chunkId = index + 1;
            var startPos = index * Constants.FileTransferChunkSize;
            var endPos = chunkId * Constants.FileTransferChunkSize;
            var fileLen = RandomAccessStream.Length;

            if (endPos > fileLen)
            {
                endPos = (uint) fileLen;
            }

            var bufferSize = (int) (endPos - startPos);
            var chunk = new byte[bufferSize];
            RandomAccessStream.Seek(startPos, SeekOrigin.Begin);

            var readTries = 0;
            var bytesRead = 0;

            while ((bytesRead += RandomAccessStream.Read(chunk, 0, bufferSize - bytesRead)) < bufferSize)
            {
                readTries++;
                if (readTries >= Constants.FileTransferMaxChunkReadTries)
                {
                    return null;
                }
            }

            var transferMessage = new TransferFileBytesRequest
            {
                ChunkBytes = ByteString.CopyFrom(chunk),
                ChunkId = chunkId,
                CorrelationFileName = CorrelationId.Id.ToByteString()
            }.ToProtocolMessage(Address);

            return new MessageDto(transferMessage, Recipient);
        }

        public int RetryCount { get; set; }

        /// <inheritdoc />
        public bool CanRetry()
        {
            return RetryCount < Constants.FileTransferMaxChunkRetryCount;
        }
    }
}
