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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging;

namespace Catalyst.Common.FileTransfer
{
    /// <summary>
    /// Handles the download file transfer
    /// </summary>
    /// <seealso cref="BaseFileTransferFactory{IDownloadFileInformation}" />
    /// <seealso cref="IDownloadFileTransferFactory" />
    public sealed class DownloadFileTransferFactory : BaseFileTransferFactory<IDownloadFileInformation>, IDownloadFileTransferFactory
    {
        /// <inheritdoc />
        protected override async Task DoTransferAsync(IDownloadFileInformation fileTransferInformation)
        {
            EnsureKeyExists(fileTransferInformation.CorrelationId);
            await DownloadAsync(fileTransferInformation).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public FileTransferResponseCodes DownloadChunk(ICorrelationId fileName, uint chunkId, byte[] fileChunk)
        {
            EnsureKeyExists(fileName);
            var fileTransferInformation = GetFileTransferInformation(fileName);

            if (fileTransferInformation == null)
            {
                return FileTransferResponseCodes.Expired;
            }

            if (fileChunk.Length > Constants.FileTransferChunkSize)
            {
                return FileTransferResponseCodes.Error;
            }

            fileTransferInformation.WriteToStream(chunkId, fileChunk);
            fileTransferInformation.UpdateChunkIndicator(chunkId - 1, true);
            return FileTransferResponseCodes.Successful;
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
    }
}
