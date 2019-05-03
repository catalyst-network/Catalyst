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
using System.IO;
using Catalyst.Common.Enums.FileTransfer;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf;

namespace Catalyst.Common.Interfaces.FileTransfer
{
    public interface ICliFileTransfer
    {
        /// <summary>Waits this instance.</summary>
        /// <returns>False if no signal was Received, true if signal wait Received</returns>
        bool Wait();

        /// <summary>Chunk write callback.</summary>
        /// <param name="responseCode">The response code.</param>
        void FileTransferCallback(AddFileToDfsResponseCode responseCode);

        /// <summary>The file transfer initialisation response callback.</summary>
        /// <param name="code">The code.</param>
        void InitialiseFileTransferResponseCallback(AddFileToDfsResponseCode code);

        /// <summary>Processes the completed callback.</summary>
        /// <param name="responseCode">The response code.</param>
        /// <param name="dfsHash">The DFS hash.</param>
        void ProcessCompletedCallback(AddFileToDfsResponseCode responseCode, string dfsHash);

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        void Dispose();

        /// <summary>Gets or sets the retry count.</summary>
        /// <value>The retry count.</value>
        int RetryCount { get; set; }

        /// <summary>Flag to check for successful initialise.</summary>
        /// <returns></returns>
        bool InitialiseSuccess();

        /// <summary>Waits for DFS hash.</summary>
        void WaitForDfsHash();

        /// <summary>Transfers the file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="node">The node.</param>
        /// <param name="nodePeerIdentifier">The node peer identifier</param>
        /// <param name="senderPeerIdentifier">The sender peer identifier</param>
        void TransferFile(string filePath, Guid correlationGuid, INodeRpcClient node, IPeerIdentifier nodePeerIdentifier, IPeerIdentifier senderPeerIdentifier);

        /// <summary>Gets the file transfer request message.</summary>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="correlationBytes">The correlation bytes.</param>
        /// <param name="fileLen">Length of the file.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        TransferFileBytesRequest GetFileTransferRequestMessage(FileStream fileStream, ByteString correlationBytes, long fileLen, uint index);
    }
}
