#region LICENSE



#endregion

using Catalyst.Common.Rpc;
using System.Collections.Generic;
using System.IO;
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
namespace Catalyst.Common.FileSystem
{
    /// <summary>
    /// The file transfer class
    /// </summary>
    public sealed class FileTransfer
    {
        private readonly Dictionary<string, FileTransferInformation> _pendingFileTransfers;

        private static readonly object _lockObject = new object();

        public FileTransfer()
        {
            _pendingFileTransfers = new Dictionary<string, FileTransferInformation>();
        }

        public AddFileToDfsResponseCode InitializeTransfer(string fileHash, FileTransferInformation fileTransferInformation)
        {
            lock (_lockObject)
            {
                if (_pendingFileTransfers.ContainsKey(fileHash))
                {
                    return AddFileToDfsResponseCode.FileAlreadyExists;
                }

                _pendingFileTransfers.Add(fileHash, fileTransferInformation);
                return AddFileToDfsResponseCode.Successful;
            }
        }

        public AddFileToDfsResponseCode WriteChunk(string fileHash, int chunkId, byte[] fileChunk)
        {
            lock (_lockObject)
            {
                if (!_pendingFileTransfers.ContainsKey(fileHash))
                {
                    return AddFileToDfsResponseCode.Expired;
                }

                FileTransferInformation fileTransferInformation = _pendingFileTransfers[fileHash];
                
                // Chunks should be sequential
                if(fileTransferInformation.CurrentChunk != chunkId-1)
                {
                    return AddFileToDfsResponseCode.Error;
                }

                fileTransferInformation.WriteToStream(chunkId, fileChunk);

                return AddFileToDfsResponseCode.Successful;
            }
        }
    }
}
