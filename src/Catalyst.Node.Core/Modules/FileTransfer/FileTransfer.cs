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

using Catalyst.Common.FileTransfer;
using Catalyst.Common.Rpc;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Catalyst.Node.Core.Modules.FileTransfer
{
    /// <summary>
    /// The file transfer class
    /// </summary>
    public sealed class FileTransfer : IFileTransfer
    {
        /// <summary>The pending file transfers</summary>
        private readonly Dictionary<string, FileTransferInformation> _pendingFileTransfers;

        /// <summary>The lock object</summary>
        private static readonly object _lockObject = new object();

        /// <summary>Initializes a new instance of the <see cref="FileTransfer"/> class.</summary>
        public FileTransfer()
        {
            _pendingFileTransfers = new Dictionary<string, FileTransferInformation>();
        }

        /// <summary>Initializes the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Response code</returns>
        public AddFileToDfsResponseCode InitializeTransfer(FileTransferInformation fileTransferInformation)
        {
            var fileHash = fileTransferInformation.UniqueFileName;

            lock (_lockObject)
            {
                if (_pendingFileTransfers.ContainsKey(fileHash))
                {
                    return AddFileToDfsResponseCode.FileAlreadyExists;
                }

                fileTransferInformation.Init();
                _pendingFileTransfers.Add(fileHash, fileTransferInformation);

                var tokenSource = new CancellationTokenSource();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                FileTaskHelper.Run(() =>
                {
                    if (fileTransferInformation.IsComplete())
                    {
                        tokenSource.Cancel();
                    }
                    else if (fileTransferInformation.IsExpired())
                    {
                        lock (_lockObject)
                        {
                            _pendingFileTransfers.Remove(fileTransferInformation.UniqueFileName);
                        }

                        fileTransferInformation.OnExpired?.Invoke(fileTransferInformation);
                        fileTransferInformation.CleanUp();
                        tokenSource.Cancel();
                    }
                }, TimeSpan.FromSeconds((FileTransferConstants.ExpiryMinutes * 60) / 2), tokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return AddFileToDfsResponseCode.Successful;
            }
        }

        /// <summary>Writes the chunk.</summary>
        /// <param name="fileName">Unique name of the file.</param>
        /// <param name="chunkId">The chunk identifier.</param>
        /// <param name="fileChunk">The file chunk.</param>
        /// <returns>Response code</returns>
        public AddFileToDfsResponseCode WriteChunk(string fileName, uint chunkId, byte[] fileChunk, out FileTransferInformation fileTransferInformation)
        {
            lock (_lockObject)
            {
                if (!_pendingFileTransfers.ContainsKey(fileName))
                {
                    fileTransferInformation = null;
                    return AddFileToDfsResponseCode.Expired;
                }

                fileTransferInformation = _pendingFileTransfers[fileName];
            }

            // Chunks should be sequential
            if (fileTransferInformation.CurrentChunk != chunkId - 1 || fileChunk.Length > FileTransferConstants.ChunkSize)
            {
                return AddFileToDfsResponseCode.Error;
            }

            fileTransferInformation.WriteToStream(chunkId, fileChunk);

            if (fileTransferInformation.IsComplete())
            {
                lock (_lockObject)
                {
                    _pendingFileTransfers.Remove(fileTransferInformation.UniqueFileName);
                }
            }

            return AddFileToDfsResponseCode.Successful;
        }
    }
}
