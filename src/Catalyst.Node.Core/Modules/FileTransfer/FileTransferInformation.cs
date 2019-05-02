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

using Catalyst.Common.FileTransfer;
using DotNetty.Transport.Channels;
using System;
using System.IO;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.P2P;

namespace Catalyst.Node.Core.Modules.FileTransfer
{
    public class FileTransferInformation : IDisposable, IFileTransferInformation
    {
        /// <summary>The time since last chunk</summary>
        private DateTime _timeSinceLastChunk;

        /// <summary>The temporary path</summary>
        private readonly string _tempPath;

        /// <summary>The maximum chunk</summary>
        private readonly uint _maxChunk;

        /// <summary>Occurs when [on expired].</summary>
        private event Action<IFileTransferInformation> _onExpired;

        /// <summary>Occurs when [on success].</summary>
        private event Action<IFileTransferInformation> _onSuccess;

        /// <summary>Initializes a new instance of the <see cref="FileTransferInformation"/> class.</summary>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="uniqueFileName">Temporary unique file name.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="maxChunk">The maximum chunk.</param>
        public FileTransferInformation(IPeerIdentifier recipientIdentifier, IChannel recipientChannel, string uniqueFileName, string fileName, uint maxChunk)
        {
            _tempPath = Path.GetTempPath() + uniqueFileName + ".tmp";
            _maxChunk = maxChunk;
            this.CurrentChunk = 0;
            this.RecipientChannel = recipientChannel;
            this.RecipientIdentifier = recipientIdentifier;
            this.UniqueFileName = uniqueFileName;
            this.FileName = fileName;
        }

        /// <summary>Writes to stream.</summary>
        /// <param name="chunk">The chunk.</param>
        /// <param name="fileBytes">The file bytes.</param>
        public void WriteToStream(uint chunk, byte[] fileBytes)
        {
            this.RandomAccessStream.Seek(0, SeekOrigin.End);
            this.RandomAccessStream.Write(fileBytes);
            this.CurrentChunk = chunk;
            _timeSinceLastChunk = DateTime.Now;
        }

        /// <summary>Initializes this instance.</summary>
        public void Init()
        {
            this.RandomAccessStream = new BinaryWriter(File.Open(_tempPath, FileMode.CreateNew));
            _timeSinceLastChunk = DateTime.Now;
        }

        /// <summary>Determines whether this instance is expired.</summary>
        /// <returns><c>true</c> if this instance is expired; otherwise, <c>false</c>.</returns>
        public bool IsExpired()
        {
            return DateTime.Now.Subtract(_timeSinceLastChunk).TotalMinutes > FileTransferConstants.ExpiryMinutes;
        }

        /// <summary>Determines whether this instance is complete.</summary>
        /// <returns><c>true</c> if this instance is complete; otherwise, <c>false</c>.</returns>
        public bool IsComplete()
        {
            return this.CurrentChunk == this.MaxChunk;
        }

        /// <summary>Cleans up.</summary>
        public void CleanUp()
        {
            this.Dispose();
            this.Delete();
        }

        /// <summary>Deletes the file.</summary>
        public void Delete()
        {
            File.Delete(_tempPath);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>Releases unmanaged and - optionally - managed resources.</summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to
        /// release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            this.RandomAccessStream.Close();
            this.RandomAccessStream.Dispose();
        }

        /// <summary>Executes the on expired.</summary>
        public void ExecuteOnExpired()
        {
            if (_onExpired == null)
            {
                return;
            }

            foreach (var action in _onExpired.GetInvocationList())
            {
                action.DynamicInvoke(this);
            }
        }

        /// <summary>Executes the on success.</summary>
        public void ExecuteOnSuccess()
        {
            if (_onSuccess == null)
            {
                return;
            }

            foreach (var action in _onSuccess.GetInvocationList())
            {
                action.DynamicInvoke(this);
            }
        }
        
        /// <summary>Gets the maximum chunk.</summary>
        /// <value>The maximum chunk.</value>
        public uint MaxChunk => _maxChunk;

        /// <summary>Gets the temporary path.</summary>
        /// <value>The temporary path.</value>
        public string TempPath => _tempPath;

        /// <summary>Gets or sets the current chunk.</summary>
        /// <value>The current chunk.</value>
        public uint CurrentChunk { get; set; }

        /// <summary>Gets or sets the DFS hash.</summary>
        /// <value>The DFS hash.</value>
        public string DfsHash { get; set; }
        
        /// <summary>Gets or sets the name of the unique file.</summary>
        /// <value>The name of the unique file.</value>
        public string UniqueFileName { get; set; }

        /// <summary>Gets or sets the random access stream.</summary>
        /// <value>The random access stream.</value>
        public BinaryWriter RandomAccessStream { get; set; }
        
        /// <summary>Gets or sets the name of the file.</summary>
        /// <value>The name of the file.</value>
        public string FileName { get; set; }

        /// <summary>Gets or sets the recipient channel.</summary>
        /// <value>The recipient channel.</value>
        public IChannel RecipientChannel { get; set; }

        /// <summary>Gets or sets the recipient identifier.</summary>
        /// <value>The recipient identifier.</value>
        public IPeerIdentifier RecipientIdentifier { get; set; }

        public void AddExpiredCallback(Action<IFileTransferInformation> callback)
        {
            _onExpired += callback;
        }

        public void AddSuccessCallback(Action<IFileTransferInformation> callback)
        {
            _onSuccess += callback;
        }
    }
}
