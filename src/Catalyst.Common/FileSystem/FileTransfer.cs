#region LICENSE



#endregion

using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Rpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
    public sealed class FileTransfer : IFileTransfer
    {
        private readonly Dictionary<string, FileTransferInformation> _pendingFileTransfers;

        private static readonly object _lockObject = new object();

        public FileTransfer()
        {
            _pendingFileTransfers = new Dictionary<string, FileTransferInformation>();
        }

        public AddFileToDfsResponseCode InitializeTransfer(FileTransferInformation fileTransferInformation)
        {
            var fileHash = fileTransferInformation.Hash;

            lock (_lockObject)
            {
                if (_pendingFileTransfers.ContainsKey(fileHash))
                {
                    return AddFileToDfsResponseCode.FileAlreadyExists;
                }

                fileTransferInformation.Init();
                _pendingFileTransfers.Add(fileHash, fileTransferInformation);

                var tokenSource = new CancellationTokenSource();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                FileTaskHelper.Run(() =>
                {
                    if (fileTransferInformation.IsComplete())
                    {
                        tokenSource.Cancel();
                    }
                    else if (fileTransferInformation.IsExpired())
                    {
                        lock(_lockObject)
                        {
                            _pendingFileTransfers.Remove(fileTransferInformation.Hash);
                        }
                        fileTransferInformation.CleanUpExpired();
                        fileTransferInformation.OnExpired?.Invoke();
                        tokenSource.Cancel();
                    }
                }, TimeSpan.FromSeconds( (FileTransferConstants.ExpiryMinutes * 60) / 2), tokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                return AddFileToDfsResponseCode.Successful;
            }
        }

        public AddFileToDfsResponseCode WriteChunk(string fileHash, int chunkId, byte[] fileChunk)
        {
            FileTransferInformation fileTransferInformation = null;

            lock (_lockObject)
            {
                if (!_pendingFileTransfers.ContainsKey(fileHash))
                {
                    return AddFileToDfsResponseCode.Expired;
                }

                fileTransferInformation = _pendingFileTransfers[fileHash];
            }

            // Chunks should be sequential
            if (fileTransferInformation.CurrentChunk != chunkId - 1)
            {
                return AddFileToDfsResponseCode.Error;
            }

            fileTransferInformation.WriteToStream(chunkId, fileChunk);

            if (fileTransferInformation.MaxChunk == chunkId)
            {
                fileTransferInformation.OnSuccess?.Invoke();
                fileTransferInformation.Dispose();
            }
            return AddFileToDfsResponseCode.Successful;
        }
    }
}
