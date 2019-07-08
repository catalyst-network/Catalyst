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
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.P2P;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.FileTransfer
{
    /// <summary>
    /// Handles the information for file downloads
    /// </summary>
    /// <seealso cref="BaseFileTransferInformation" />
    /// <seealso cref="IDownloadFileInformation" />
    public sealed class DownloadFileTransferInformation : BaseFileTransferInformation, IDownloadFileInformation
    {
        /// <summary>The file lock</summary>
        private readonly object _fileLock;

        /// <summary>Initializes a new instance of the <see cref="DownloadFileTransferInformation"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="recipientIdentifier">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="fileOutputPath">The file output path.</param>
        /// <param name="fileSize">Size of the file.</param>
        public DownloadFileTransferInformation(IPeerIdentifier peerIdentifier,
            IPeerIdentifier recipientIdentifier,
            IChannel recipientChannel,
            ICorrelationId correlationGuid,
            string fileOutputPath,
            ulong fileSize) :
            base(peerIdentifier, recipientIdentifier, recipientChannel,
                correlationGuid, fileOutputPath, fileSize)
        {
            _fileLock = new object();
            RandomAccessStream = File.Open(TempPath, FileMode.CreateNew);
            RandomAccessStream.SetLength((long) fileSize);
        }

        /// <inheritdoc />
        public void WriteToStream(uint chunk, byte[] fileBytes)
        {
            lock (_fileLock)
            {
                var idx = chunk - 1;
                RandomAccessStream.Seek(idx * Constants.FileTransferChunkSize, SeekOrigin.Begin);
                RandomAccessStream.Write(fileBytes);
                TimeSinceLastChunk = DateTime.Now;
            }
        }

        /// <inheritdoc />
        public void SetLength(ulong fileSize)
        {
            MaxChunk = (uint) Math.Max(1, (int) Math.Ceiling((double) fileSize / Constants.FileTransferChunkSize));
            RandomAccessStream.SetLength((long) fileSize);
            ChunkIndicators = new bool[MaxChunk];
        }
    }
}
