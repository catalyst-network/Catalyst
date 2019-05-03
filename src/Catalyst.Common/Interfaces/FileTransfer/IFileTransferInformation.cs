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
using Catalyst.Common.Interfaces.P2P;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.Interfaces.FileTransfer
{
    /// <summary>
    /// The File transfer interface
    /// </summary>
    public interface IFileTransferInformation
    {
        /// <summary>Writes to stream.</summary>
        /// <param name="chunk">The chunk.</param>
        /// <param name="fileBytes">The file bytes.</param>
        void WriteToStream(uint chunk, byte[] fileBytes);

        /// <summary>Initializes this instance.</summary>
        void Init();

        /// <summary>Determines whether this instance is expired.</summary>
        /// <returns><c>true</c> if this instance is expired; otherwise, <c>false</c>.</returns>
        bool IsExpired();

        /// <summary>Determines whether this instance is complete.</summary>
        /// <returns><c>true</c> if this instance is complete; otherwise, <c>false</c>.</returns>
        bool IsComplete();

        /// <summary>Cleans up.</summary>
        void CleanUp();

        /// <summary>Deletes the file.</summary>
        void Delete();

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        void Dispose();

        /// <summary>Executes the on expired.</summary>
        void ExecuteOnExpired();

        /// <summary>Executes the on success.</summary>
        void ExecuteOnSuccess();
        
        /// <summary>Gets or sets the current chunk.</summary>
        /// <value>The current chunk.</value>
        uint CurrentChunk { get; set; }

        /// <summary>Gets or sets the DFS hash.</summary>
        /// <value>The DFS hash.</value>
        string DfsHash { get; set; }

        /// <summary>Gets the maximum chunk.</summary>
        /// <value>The maximum chunk.</value>
        uint MaxChunk { get; }

        /// <summary>Gets or sets the name of the unique file.</summary>
        /// <value>The name of the unique file.</value>
        string UniqueFileName { get; set; }

        /// <summary>Gets the temporary path.</summary>
        /// <value>The temporary path.</value>
        string TempPath { get; }

        /// <summary>Gets or sets the name of the file.</summary>
        /// <value>The name of the file.</value>
        string FileName { get; set; }

        /// <summary>Gets or sets the recipient channel.</summary>
        /// <value>The recipient channel.</value>
        IChannel RecipientChannel { get; set; }

        /// <summary>Gets or sets the recipient identifier.</summary>
        /// <value>The recipient identifier.</value>
        IPeerIdentifier RecipientIdentifier { get; set; }

        /// <summary>Occurs when [on expired].</summary>
        void AddExpiredCallback(Action<IFileTransferInformation> callback);

        /// <summary>Occurs when [on success].</summary>
        void AddSuccessCallback(Action<IFileTransferInformation> callback);
    }
}
