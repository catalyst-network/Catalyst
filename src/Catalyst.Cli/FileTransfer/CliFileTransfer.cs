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
using Catalyst.Common.Rpc;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.Rpc.Messaging;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf;
using System;
using System.IO;
using System.Net;
using System.Threading;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Shell;
using Dawn;

namespace Catalyst.Cli.FileTransfer
{
    /// <summary>
    /// Handles file transfer on the CLI
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class CliFileTransfer : IDisposable
    {
        /// <summary>The instance</summary>
        private static CliFileTransfer _instance;

        /// <summary>The current chunk</summary>
        private uint _currentChunk;

        /// <summary>The maximum chunk</summary>
        private uint _maxChunk;

        /// <summary>The RPC message factory</summary>
        private readonly RpcMessageFactory<TransferFileBytesRequest, RpcMessages> _rpcMessageFactory;

        /// <summary>The user output</summary>
        private readonly IUserOutput _userOutput;

        /// <summary>Initializes a new instance of the <see cref="CliFileTransfer"/> class.</summary>
        public CliFileTransfer()
        {
            RetryCount = 0;
            WaitHandle = new ManualResetEvent(false);
            _userOutput = new ConsoleUserOutput();
            _rpcMessageFactory = new RpcMessageFactory<TransferFileBytesRequest, RpcMessages>();
        }

        /// <summary>Waits this instance.</summary>
        /// <returns>False if no signal was recieved, true if signal wait recieved</returns>
        public bool Wait()
        {
            return WaitHandle.WaitOne(TimeSpan.FromSeconds(FileTransferConstants.CliFileTransferWaitTime));
        }

        /// <summary>Chunk write callback.</summary>
        /// <param name="responseCode">The response code.</param>
        public void FileTransferCallback(AddFileToDfsResponseCode responseCode)
        {
            CurrentChunkResponse = responseCode;
            WaitHandle.Set();
        }

        /// <summary>The file transfer initialisation response callback.</summary>
        /// <param name="code">The code.</param>
        public void InitialiseFileTransferResponseCallback(AddFileToDfsResponseCode code)
        {
            InitialiseFileTransferResponse = code;
            WaitHandle.Set();
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>Gets or sets the initialise file transfer response.</summary>
        /// <value>The initialise file transfer response.</value>
        public AddFileToDfsResponseCode InitialiseFileTransferResponse { get; set; }

        /// <summary>Gets or sets the current chunk response.</summary>
        /// <value>The current chunk response.</value>
        public AddFileToDfsResponseCode CurrentChunkResponse { get; set; }

        /// <summary>Gets or sets the wait handle.</summary>
        /// <value>The wait handle.</value>
        public ManualResetEvent WaitHandle { get; set; }

        /// <summary>Gets or sets the retry count.</summary>
        /// <value>The retry count.</value>
        public int RetryCount { get; set; }

        /// <summary>Gets the instance.</summary>
        /// <value>The instance.</value>
        public static CliFileTransfer Instance
        {
            get
            {
                return (_instance ?? (_instance = new CliFileTransfer()));
            }
        }

        /// <summary>Transfers the file.</summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="node">The node.</param>
        /// <param name="nodePeerIdentifier">The node peer identifier</param>
        /// <param name="peerIdentifier">The peer identifier.</param>
        public void TransferFile(string filePath, Guid correlationGuid, INodeRpcClient node, IPeerIdentifier nodePeerIdentifier, IPeerIdentifier peerIdentifier)
        {
            WaitHandle.Reset();

            ByteString correlationBytes = ByteString.CopyFrom(correlationGuid.ToByteArray());

            using (FileStream fileStream = File.Open(filePath, FileMode.Open))
            {
                long fileLen = fileStream.Length;

                _currentChunk = 0;
                _maxChunk = (uint) Math.Max(1, (int) Math.Ceiling((double) fileLen / FileTransferConstants.ChunkSize));

                for (uint i = 0; i < _maxChunk; i++)
                {
                    var transferMessage = GetFileTransferRequestMessage(fileStream, correlationBytes, fileLen, i);

                    var requestMessage = _rpcMessageFactory.GetMessage(new MessageDto<TransferFileBytesRequest, RpcMessages>(
                        type: RpcMessages.TransferFileBytesRequest,
                        message: transferMessage,
                        recipient: nodePeerIdentifier,
                        sender: peerIdentifier
                    ));

                    node.SendMessage(requestMessage);

                    bool responseRecieved = Wait();

                    if (!responseRecieved)
                    {
                        bool retrySuccess = Retry(ref i);
                        if (!retrySuccess)
                        {
                            WriteUserMessage("Error transferring file. Node Timeout");
                            break;
                        }
                    }
                    else
                    {
                        bool processSuccess = ProcessChunkResponse(ref i);
                        if (!processSuccess)
                        {
                            WriteUserMessage("Error transferring file. Node Response: " + CurrentChunkResponse);
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

            int bytesRead = 0;
            while ((bytesRead += fileStream.Read(chunk, 0, bufferSize - bytesRead)) < bufferSize) ;

            var transferMessage = new TransferFileBytesRequest
            {
                ChunkBytes = ByteString.CopyFrom(chunk),
                ChunkId = chunkId,
                CorrelationFileName = correlationBytes
            };
            return transferMessage;
        }

        /// <summary>Processes the chunk response.</summary>
        /// <param name="index">The index.</param>
        /// <returns>True if success, False if failure</returns>
        private bool ProcessChunkResponse(ref uint index)
        {
            if (CurrentChunkResponse == AddFileToDfsResponseCode.Expired)
            {
                return false;
            }
            else if (CurrentChunkResponse == AddFileToDfsResponseCode.Successful)
            {
                _currentChunk = index + 1;
                RetryCount = 0;
                WriteUserMessage("Transferring file: " + GetPercentage() + " %");
                if (_currentChunk == _maxChunk)
                {
                    WriteUserMessage("\nSuccessful transfer\n");
                    this.Dispose();
                }

                WaitHandle.Reset();
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
            if (disposing)
            {
                WaitHandle.Reset();
                RetryCount = 0;
                InitialiseFileTransferResponse = default;
                CurrentChunkResponse = default;
            }
        }
    }
}
