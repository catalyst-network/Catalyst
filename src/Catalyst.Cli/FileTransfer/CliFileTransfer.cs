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
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf;
using System.IO;
using System.Threading;
using Catalyst.Common.Enums.FileTransfer;
using Catalyst.Common.Enums.Messages;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Shell;

namespace Catalyst.Cli.FileTransfer
{
    /// <inheritdoc cref="ICliFileTransfer" />
    /// <summary>
    /// Handles file transfer on the CLI
    /// </summary>
    /// <seealso cref="T:System.IDisposable" />
    public sealed class CliFileTransfer : IDisposable, ICliFileTransfer
    {
        /// <summary>The current chunk</summary>
        private uint _currentChunk;

        /// <summary>The maximum chunk</summary>
        private uint _maxChunk;

        /// <summary>The RPC message factory</summary>
        private readonly RpcMessageFactory<TransferFileBytesRequest> _rpcMessageFactory;

        /// <summary>The user output</summary>
        private readonly IUserOutput _userOutput;

        /// <summary>Gets or sets the wait handle.</summary>
        /// <value>The wait handle.</value>
        private readonly ManualResetEvent _waitHandle;

        /// <summary>The initialise file transfer response</summary>
        private AddFileToDfsResponseCode _initialiseFileTransferResponse;

        /// <summary>The current chunk response</summary>
        private AddFileToDfsResponseCode _currentChunkResponse;

        /// <summary>Initializes a new instance of the <see cref="CliFileTransfer"/> class.</summary>
        public CliFileTransfer()
        {
            RetryCount = 0;
            _waitHandle = new ManualResetEvent(false);
            _userOutput = new ConsoleUserOutput();
            _rpcMessageFactory = new RpcMessageFactory<TransferFileBytesRequest>();
        }

        /// <inheritdoc />
        /// <summary>Waits this instance.</summary>
        /// <returns>False if no signal was Received, true if signal wait Received</returns>
        public bool Wait()
        {
            return _waitHandle.WaitOne(TimeSpan.FromSeconds(FileTransferConstants.CliFileTransferWaitTime));
        }

        /// <inheritdoc />
        /// <summary>Chunk write callback.</summary>
        /// <param name="responseCode">The response code.</param>
        public void FileTransferCallback(AddFileToDfsResponseCode responseCode)
        {
            _currentChunkResponse = responseCode;
            _waitHandle.Set();
        }

        /// <inheritdoc />
        /// <summary>The file transfer initialisation response callback.</summary>
        /// <param name="code">The code.</param>
        public void InitialiseFileTransferResponseCallback(AddFileToDfsResponseCode code)
        {
            _initialiseFileTransferResponse = code;

            if (InitialiseSuccess())
            {
                _userOutput.WriteLine("Initialising File Transfer");
            }
            else
            {
                _userOutput.WriteLine("Error initialising file transfer, Node Response: " + code);
            }

            _waitHandle.Set();
        }

        /// <inheritdoc />
        /// <summary>Processes the completed callback.</summary>
        /// <param name="responseCode">The response code.</param>
        /// <param name="dfsHash">The DFS hash.</param>
        public void ProcessCompletedCallback(AddFileToDfsResponseCode responseCode, string dfsHash)
        {
            _userOutput.WriteLine(responseCode == AddFileToDfsResponseCode.Finished
                ? $"Successfully added file to DFS, DFS Hash: {dfsHash}"
                : "Failed to add file to DFS");

            _waitHandle.Set();
        }

        /// <inheritdoc cref="IDisposable" />
        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <inheritdoc />
        /// <summary>Gets or sets the retry count.</summary>
        /// <value>The retry count.</value>
        public int RetryCount { get; set; }

        /// <inheritdoc />
        /// <summary>Flag to check for successful initialise.</summary>
        /// <returns></returns>
        public bool InitialiseSuccess()
        {
            return _initialiseFileTransferResponse == AddFileToDfsResponseCode.Successful;
        }

        /// <inheritdoc />
        /// <summary>Waits for DFS hash.</summary>
        public void WaitForDfsHash()
        {
            _userOutput.WriteLine("Waiting for node to return DFS Hash");
            _waitHandle.Reset();
            var signalReceived = Wait();

            if (!signalReceived)
            {
                PrintTimeoutMessage();
            }

            _waitHandle.Reset();
        }

