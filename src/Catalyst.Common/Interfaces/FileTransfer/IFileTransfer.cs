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

using Catalyst.Common.Enums.FileTransfer;

namespace Catalyst.Common.Interfaces.FileTransfer
{
    /// <summary>
    /// The File Transfer interface
    /// </summary>
    public interface IFileTransfer
    {
        /// <summary>Initializes the transfer.</summary>
        /// <param name="fileTransferInformation">The file transfer information.</param>
        /// <returns>Initialization response code</returns>
        AddFileToDfsResponseCode InitializeTransfer(IFileTransferInformation fileTransferInformation);

        /// <summary>Writes the chunk.</summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="chunkId">The chunk identifier.</param>
        /// <param name="fileChunk">The file chunk.</param>
        /// <returns>Writing chunk response code</returns>
        AddFileToDfsResponseCode WriteChunk(string fileName, uint chunkId, byte[] fileChunk);

        /// <summary>Gets the file transfer information.</summary>
        /// <param name="key">The unique file name.</param>
        /// <returns>File transfer information</returns>
        IFileTransferInformation GetFileTransferInformation(string key);

        /// <summary>Gets the keys.</summary>
        /// <value>The keys.</value>
        string[] Keys { get; }
    }
}
