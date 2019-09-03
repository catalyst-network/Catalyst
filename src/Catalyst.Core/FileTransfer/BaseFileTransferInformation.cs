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
using System.Threading;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Config;
using DotNetty.Transport.Channels;

namespace Catalyst.Core.FileTransfer
{
    public class BaseFileTransferInformation : IFileTransferInformation
    {
        /// <inheritdoc />
        public string TempPath { get; }

        /// <inheritdoc />
        public string DfsHash { get; set; }
        
        /// <inheritdoc />
        public ICorrelationId CorrelationId { get; set; }
        
        /// <inheritdoc />
        public string FileOutputPath { get; set; }

        /// <inheritdoc />
        public uint MaxChunk { get; set; }

        /// <inheritdoc />
        public IChannel RecipientChannel { get; set; }

        /// <inheritdoc />
        public IPeerIdentifier RecipientIdentifier { get; set; }

        /// <inheritdoc />
        public IPeerIdentifier PeerIdentifier { get; set; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; set; }

        /// <inheritdoc />
        public bool Initialised { get; set; }

        /// <summary>Gets or sets the random access stream.</summary>
        /// <value>The random access stream.</value>
        protected Stream RandomAccessStream { get; set; }
        
        /// <summary>The time since last chunk</summary>
        protected DateTime TimeSinceLastChunk { get; set; }
        
        /// <summary>The chunk indicators</summary>
        protected bool[] ChunkIndicators { get; set; }

        /// <summary>The expired flag</summary>
        private bool _expired;
        
        /// <summary>Initializes a new instance of the <see cref="BaseFileTransferInformation"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier</param>
        /// <param name="recipientIdentifier">The recipient identifier.</param>
        /// <param name="recipientChannel">The recipient channel.</param>
        /// <param name="correlationId">The correlation unique identifier.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="fileSize">Size of the file.</param>
        protected BaseFileTransferInformation(IPeerIdentifier peerIdentifier,
            IPeerIdentifier recipientIdentifier,
            IChannel recipientChannel,
            ICorrelationId correlationId,
            string fileName,
            ulong fileSize)
        {
            TempPath = Path.GetTempPath() + correlationId.Id + ".tmp";
            MaxChunk = (uint) Math.Max(1, (int) Math.Ceiling((double) fileSize / Constants.FileTransferChunkSize));
            RecipientChannel = recipientChannel;
            RecipientIdentifier = recipientIdentifier;
            PeerIdentifier = peerIdentifier;
            CorrelationId = correlationId;
            FileOutputPath = fileName;
            ChunkIndicators = new bool[MaxChunk];
            TimeSinceLastChunk = DateTime.Now;
        }
        
        /// <inheritdoc />
        public bool IsExpired()
        {
            return _expired || 
                CancellationToken.IsCancellationRequested ||
                DateTime.Now.Subtract(TimeSinceLastChunk).TotalSeconds > Constants.FileTransferExpirySeconds;
        }

        /// <inheritdoc />
        public bool ChunkIndicatorsTrue()
        {
            return ChunkIndicators.All(indicator => indicator);
        }

        /// <inheritdoc />
        public void CleanUp()
        {
            Dispose();
            Delete();
        }

        /// <inheritdoc />
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
        public void Dispose()
        {
            Dispose(true);
        }

        public bool IsCompleted { get; set; }

        protected virtual void Dispose(bool disposing)
        {
            RandomAccessStream.Close();
            RandomAccessStream.Dispose();
        }

        /// <inheritdoc />
        public void UpdateChunkIndicator(uint chunkId, bool state)
        {
            ChunkIndicators[chunkId] = state;
            if (state)
            {
                TimeSinceLastChunk = DateTime.Now;
            }
        }

        /// <inheritdoc />
        public void Expire()
        {
            _expired = true;
        }

        /// <inheritdoc />
        public int GetPercentage()
        {
            int sentCount = ChunkIndicators.Count(x => x);
            return (int) Math.Ceiling((double) sentCount / ChunkIndicators.Length * 100D);
        }
    }
}
