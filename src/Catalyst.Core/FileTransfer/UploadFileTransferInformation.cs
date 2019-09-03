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
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace Catalyst.Core.FileTransfer
{
    public sealed class UploadFileTransferInformation : BaseFileTransferInformation, IUploadFileInformation
    {
        /// <summary>Initializes a new instance of the <see cref="UploadFileTransferInformation"/> class.</summary>
        /// <param name="stream">The stream.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="recipientIdentifier">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="uploadDtoFactory">The upload message factory.</param>
        public UploadFileTransferInformation(Stream stream,
            IPeerIdentifier peerIdentifier,
            IPeerIdentifier recipientIdentifier,
            IChannel recipientChannel,
            ICorrelationId correlationGuid) :
            base(peerIdentifier, recipientIdentifier, recipientChannel,
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
            }.ToProtocolMessage(PeerIdentifier.PeerId);

            return new MessageDto(transferMessage,
                RecipientIdentifier
            );
        }

        public int RetryCount { get; set; }

        /// <inheritdoc />
        public bool CanRetry()
        {
            return RetryCount < Constants.FileTransferMaxChunkRetryCount;
        }
    }
}
