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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace Catalyst.Common.FileTransfer
{
    public sealed class UploadFileTransferInformation : BaseFileTransferInformation, IUploadFileInformation
    {
        /// <summary>The upload message factory</summary>
        private readonly MessageFactoryBase<TransferFileBytesRequest> _uploadMessageFactory;

        /// <summary>The upload retry count</summary>
        private int _uploadRetryCount;

        /// <summary>Initializes a new instance of the <see cref="UploadFileTransferInformation"/> class.</summary>
        /// <param name="stream">The stream.</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="recipientIdentifier">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="uploadMessageFactory">The upload message factory.</param>
        public UploadFileTransferInformation(Stream stream,
            IPeerIdentifier peerIdentifier,
            IPeerIdentifier recipientIdentifier,
            IChannel recipientChannel,
            Guid correlationGuid,
            MessageFactoryBase<TransferFileBytesRequest> uploadMessageFactory) :
            base(peerIdentifier, recipientIdentifier, recipientChannel,
                correlationGuid, string.Empty, (ulong) stream.Length)
        {
            RandomAccessStream = stream;
            _uploadMessageFactory = uploadMessageFactory;
            _uploadRetryCount = 0;
        }

        /// <inheritdoc />
        public AnySigned GetUploadMessageDto(uint index)
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
                    break;
                }
            }

            var readSuccess = bytesRead == bufferSize;
            TransferFileBytesRequest transferMessage;

            if (readSuccess)
            {
                transferMessage = new TransferFileBytesRequest
                {
                    ChunkBytes = ByteString.CopyFrom(chunk),
                    ChunkId = chunkId,
                    CorrelationFileName = CorrelationGuid.ToByteString()
                };
            }
            else
            {
                return null;
            }

            return _uploadMessageFactory.GetMessage(
                transferMessage,
                PeerIdentifier,
                RecipientIdentifier,
                MessageTypes.Ask
            );
        }

        /// <inheritdoc />
        public bool RetryUpload(ref uint index)
        {
            if (_uploadRetryCount >= Constants.FileTransferMaxChunkRetryCount)
            {
                return false;
            }

            _uploadRetryCount += 1;
            index--;
            return true;
        }
    }
}
