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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.FileTransfer;

namespace Catalyst.Common.FileTransfer
{
    /// <inheritdoc />
    /// <summary>
    /// The file transfer class
    /// </summary>
    public sealed class FileTransfer : IFileTransfer
    {
        /// <summary>The pending file transfers</summary>
        private readonly Dictionary<string, IFileTransferInformation> _pendingFileTransfers;

        /// <summary>The lock object</summary>
        private static readonly object LockObject = new object();

        /// <inheritdoc />
        /// <summary>Gets the keys.</summary>
        /// <value>The keys.</value>
        public string[] Keys => _pendingFileTransfers.Keys.ToArray();

        /// <summary>Initializes a new instance of the <see cref="FileTransfer"/> class.</summary>
        public FileTransfer()
        {
            _pendingFileTransfers = new Dictionary<string, IFileTransferInformation>();
        }

        /// <inheritdoc />
        /// <summary>Initializes the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Response code</returns>
        public FileTransferResponseCodes InitializeTransfer(IFileTransferInformation fileTransferInformation)
        {
            var fileHash = fileTransferInformation.UniqueFileName;

            lock (LockObject)
            {
                if (_pendingFileTransfers.ContainsKey(fileHash))
                {
                    return FileTransferResponseCodes.FileAlreadyExists;
                }

                fileTransferInformation.Init();
                _pendingFileTransfers.Add(fileHash, fileTransferInformation);

                var tokenSource = new CancellationTokenSource();

                FileTaskHelper.Run(() =>
                {
                    if (fileTransferInformation.IsComplete())
                    {
                        tokenSource.Cancel();
                    }
                    else if (fileTransferInformation.IsExpired())
                    {
                        Remove(fileTransferInformation.UniqueFileName);
                        fileTransferInformation.ExecuteOnExpired();
                        fileTransferInformation.CleanUp();
                        tokenSource.Cancel();
                    }
                }, TimeSpan.FromSeconds((double) Constants.FileTransferExpiryMinutes * 60 / 2), tokenSource.Token).ConfigureAwait(false);

                return FileTransferResponseCodes.Successful;
            }
        }

        /// <inheritdoc />
        /// <summary>Writes the chunk.</summary>
        /// <param name="fileName">Unique name of the file.</param>
        /// <param name="chunkId">The chunk identifier.</param>
        /// <param name="fileChunk">The file chunk.</param>
        /// <returns>Response code</returns>
        public FileTransferResponseCodes WriteChunk(string fileName, uint chunkId, byte[] fileChunk)
        {
            var fileTransferInformation = GetFileTransferInformation(fileName);
            if (fileTransferInformation == null)
            {
                return FileTransferResponseCodes.Expired;
            }

            // Chunks should be sequential
            if (fileTransferInformation.CurrentChunk != chunkId - 1 || fileChunk.Length > Constants.FileTransferChunkSize)
            {
                return FileTransferResponseCodes.Error;
            }

            fileTransferInformation.WriteToStream(chunkId, fileChunk);

            if (fileTransferInformation.IsComplete())
            {
                Remove(fileTransferInformation.UniqueFileName);
            }

            return FileTransferResponseCodes.Successful;
        }

        /// <inheritdoc />
        /// <summary>Gets the file transfer information.</summary>
        /// <param name="key">The unique file name.</param>
        /// <returns>File transfer information</returns>
        public IFileTransferInformation GetFileTransferInformation(string key)
        {
            lock (LockObject)
            {
                return !_pendingFileTransfers.ContainsKey(key) ? null : _pendingFileTransfers[key];
            }
        }

        /// <summary>Removes the specified key.</summary>
        /// <param name="key">The key.</param>
        private void Remove(string key)
        {
            lock (LockObject)
            {
                _pendingFileTransfers.Remove(key);
            }
        }
    }
}