        /// <inheritdoc />
        /// <summary>Transfers the file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="node">The node.</param>
        /// <param name="nodePeerIdentifier">The node peer identifier</param>
        /// <param name="senderPeerIdentifier">The sender peer identifier.</param>
        public void TransferFile(string filePath, Guid correlationGuid, INodeRpcClient node, IPeerIdentifier nodePeerIdentifier, IPeerIdentifier senderPeerIdentifier)
        {
            _waitHandle.Reset();

            var correlationBytes = ByteString.CopyFrom(correlationGuid.ToByteArray());

            using (var fileStream = File.Open(filePath, FileMode.Open))
            {
                var fileLen = fileStream.Length;

                _currentChunk = 0;
                _maxChunk = (uint) Math.Max(1, (int) Math.Ceiling((double) fileLen / FileTransferConstants.ChunkSize));

                for (uint i = 0; i < _maxChunk; i++)
                {
                    var transferMessage = GetFileTransferRequestMessage(fileStream, correlationBytes, fileLen, i);

                    var requestMessage = _rpcMessageFactory.GetMessage(
                        message: transferMessage,
                        recipient: nodePeerIdentifier,
                        sender: senderPeerIdentifier,
                        messageType: DtoMessageType.Ask
                    );

                    node.SendMessage(requestMessage);

                    var responseReceived = Wait();

                    if (!responseReceived)
                    {
                        var retrySuccess = Retry(ref i);
                        if (retrySuccess)
                        {
                            continue;
                        }
                        
                        PrintTimeoutMessage();
                        break;
                    }

                    var processSuccess = ProcessChunkResponse(ref i);
                    if (processSuccess)
                    {
                        continue;
                    }
                    
                    WriteUserMessage("Error transferring file. Node Response: " + _currentChunkResponse);
                    break;
                }

                Dispose();
            }
        }

        /// <inheritdoc />
        /// <summary>Gets the file transfer request message.</summary>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="correlationBytes">The correlation bytes.</param>
        /// <param name="fileLen">Length of the file.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public TransferFileBytesRequest GetFileTransferRequestMessage(FileStream fileStream, ByteString correlationBytes, long fileLen, uint index)
        {
            var chunkId = index + 1;
            var startPos = index * FileTransferConstants.ChunkSize;
            var endPos = chunkId * FileTransferConstants.ChunkSize;
            if (endPos > fileLen)
            {
                endPos = fileLen;
            }

            var bufferSize = (int) (endPos - startPos);
            var chunk = new byte[bufferSize];
            fileStream.Position = startPos;

            var readTries = 0;
            var bytesRead = 0;

            while ((bytesRead += fileStream.Read(chunk, 0, bufferSize - bytesRead)) < bufferSize)
            {
                readTries++;
                if (readTries >= FileTransferConstants.MaxChunkReadTries)
                {
                    break;
                }
            }

            var readSuccess = bytesRead == bufferSize;
            TransferFileBytesRequest transferMessage = null;

            if (readSuccess)
            {
                transferMessage = new TransferFileBytesRequest
                {
                    ChunkBytes = ByteString.CopyFrom(chunk),
                    ChunkId = chunkId,
                    CorrelationFileName = correlationBytes
                };
            }
            else
            {
                _userOutput.WriteLine("Error transferring chunk: " + chunkId);
            }

            return transferMessage;
        }

        /// <summary>Processes the chunk response.</summary>
        /// <param name="index">The index.</param>
        /// <returns>True if success, False if failure</returns>
        private bool ProcessChunkResponse(ref uint index)
        {
            switch (_currentChunkResponse) 
            {
                case AddFileToDfsResponseCode.Expired:
                    return false;
                case AddFileToDfsResponseCode.Successful:
                {
                    _currentChunk = index + 1;
                    RetryCount = 0;
                    WriteUserMessage("Transferring file: " + GetPercentage() + " %");
                    if (_currentChunk == _maxChunk)
                    {
                        WriteUserMessage("\nSuccessful transfer\n");
                        Dispose();
                    }

                    _waitHandle.Reset();
                    break;
                }
                case AddFileToDfsResponseCode.FileAlreadyExists:
                    break;
                case AddFileToDfsResponseCode.Error:
                    break;
                case AddFileToDfsResponseCode.Finished:
                    break;
                case AddFileToDfsResponseCode.Failed:
                    break;
                default:
                    return Retry(ref index);
            }

            return true;
        }

        /// <summary>Gets the percentage.</summary>
        /// <returns>Current percentage</returns>
        private int GetPercentage()
        {
            return (int) Math.Ceiling((double) _currentChunk / _maxChunk * 100D);
        }

        /// <summary>Retries the specified index.</summary>
        /// <param name="index">The index.</param>
        /// <returns>True if retry success, false if retry failure</returns>
        private bool Retry(ref uint index)
        {
            if (RetryCount >= FileTransferConstants.MaxChunkRetryCount)
            {
                return false;
            }

            WriteUserMessage($"Retrying Chunk: {index}, Retry Count: {RetryCount}");
            RetryCount += 1;
            index--;
            return true;
        }

        /// <summary>Prints the timeout message.</summary>
        private void PrintTimeoutMessage()
        {
            WriteUserMessage("Error transferring file. Node Timeout");
        }

        /// <summary>Writes the user message to console.</summary>
        /// <param name="message">The message.</param>
        private void WriteUserMessage(string message)
        {
            _userOutput.Write($"\r{message}");
        }

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to
        /// release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            _waitHandle.Reset();
            RetryCount = 0;
            _initialiseFileTransferResponse = default;
            _currentChunkResponse = default;
        }
    }
}
