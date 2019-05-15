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
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.Interfaces.FileTransfer
{
    /// <summary>
    /// The File transfer interface
    /// </summary>
    public interface IFileTransferInformation
    {
        /// <summary>Gets the percentage.</summary>
        int GetPercentage();
        
        /// <summary>Determines whether this instance is expired.</summary>
        /// <returns><c>true</c> if this instance is expired; otherwise, <c>false</c>.</returns>
        bool IsExpired();

        /// <summary>Chunks the indicators true.</summary>
        /// <returns></returns>
        bool ChunkIndicatorsTrue();

        /// <summary>Cleans up.</summary>
        void CleanUp();

        /// <summary>Deletes the file.</summary>
        void Delete();

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        void Dispose();

        /// <summary>Gets or sets a value indicating whether this instance is completed.</summary>
        /// <value><c>true</c> if this instance is completed; otherwise, <c>false</c>.</value>
        bool IsCompleted { get; set; }
        
        /// <summary>Gets or sets the DFS hash.</summary>
        /// <value>The DFS hash.</value>
        string DfsHash { get; set; }

        /// <summary>Gets the maximum chunk.</summary>
        /// <value>The maximum chunk.</value>
        uint MaxChunk { get; }
        
        /// <summary>Gets or sets the name of the unique file.</summary>
        /// <value>The name of the unique file.</value>
        Guid CorrelationGuid { get; set; }

        /// <summary>Gets the temporary path.</summary>
        /// <value>The temporary path.</value>
        string TempPath { get; }

        /// <summary>Gets or sets the file output path.</summary>
        /// <value>The file output path.</value>
        string FileOutputPath { get; set; }

        /// <summary>Gets or sets the recipient channel.</summary>
        /// <value>The recipient channel.</value>
        IChannel RecipientChannel { get; set; }

        /// <summary>Gets or sets the recipient identifier.</summary>
        /// <value>The recipient identifier.</value>
        IPeerIdentifier RecipientIdentifier { get; set; }
        
        /// <summary>Gets or sets the peer identifier.</summary>
        /// <value>The peer identifier.</value>
        IPeerIdentifier PeerIdentifier { get; set; }

        /// <summary>The cancellation token</summary>
        CancellationToken CancellationToken { get; set; }
        
        /// <summary>Gets or sets a value indicating whether this <see cref="IFileTransferInformation"/> is initialised.</summary>
        /// <value><c>true</c> if initialised; otherwise, <c>false</c>.</value>
        bool Initialised { get; set; }
        
        /// <summary>Updates the chunk indicator.</summary>
        /// <param name="chunkId">The chunk identifier.</param>
        /// <param name="state">if set to <c>true</c> [state].</param>
        void UpdateChunkIndicator(uint chunkId, bool state);
        
        /// <summary>Expires this instance.</summary>
        void Expire();
    }

    public interface IUploadFileInformation : IFileTransferInformation
    {
        /// <summary>Gets or sets the retry count.</summary>
        /// <value>The retry count.</value>
        int RetryCount { get; set; }

        /// <summary>Retries the upload.</summary>
        /// <returns></returns>
        bool CanRetry();
        
        /// <summary>Gets the upload message.</summary>
        /// <param name="chunkId">The chunk identifier.</param>
        /// <returns></returns>
        AnySigned GetUploadMessageDto(uint chunkId);
    }

    public interface IDownloadFileInformation : IFileTransferInformation
    {
        /// <summary>Writes to stream.</summary>
        /// <param name="chunk">The chunk.</param>
        /// <param name="fileBytes">The file bytes.</param>
        void WriteToStream(uint chunk, byte[] fileBytes);

        /// <summary>Sets file the length.</summary>
        /// <param name="fileSize">Size of the file.</param>
        void SetLength(ulong fileSize);
    }
}
