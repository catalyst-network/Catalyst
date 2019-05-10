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
using Catalyst.Common.Interfaces.FileTransfer;

namespace Catalyst.Common.FileTransfer
{
    /// <inheritdoc />
    /// <summary>
    /// The file transfer class handles uploads and downloads
    /// </summary>
    public abstract class BaseFileTransferFactory<T> : IFileTransferFactory<T> where T : IFileTransferInformation
    {
        /// <summary>The pending file transfers</summary>
        private readonly Dictionary<Guid, T> _pendingFileTransfers;

        /// <summary>The lock object</summary>
        private static readonly object LockObject = new object();

        /// <inheritdoc />
        public Guid[] Keys => _pendingFileTransfers.Keys.ToArray();

        /// <summary>Initializes a new instance of the <see cref="BaseFileTransferFactory{T}"/> class.</summary>
        protected BaseFileTransferFactory()
        {
            _pendingFileTransfers = new Dictionary<Guid, T>();
        }

        /// <inheritdoc />
        /// <summary>Registers the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Initialization response code</returns>
        /// <exception cref="InvalidOperationException">This instance cannot be registered to the factory due to IsDownload flag.</exception>
        public FileTransferResponseCodes RegisterTransfer(T fileTransferInformation)
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

        /// <inheritdoc />
        /// <summary>Files the transfer asynchronous.</summary>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">File transfer has not been registered to factory</exception>
        /// <exception cref="InvalidOperationException">File transfer has already been initialised</exception>
        public async Task FileTransferAsync(Guid correlationGuid, CancellationToken cancellationToken)
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
            try
            {
                fileTransferInformation.CancellationToken = cancellationToken;
                await DoTransfer(fileTransferInformation, cancellationToken);
                Remove(fileTransferInformation, fileTransferInformation.IsExpired());
            }
            catch (Exception)
            {
                Remove(fileTransferInformation, true);
                throw;
            }
        }
        
        /// <inheritdoc />
        public T GetFileTransferInformation(Guid key)
        {
            lock (LockObject)
            {
                return !_pendingFileTransfers.ContainsKey(key) ? default : _pendingFileTransfers[key];
            }
        }

        /// <inheritdoc />
        public void Remove(Guid key)
        {
            lock (LockObject)
            {
                if (_pendingFileTransfers.ContainsKey(key))
                {
                    _pendingFileTransfers.Remove(key);
                }
            }
        }

        /// <summary>Removes the specified file transfer information.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="expiredOrCancelled">if set to <c>true</c> [expired or cancelled].</param>
        protected void Remove(T fileTransferInformation, bool expiredOrCancelled)
        {
            if (expiredOrCancelled)
            {
                Remove(fileTransferInformation.CorrelationGuid);
                fileTransferInformation.CleanUp();
            }
            else
            {
                Remove(fileTransferInformation.CorrelationGuid);
                fileTransferInformation.Dispose();
            }

            fileTransferInformation.IsCompleted = true;
        }

        /// <summary>Does the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="cancellationToken">The cancellation token source.</param>
        protected abstract Task DoTransfer(T fileTransferInformation, CancellationToken cancellationToken);
    }
}
