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
using System.Linq;
using System.Threading.Tasks;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Shell;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace Catalyst.Common.FileTransfer
{
    public sealed class FileTransferInformation : IDisposable, IFileTransferInformation
    {
        /// <inheritdoc />
        /// <summary>Gets the temporary path.</summary>
        /// <value>The temporary path.</value>
        public string TempPath { get; }

        /// <inheritdoc />
        /// <summary>Gets or sets the DFS hash.</summary>
        /// <value>The DFS hash.</value>
        public string DfsHash { get; set; }

        /// <inheritdoc />
        /// <summary>Gets or sets the name of the unique file.</summary>
        /// <value>The name of the unique file.</value>
        public Guid CorrelationGuid { get; set; }

        /// <summary>Gets or sets the random access stream.</summary>
        /// <value>The random access stream.</value>
        private Stream RandomAccessStream { get; set; }

        /// <inheritdoc />
        /// <summary>Gets or sets the file output path.</summary>
        /// <value>The file output path.</value>
        public string FileOutputPath { get; set; }

        /// <inheritdoc />
        /// <summary>Gets the maximum chunk.</summary>
        /// <value>The maximum chunk.</value>
        public uint MaxChunk { get; set; }

        /// <inheritdoc />
        /// <summary>Gets or sets the recipient channel.</summary>
        /// <value>The recipient channel.</value>
        public IChannel RecipientChannel { get; set; }

        /// <inheritdoc />
        /// <summary>Gets or sets the recipient identifier.</summary>
        /// <value>The recipient identifier.</value>
        public IPeerIdentifier RecipientIdentifier { get; set; }

        /// <summary>Gets or sets the peer identifier.</summary>
        /// <value>The peer identifier.</value>
        public IPeerIdentifier PeerIdentifier { get; set; }

        /// <summary>Gets or sets a value indicating whether this instance is download.</summary>
        /// <value><c>true</c> if this instance is download; otherwise, <c>false</c>.</value>
        public bool IsDownload { get; set; }

        /// <summary>The time since last chunk</summary>
        private DateTime _timeSinceLastChunk;

        /// <summary>The chunk indicators</summary>
        private bool[] _chunkIndicators;

        /// <summary>Occurs when [on expired].</summary>
        private event Action<IFileTransferInformation> OnExpired;

        /// <summary>Occurs when [on success].</summary>
        private event Action<IFileTransferInformation> OnSuccess;
        
        /// <summary>The upload message factory</summary>
        private MessageFactoryBase<TransferFileBytesRequest> _uploadMessageFactory;

        /// <summary>The upload retry count</summary>
        private int _uploadRetryCount;

        /// <summary>Flag if instance is expired from an error</summary>
        private bool _expired;

        /// <summary>The user output</summary>
        private readonly IUserOutput _userOutput;

        /// <summary>The file lock</summary>
        private readonly object _fileLock;

        /// <summary>Initializes a new instance of the <see cref="FileTransferInformation"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier</param>
        /// <param name="recipientIdentifier">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileSize">Size of the file.</param>
        /// <param name="isDownload">if set to <c>true</c> [is download].</param>
        FileTransferInformation(IPeerIdentifier peerIdentifier, IPeerIdentifier recipientIdentifier, IChannel recipientChannel, Guid correlationGuid, string fileName, ulong fileSize, bool isDownload)
        {
            TempPath = Path.GetTempPath() + correlationGuid + ".tmp";
            MaxChunk = (uint) Math.Max(1, (int) Math.Ceiling((double) fileSize / Constants.FileTransferChunkSize));
            RecipientChannel = recipientChannel;
            RecipientIdentifier = recipientIdentifier;
            PeerIdentifier = peerIdentifier;
            CorrelationGuid = correlationGuid;
            FileOutputPath = fileName;
            IsDownload = isDownload;
            _chunkIndicators = new bool[MaxChunk];
            _userOutput = new ConsoleUserOutput();
            _fileLock = new object();
        }

        /// <summary>Builds the download.</summary>
        /// <param name="peerIdentifier">The peer identifier</param>
        /// <param name="recipientIdentifier">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="fileOutputPath">Name of the file.</param>
        /// <param name="fileSize">Size of the file.</param>
        /// <returns></returns>
        public static IFileTransferInformation BuildDownload(IPeerIdentifier peerIdentifier,
            IPeerIdentifier recipientIdentifier, 
            IChannel recipientChannel,
            Guid correlationGuid, 
            string fileOutputPath, 
            ulong fileSize)
        {
            FileTransferInformation info = new FileTransferInformation(peerIdentifier, recipientIdentifier, recipientChannel,
                correlationGuid, fileOutputPath, fileSize, true);
            info.RandomAccessStream = File.Open(info.TempPath, FileMode.CreateNew);
            info.RandomAccessStream.SetLength((long) fileSize);
            return info;
        }

        /// <summary>Builds the upload.</summary>
        /// <param name="stream">The stream.</param>
        /// <param name="peerIdentifier">The peer identifier</param>
        /// <param name="recipientIdentifier">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="correlationGuid">The correlation unique identifier.</param>
        /// <param name="uploadMessageFactory">The upload message factory</param>
        /// <returns></returns>
        public static IFileTransferInformation BuildUpload(Stream stream,
            IPeerIdentifier peerIdentifier,
            IPeerIdentifier recipientIdentifier,
            IChannel recipientChannel,
            Guid correlationGuid,
            MessageFactoryBase<TransferFileBytesRequest> uploadMessageFactory)
        {
            FileTransferInformation info = new FileTransferInformation(peerIdentifier, recipientIdentifier, recipientChannel,
                correlationGuid, string.Empty, (ulong) stream.Length, false);
            info.RandomAccessStream = stream;
            info._uploadMessageFactory = uploadMessageFactory;
            info._uploadRetryCount = 0;
            return info;
        }

        /// <inheritdoc />
        /// <summary>Writes to stream.</summary>
        /// <param name="chunk">The chunk.</param>
        /// <param name="fileBytes">The file bytes.</param>
        public void WriteToStream(uint chunk, byte[] fileBytes)
        {
            lock (_fileLock)
            {
                var idx = chunk - 1;
                RandomAccessStream.Seek(idx * Constants.FileTransferChunkSize, SeekOrigin.Begin);
                RandomAccessStream.Write(fileBytes);
                _chunkIndicators[idx] = true;
                _timeSinceLastChunk = DateTime.Now;
            }
        }

        /// <summary>Sets file the length.</summary>
        /// <param name="fileSize">Size of the file.</param>
        /// <exception cref="NotSupportedException">Cannot set length for upload type file transfer</exception>
        public void SetLength(ulong fileSize)
        {
            if (IsDownload)
            {
                MaxChunk = (uint) Math.Max(1, (int) Math.Ceiling((double) fileSize / Constants.FileTransferChunkSize));
                RandomAccessStream.SetLength((long) fileSize);
                _chunkIndicators = new bool[MaxChunk];
            }
            else
            {
                throw new NotSupportedException("Cannot set length for upload type file transfer");
            }
        }

        /// <summary>Uploads this instance.</summary>
        /// <returns></returns>
        public async Task Upload()
        {
            for (uint i = 0; i < MaxChunk; i++)
            {
                var transferMessage = GetUploadMessage(i);

                var requestMessage = _uploadMessageFactory.GetMessage(
                    message: transferMessage,
                    recipient: RecipientIdentifier,
                    sender: PeerIdentifier,
                    messageType: MessageTypes.Ask
                );
                try
                {
                    await RecipientChannel.WriteAndFlushAsync(requestMessage);
                    _chunkIndicators[i] = true;
                    _timeSinceLastChunk = DateTime.Now;
                }
                catch (Exception e)
                {
                    bool retrySuccess = RetryUpload(ref i);
                    if (!retrySuccess)
                    {
                        _userOutput.WriteLine("File upload failed. Exception: " + e);
                        break;
                    }
                }
            }

            if (IsComplete())
            {
                this.Dispose();
                this.ExecuteOnSuccess();
                this.Delete();
            }
        }

        /// <inheritdoc />
        /// <summary>Initializes this instance.</summary>
        public void Init()
        {
            _timeSinceLastChunk = DateTime.Now;
        }

        /// <inheritdoc />
        /// <summary>Determines whether this instance is expired.</summary>
        /// <returns><c>true</c> if this instance is expired; otherwise, <c>false</c>.</returns>
        public bool IsExpired()
        {
            return _expired || DateTime.Now.Subtract(_timeSinceLastChunk).TotalMinutes > Constants.FileTransferExpiryMinutes;
        }

        /// <inheritdoc />
        /// <summary>Determines whether this instance is complete.</summary>
        /// <returns><c>true</c> if this instance is complete; otherwise, <c>false</c>.</returns>
        public bool IsComplete()
        {
            return _chunkIndicators.All(indicator => indicator);
        }

        /// <inheritdoc />
        /// <summary>Cleans up.</summary>
        public void CleanUp()
        {
            Dispose();
            Delete();
        }

        /// <inheritdoc />
        /// <summary>Deletes the file.</summary>
        public void Delete()
        {
            try
            {
                File.Delete(TempPath);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <inheritdoc cref="IDisposable" />
        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to
        /// release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            RandomAccessStream.Close();
            RandomAccessStream.Dispose();
        }

        /// <inheritdoc />
        /// <summary>Executes the on expired.</summary>
        public void ExecuteOnExpired()
        {
            if (OnExpired == null)
            {
                return;
            }

            foreach (var action in OnExpired.GetInvocationList())
            {
                action.DynamicInvoke(this);
            }
        }

        /// <inheritdoc />
        /// <summary>Executes the on success.</summary>
        public void ExecuteOnSuccess()
        {
            if (OnSuccess == null)
            {
                return;
            }

            foreach (var action in OnSuccess.GetInvocationList())
            {
                action.DynamicInvoke(this);
            }
        }

        /// <summary>Occurs when [on expired].</summary>
        /// <param name="callback"></param>
        public void AddExpiredCallback(Action<IFileTransferInformation> callback)
        {
            OnExpired += callback;
        }

        /// <summary>Occurs when [on success].</summary>
        /// <param name="callback"></param>
        public void AddSuccessCallback(Action<IFileTransferInformation> callback)
        {
            OnSuccess += callback;
        }

        /// <summary>Gets the upload message.</summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public TransferFileBytesRequest GetUploadMessage(uint index)
        {
            var chunkId = index + 1;
            var startPos = index * Constants.FileTransferChunkSize;
            var endPos = chunkId * Constants.FileTransferChunkSize;
            var fileLen = RandomAccessStream.Length;

            if (endPos > fileLen)
            {
                endPos = (uint) fileLen;
            }

            var bufferSize = (int) (endPos - startPos);
            var chunk = new byte[bufferSize];
            RandomAccessStream.Seek(startPos, SeekOrigin.Begin);

            var readTries = 0;
            var bytesRead = 0;
            
            while ((bytesRead += RandomAccessStream.Read(chunk, 0, bufferSize - bytesRead)) < bufferSize)
            {
                readTries++;
                if (readTries >= Constants.FileTransferMaxChunkReadTries)
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
                    CorrelationFileName = CorrelationGuid.ToByteString()
                };
            }
            else
            {
                _userOutput.WriteLine("Error transferring chunk: " + chunkId);
            }

            return transferMessage;
        }

        public void Expire() { _expired = true; }

        /// <summary>Retries the specified index.</summary>
        /// <param name="index">The index.</param>
        /// <returns>True if retry success, false if retry failure</returns>
        private bool RetryUpload(ref uint index)
        {
            if (_uploadRetryCount >= Constants.FileTransferMaxChunkRetryCount)
            {
                return false;
            }

            _userOutput.Write($"Retrying Chunk: {index}, Retry Count: {_uploadRetryCount}");
            _uploadRetryCount += 1;
            index--;
            return true;
        }

        /// <summary>Gets the percentage.</summary>
        /// <returns></returns>
        public int GetPercentage()
        {
            int sentCount = _chunkIndicators.Count(x => x);
            return (int) Math.Ceiling((double) sentCount / _chunkIndicators.Length * 100D);
        }
    }
}
