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
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Microsoft.Extensions.Logging;

namespace Catalyst.Common.FileTransfer
{
    /// <inheritdoc />
    /// <summary>
    /// The file transfer class handles uploads and downloads
    /// </summary>
    public sealed class FileTransferFactory : IFileTransferFactory
    {
        /// <summary>The pending file transfers</summary>
        private readonly Dictionary<Guid, IFileTransferInformation> _pendingFileTransfers;

        /// <summary>The lock object</summary>
        private static readonly object LockObject = new object();

        /// <inheritdoc />
        /// <summary>Gets the keys.</summary>
        /// <value>The keys.</value>
        public Guid[] Keys => _pendingFileTransfers.Keys.ToArray();
        
        /// <summary>Initializes a new instance of the <see cref="FileTransferFactory"/> class.</summary>
        public FileTransferFactory()
        {
            _pendingFileTransfers = new Dictionary<Guid, IFileTransferInformation>();
        }

        /// <inheritdoc />
        /// <summary>Registers the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Response code</returns>
        public FileTransferResponseCodes RegisterTransfer(IFileTransferInformation fileTransferInformation)
        {
            var fileHash = fileTransferInformation.CorrelationGuid;

            lock (LockObject)
            {
                if (_pendingFileTransfers.ContainsKey(fileHash))
                {
                    return FileTransferResponseCodes.FileAlreadyExists;
                }

                _pendingFileTransfers.Add(fileHash, fileTransferInformation);
                return FileTransferResponseCodes.Successful;
            }
        }

        /// <summary>Initialises the file transfer.</summary>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">File transfer is null</exception>
        /// <exception cref="InvalidOperationException">File transfer has already been initialised</exception>
        public async Task InitialiseFileTransferAsync(Guid correlationGuid, CancellationTokenSource cancellationTokenSource)
        {
            var fileTransferInformation = GetFileTransferInformation(correlationGuid);
            if (fileTransferInformation == null)
            {
                throw new NullReferenceException("File transfer has not been registered to factory");
            }

            if (fileTransferInformation.Initialised)
            {
                throw new InvalidOperationException("File transfer has already been initialised");
            }

            fileTransferInformation.Initialised = true;
            if (fileTransferInformation.IsDownload)
            {
                await (fileTransferInformation.TaskContext = Download(fileTransferInformation, cancellationTokenSource));
            }
            else
            {
                await (fileTransferInformation.TaskContext = Upload(fileTransferInformation, cancellationTokenSource));
            }
        }

        /// <inheritdoc />
        /// <summary>Writes the chunk.</summary>
        /// <param name="fileName">Unique name of the file.</param>
        /// <param name="chunkId">The chunk identifier.</param>
        /// <param name="fileChunk">The file chunk.</param>
        /// <returns>Response code</returns>
        public FileTransferResponseCodes DownloadChunk(Guid fileName, uint chunkId, byte[] fileChunk)
        {
            var fileTransferInformation = GetFileTransferInformation(fileName);

            if (fileTransferInformation == null)
            {
                return FileTransferResponseCodes.Expired;
            }

            if (!fileTransferInformation.IsDownload)
            {
                throw new InvalidOperationException("This instance cannot be downloaded");
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
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        /// <returns></returns>
        private async Task Download(IFileTransferInformation fileTransferInformation, CancellationTokenSource cancellationTokenSource)
        {
            bool cancelRequested = cancellationTokenSource?.IsCancellationRequested ?? false;
            while (!fileTransferInformation.IsComplete() && !fileTransferInformation.IsExpired() && !(cancelRequested = cancellationTokenSource?.IsCancellationRequested ?? false))
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            Remove(fileTransferInformation, fileTransferInformation.IsExpired() || cancelRequested);
        }

        /// <summary>Uploads this instance.</summary>
        /// <returns></returns>
        private async Task Upload(IFileTransferInformation fileTransferInformation, CancellationTokenSource cancellationTokenSource)
        {
            if (fileTransferInformation.IsDownload)
            {
                throw new InvalidOperationException("This instance cannot be uploaded");
            }

            var cancellationRequested = fileTransferInformation.IsExpired() || (cancellationTokenSource?.IsCancellationRequested ?? false);

            for (uint i = 0; i < fileTransferInformation.MaxChunk; i++)
            {
                cancellationRequested = fileTransferInformation.IsExpired() || (cancellationTokenSource?.IsCancellationRequested ?? false);
                if (cancellationRequested)
                {
                    break;
                }

                var transferMessage = fileTransferInformation.GetUploadMessageDto(i);
                try
                {
                    await fileTransferInformation.RecipientChannel.WriteAndFlushAsync(transferMessage);
                    fileTransferInformation.UpdateChunkIndicator(i, true);
                }
                catch (Exception)
                {
                    bool retrySuccess = fileTransferInformation.RetryUpload(ref i);
                    if (!retrySuccess)
                    {
                        cancellationRequested = true;
                        break;
                    }
                }
            }

            Remove(fileTransferInformation, cancellationRequested);
        }

        /// <inheritdoc />
        /// <summary>Gets the file transfer information.</summary>
        /// <param name="key">The unique file name.</param>
        /// <returns>File transfer information</returns>
        public IFileTransferInformation GetFileTransferInformation(Guid key)
        {
            lock (LockObject)
            {
                return !_pendingFileTransfers.ContainsKey(key) ? null : _pendingFileTransfers[key];
            }
        }

        /// <summary>Removes the specified key.</summary>
        /// <param name="key">The key.</param>
        private void Remove(Guid key)
        {
            lock (LockObject)
            {
                _pendingFileTransfers.Remove(key);
            }
        }

        /// <summary>Removes the specified file transfer information.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="expiredOrCancelled">if set to <c>true</c> [expired or cancelled].</param>
        private void Remove(IFileTransferInformation fileTransferInformation, bool expiredOrCancelled)
        {
            if (expiredOrCancelled)
            {
                Remove(fileTransferInformation.CorrelationGuid);
                fileTransferInformation.ExecuteOnExpired();
                fileTransferInformation.CleanUp();
            }
            else
            {
                Remove(fileTransferInformation.CorrelationGuid);
                fileTransferInformation.Dispose();
                fileTransferInformation.ExecuteOnSuccess();
                fileTransferInformation.Delete();
            }
        }
    }
}
