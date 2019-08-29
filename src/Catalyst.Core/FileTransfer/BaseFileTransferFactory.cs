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
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;
using Serilog;

namespace Catalyst.Core.FileTransfer
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
        private readonly object _lockObject = new object();

        /// <summary>The logger</summary>
        protected readonly ILogger Logger;

        /// <inheritdoc />
        public Guid[] Keys
        {
            get
            {
                lock (_lockObject)
                {
                    return _pendingFileTransfers.Keys.ToArray();
                }
            }
        }

        /// <summary>Initializes a new instance of the <see cref="BaseFileTransferFactory{T}"/> class.</summary>
        protected BaseFileTransferFactory(ILogger logger)
        {
            Logger = logger;
            _pendingFileTransfers = new Dictionary<Guid, T>();
        }

        /// <inheritdoc />
        /// <summary>Registers the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Initialization response code</returns>
        /// <exception cref="InvalidOperationException">This instance cannot be registered to the factory due to IsDownload flag.</exception>
        public FileTransferResponseCodeTypes RegisterTransfer(T fileTransferInformation)
        {
            var fileHash = fileTransferInformation.CorrelationId;

            lock (_lockObject)
            {
                if (_pendingFileTransfers.ContainsKey(fileHash.Id))
                {
                    return FileTransferResponseCodeTypes.TransferPending;
                }

                _pendingFileTransfers.Add(fileHash.Id, fileTransferInformation);
                return FileTransferResponseCodeTypes.Successful;
            }
        }

        /// <inheritdoc />
        /// <summary>Files the transfer asynchronous.</summary>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">File transfer has not been registered to factory</exception>
        /// <exception cref="InvalidOperationException">File transfer has already been initialised</exception>
        public async Task FileTransferAsync(ICorrelationId correlationGuid, CancellationToken cancellationToken)
        {
            EnsureKeyExists(correlationGuid);
            var fileTransferInformation = GetFileTransferInformation(correlationGuid);

            if (fileTransferInformation.Initialised)
            {
                throw new InvalidOperationException("File transfer has already been initialised");
            }

            fileTransferInformation.Initialised = true;
            try
            {
                fileTransferInformation.CancellationToken = cancellationToken;
                await DoTransferAsync(fileTransferInformation).ConfigureAwait(false);
                Remove(fileTransferInformation, fileTransferInformation.IsExpired());
            }
            catch (Exception)
            {
                Remove(fileTransferInformation, true);
                throw;
            }
        }
        
        /// <inheritdoc />
        public T GetFileTransferInformation(ICorrelationId key)
        {
            lock (_lockObject)
            {
                return !_pendingFileTransfers.ContainsKey(key.Id) ? default : _pendingFileTransfers[key.Id];
            }
        }

        /// <summary>Removes the specified unique identifier.</summary>
        /// <param name="guid">The unique identifier.</param>
        private void Remove(ICorrelationId key)
        {
            lock (_lockObject)
            {
                if (_pendingFileTransfers.ContainsKey(key.Id))
                {
                    _pendingFileTransfers.Remove(key.Id);
                }
            }
        }

        /// <inheritdoc />
        public void Remove(T fileTransferInformation, bool expiredOrCancelled)
        {
            EnsureKeyExists(fileTransferInformation.CorrelationId);
            if (expiredOrCancelled)
            {
                Remove(fileTransferInformation.CorrelationId);
                fileTransferInformation.CleanUp();
            }
            else
            {
                Remove(fileTransferInformation.CorrelationId);
                fileTransferInformation.Dispose();
            }

            fileTransferInformation.IsCompleted = true;
        }

        /// <summary>Ensures the key exists.</summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">The specified key does not exist inside the factory.</exception>
        protected bool EnsureKeyExists(ICorrelationId guid)
        {
            if (GetFileTransferInformation(guid) == null)
            {
                throw new InvalidOperationException("The specified key does not exist inside the factory.");
            }

            return true;
        }

        /// <summary>Does the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        protected abstract Task DoTransferAsync(T fileTransferInformation);
    }
}
