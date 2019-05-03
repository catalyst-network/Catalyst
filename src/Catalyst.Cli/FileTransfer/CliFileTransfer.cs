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

using Catalyst.Common.Config;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf;
using System;
using System.IO;
using System.Threading;
using Catalyst.Common.Enums.FileTransfer;
using Catalyst.Common.Enums.Messages;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Shell;
using PeerTalk.Routing;

namespace Catalyst.Cli.FileTransfer
{
    /// <summary>
    /// Handles file transfer on the CLI
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class CliFileTransfer : IDisposable, ICliFileTransfer
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
        /// <param name="peerIdentifier">The peer identifier.</param>
        public CliFileTransfer()
        {
            RetryCount = 0;
            _waitHandle = new ManualResetEvent(false);
            _userOutput = new ConsoleUserOutput();
            _rpcMessageFactory = new RpcMessageFactory<TransferFileBytesRequest>();
        }

        /// <summary>Waits this instance.</summary>
        /// <returns>False if no signal was Received, true if signal wait Received</returns>
        public bool Wait()
        {
            return _waitHandle.WaitOne(TimeSpan.FromSeconds(FileTransferConstants.CliFileTransferWaitTime));
        }

        /// <summary>Chunk write callback.</summary>
        /// <param name="responseCode">The response code.</param>
        public void FileTransferCallback(AddFileToDfsResponseCode responseCode)
        {
            _currentChunkResponse = responseCode;
            _waitHandle.Set();
        }

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

        /// <summary>Processes the completed callback.</summary>
        /// <param name="responseCode">The response code.</param>
        /// <param name="dfsHash">The DFS hash.</param>
        public void ProcessCompletedCallback(AddFileToDfsResponseCode responseCode, string dfsHash)
        {
            if (responseCode == AddFileToDfsResponseCode.Finished)
            {
                _userOutput.WriteLine($"Successfully added file to DFS, DFS Hash: {dfsHash}");
            }
            else
            {
                _userOutput.WriteLine($"Failed to add file to DFS");
            }

            _waitHandle.Set();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>Gets or sets the retry count.</summary>
        /// <value>The retry count.</value>
        public int RetryCount { get; set; }

        /// <summary>Flag to check for successful initialise.</summary>
        /// <returns></returns>
        public bool InitialiseSuccess()
        {
            return _initialiseFileTransferResponse == AddFileToDfsResponseCode.Successful;
        }

        /// <summary>Waits for DFS hash.</summary>
        public void WaitForDfsHash()
        {
            _userOutput.WriteLine("Waiting for node to return DFS Hash");
            _waitHandle.Reset();
            bool signalReceived = Wait();

            if (!signalReceived)
            {
                PrintTimeoutMessage();
            }

            _waitHandle.Reset();
        }

        /// <summary>Transfers the file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="node">The node.</param>
        /// <param name="nodePeerIdentifier">The node peer identifier</param>
        /// <param name="senderPeerIdentifier">The sender peer identifier.</param>
        public void TransferFile(string filePath, Guid correlationGuid, INodeRpcClient node, IPeerIdentifier nodePeerIdentifier, IPeerIdentifier senderPeerIdentifier)
        {
            _waitHandle.Reset();

            ByteString correlationBytes = ByteString.CopyFrom(correlationGuid.ToByteArray());

            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            {
                long fileLen = fileStream.Length;

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

                    bool responseReceived = Wait();

                    if (!responseReceived)
                    {
                        bool retrySuccess = Retry(ref i);
                        if (!retrySuccess)
                        {
                            PrintTimeoutMessage();
                            break;
                        }
                    }
                    else
                    {
                        bool processSuccess = ProcessChunkResponse(ref i);
                        if (!processSuccess)
                        {
                            WriteUserMessage("Error transferring file. Node Response: " + _currentChunkResponse);
                            break;
                        }
                    }
                }

                Dispose();
            }
        }

        /// <summary>Gets the file transfer request message.</summary>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="correlationBytes">The correlation bytes.</param>
        /// <param name="fileLen">Length of the file.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public TransferFileBytesRequest GetFileTransferRequestMessage(FileStream fileStream, ByteString correlationBytes, long fileLen, uint index)
        {
            uint chunkId = index + 1;
            long startPos = index * FileTransferConstants.ChunkSize;
            long endPos = chunkId * FileTransferConstants.ChunkSize;
            if (endPos > fileLen)
            {
                endPos = fileLen;
            }

            int bufferSize = (int) (endPos - startPos);
            byte[] chunk = new byte[bufferSize];
            fileStream.Position = startPos;

            int readTries = 0;
            int bytesRead = 0;
            bool readSuccess = false;

            while ((bytesRead += fileStream.Read(chunk, 0, bufferSize - bytesRead)) < bufferSize)
            {
                readTries++;
                if (readTries >= FileTransferConstants.MaxChunkReadTries)
                {
                    break;
                }
            }

            readSuccess = bytesRead == bufferSize;
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
            if (_currentChunkResponse == AddFileToDfsResponseCode.Expired)
            {
                return false;
            }
            else if (_currentChunkResponse == AddFileToDfsResponseCode.Successful)
            {
                _currentChunk = index + 1;
                RetryCount = 0;
                WriteUserMessage("Transferring file: " + GetPercentage() + " %");
                if (_currentChunk == _maxChunk)
                {
                    WriteUserMessage("\nSuccessful transfer\n");
                    this.Dispose();
                }

                _waitHandle.Reset();
            }
            else
            {
                return Retry(ref index);
            }

            return true;
        }

        /// <summary>Gets the percentage.</summary>
        /// <returns>Current percentage</returns>
        private int GetPercentage()
        {
            return (int) (Math.Ceiling((double) _currentChunk / _maxChunk * 100D));
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
            else
            {
                WriteUserMessage($"Retrying Chunk: {index}, Retry Count: {RetryCount}");
                RetryCount += 1;
                index--;
                return true;
            }
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
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _waitHandle.Reset();
                RetryCount = 0;
                _initialiseFileTransferResponse = default;
                _currentChunkResponse = default;
            }
        }
    }
}
