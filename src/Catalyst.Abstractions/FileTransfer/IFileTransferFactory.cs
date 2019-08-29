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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;

namespace Catalyst.Abstractions.FileTransfer
{
    /// <summary>
    /// The File Transfer interface
    /// </summary>
    public interface IFileTransferFactory<T> where T : IFileTransferInformation
    {
        /// <summary>Registers the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Initialization response code</returns>
        FileTransferResponseCodeTypes RegisterTransfer(T fileTransferInformation);

        /// <summary>Files the transfer asynchronous.</summary>
        /// <param name="correlationId">The correlation unique identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task FileTransferAsync(ICorrelationId correlationId, CancellationToken cancellationToken);
        
        /// <summary>Gets the file transfer information.</summary>
        /// <param name="correlationId">The correlation unique identifier.</param>
        /// <returns></returns>
        T GetFileTransferInformation(ICorrelationId correlationId);

        /// <summary>Removes the specified file transfer information.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <param name="expiredOrCancelled">if set to <c>true</c> [expired or cancelled].</param>
        void Remove(T fileTransferInformation, bool expiredOrCancelled);

        /// <summary>Gets the keys.</summary>
        /// <value>The keys.</value>
        Guid[] Keys { get; }
    }
}
