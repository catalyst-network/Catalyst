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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.P2P;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.FileTransfer
{
    public sealed class FileTransferInformation : IDisposable, IFileTransferInformation
    {
        /// <inheritdoc />
        /// <summary>Gets the maximum chunk.</summary>
        /// <value>The maximum chunk.</value>
        public uint MaxChunk { get; }

        /// <inheritdoc />
        /// <summary>Gets the temporary path.</summary>
        /// <value>The temporary path.</value>
        public string TempPath { get; }

        /// <inheritdoc />
        /// <summary>Gets or sets the current chunk.</summary>
        /// <value>The current chunk.</value>
        public uint CurrentChunk { get; set; }

        /// <inheritdoc />
        /// <summary>Gets or sets the DFS hash.</summary>
        /// <value>The DFS hash.</value>
        public string DfsHash { get; set; }
        
        /// <inheritdoc />
        /// <summary>Gets or sets the name of the unique file.</summary>
        /// <value>The name of the unique file.</value>
        public string UniqueFileName { get; set; }

        /// <summary>Gets or sets the random access stream.</summary>
        /// <value>The random access stream.</value>
        private BinaryWriter RandomAccessStream { get; set; }
        
        /// <inheritdoc />
        /// <summary>Gets or sets the name of the file.</summary>
        /// <value>The name of the file.</value>
        public string FileName { get; set; }

        /// <inheritdoc />
        /// <summary>Gets or sets the recipient channel.</summary>
        /// <value>The recipient channel.</value>
        public IChannel RecipientChannel { get; set; }

        /// <inheritdoc />
        /// <summary>Gets or sets the recipient identifier.</summary>
        /// <value>The recipient identifier.</value>
        public IPeerIdentifier RecipientIdentifier { get; set; }
        
        /// <summary>The time since last chunk</summary>
        private DateTime _timeSinceLastChunk;

        /// <summary>Occurs when [on expired].</summary>
        private event Action<IFileTransferInformation> OnExpired;

        /// <summary>Occurs when [on success].</summary>
        private event Action<IFileTransferInformation> OnSuccess;

        /// <summary>Initializes a new instance of the <see cref="FileTransferInformation"/> class.</summary>
        /// <param name="recipientIdentifier"></param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="uniqueFileName">Temporary unique file name.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="maxChunk">The maximum chunk.</param>
        public FileTransferInformation(IPeerIdentifier recipientIdentifier, IChannel recipientChannel, string uniqueFileName, string fileName, uint maxChunk)
        {
            TempPath = Path.GetTempPath() + uniqueFileName + ".tmp";
            MaxChunk = maxChunk;
            CurrentChunk = 0;
            RecipientChannel = recipientChannel;
            RecipientIdentifier = recipientIdentifier;
            UniqueFileName = uniqueFileName;
            FileName = fileName;
        }

        /// <inheritdoc />
        /// <summary>Writes to stream.</summary>
        /// <param name="chunk">The chunk.</param>
        /// <param name="fileBytes">The file bytes.</param>
        public void WriteToStream(uint chunk, byte[] fileBytes)
        {
            RandomAccessStream.Seek(0, SeekOrigin.End);
            RandomAccessStream.Write(fileBytes);
            CurrentChunk = chunk;
            _timeSinceLastChunk = DateTime.Now;
        }

        /// <inheritdoc />
        /// <summary>Initializes this instance.</summary>
        public void Init()
        {
            RandomAccessStream = new BinaryWriter(File.Open(TempPath, FileMode.CreateNew));
            _timeSinceLastChunk = DateTime.Now;
        }

        /// <inheritdoc />
        /// <summary>Determines whether this instance is expired.</summary>
        /// <returns><c>true</c> if this instance is expired; otherwise, <c>false</c>.</returns>
        public bool IsExpired()
        {
            return DateTime.Now.Subtract(_timeSinceLastChunk).TotalMinutes > Constants.FileTransferExpiryMinutes;
        }

        /// <inheritdoc />
        /// <summary>Determines whether this instance is complete.</summary>
        /// <returns><c>true</c> if this instance is complete; otherwise, <c>false</c>.</returns>
        public bool IsComplete()
        {
            return CurrentChunk == MaxChunk;
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
            File.Delete(TempPath);
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

        public void AddExpiredCallback(Action<IFileTransferInformation> callback)
        {
            OnExpired += callback;
        }

        public void AddSuccessCallback(Action<IFileTransferInformation> callback)
        {
            OnSuccess += callback;
        }
    }
}
