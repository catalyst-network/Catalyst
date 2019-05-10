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
using Catalyst.Common.Config;

namespace Catalyst.Common.Interfaces.FileTransfer
{
    /// <summary>
    /// The File Transfer interface
    /// </summary>
    public interface IFileTransferFactory<T> where T : IFileTransferInformation
    {
        /// <summary>Registers the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Initialization response code</returns>
        FileTransferResponseCodes RegisterTransfer(T fileTransferInformation);

        /// <summary>Files the transfer asynchronous.</summary>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task FileTransferAsync(Guid correlationGuid, CancellationToken cancellationToken);
        
        /// <summary>Gets the file transfer information.</summary>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <returns></returns>
        T GetFileTransferInformation(Guid correlationGuid);

        /// <summary>Gets the keys.</summary>
        /// <value>The keys.</value>
        Guid[] Keys { get; }
    }

    /// <summary>
    /// Handles storing of file uploads
    /// </summary>
    /// <seealso cref="IFileTransferFactory{IUploadFileInformation}" />
    public interface IUploadFileTransferFactory : IFileTransferFactory<IUploadFileInformation> { }

    /// <summary>
    /// Handles storing of the file downloads
    /// </summary>
    /// <seealso cref="IFileTransferFactory{IDownloadFileInformation}" />
    public interface IDownloadFileTransferFactory : IFileTransferFactory<IDownloadFileInformation>
    {
        /// <summary>Downloads the chunk.</summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="chunkId">The chunk identifier.</param>
        /// <param name="fileChunk">The file chunk.</param>
        /// <returns></returns>
        FileTransferResponseCodes DownloadChunk(Guid fileName, uint chunkId, byte[] fileChunk);
    }
}
