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
using System.Threading.Tasks;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Config;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Protocol.Rpc.Node;
using Serilog;

namespace Catalyst.Core.FileTransfer
{
    /// <summary>
    /// Handles the download file transfer
    /// </summary>
    /// <seealso cref="BaseFileTransferFactory{IDownloadFileInformation}" />
    /// <seealso cref="IDownloadFileTransferFactory" />
    public sealed class DownloadFileTransferFactory : BaseFileTransferFactory<IDownloadFileInformation>, IDownloadFileTransferFactory
    {
        public DownloadFileTransferFactory(ILogger logger) : base(logger) { }

        /// <inheritdoc />
        protected override async Task DoTransferAsync(IDownloadFileInformation fileTransferInformation)
        {
            EnsureKeyExists(fileTransferInformation.CorrelationId);
            await DownloadAsync(fileTransferInformation).ConfigureAwait(false);
        }

        /// <summary>Writes chunk to file system and returns the write status response.</summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="chunkId">The chunk identifier.</param>
        /// <param name="fileChunk">The file chunk bytes.</param>
        /// <returns><see cref="FileTransferResponseCodeTypes"/></returns>
        private FileTransferResponseCodeTypes DownloadChunkResponse(ICorrelationId fileName, uint chunkId, byte[] fileChunk)
        {
            EnsureKeyExists(fileName);
            var fileTransferInformation = GetFileTransferInformation(fileName);

            if (fileTransferInformation == null)
            {
                return FileTransferResponseCodeTypes.Expired;
            }

            if (fileChunk.Length > Constants.FileTransferChunkSize)
            {
                return FileTransferResponseCodeTypes.Error;
            }

            fileTransferInformation.WriteToStream(chunkId, fileChunk);
            fileTransferInformation.UpdateChunkIndicator(chunkId - 1, true);
            return FileTransferResponseCodeTypes.Successful;
        }

        /// <summary>Downloads the specified file transfer information.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns></returns>
        private async Task DownloadAsync(IDownloadFileInformation fileTransferInformation)
        {
            EnsureKeyExists(fileTransferInformation.CorrelationId);
            while (!fileTransferInformation.ChunkIndicatorsTrue() && !fileTransferInformation.IsExpired())
            {
                await Task.Delay(TimeSpan.FromSeconds(1), fileTransferInformation.CancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public FileTransferResponseCodeTypes DownloadChunk(TransferFileBytesRequest transferFileBytesRequest)
        {
            FileTransferResponseCodeTypes responseCodeType;
            try
            {
                var fileTransferCorrelationId = new CorrelationId(transferFileBytesRequest.CorrelationFileName.ToByteArray());
                responseCodeType = DownloadChunkResponse(fileTransferCorrelationId, transferFileBytesRequest.ChunkId, transferFileBytesRequest.ChunkBytes.ToByteArray());
            }
            catch (Exception e)
            {
                Logger.Error(e,
                    "Failed to handle TransferFileBytesRequestHandler after receiving message {0}", transferFileBytesRequest);
                responseCodeType = FileTransferResponseCodeTypes.Error;
            }

            return responseCodeType;
        }
    }
}
