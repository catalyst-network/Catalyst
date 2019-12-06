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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Dawn;
using Google.Protobuf;
using Lib.P2P;
using Polly;
using Polly.Retry;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc cref="IDeltaHub" />
    /// <inheritdoc cref="IDisposable" />
    public class DeltaHub : IDeltaHub
    {
        private readonly IBroadcastManager _broadcastManager;
        private readonly PeerId _peerId;
        private readonly IDfs _dfs;
        private readonly ILogger _logger;

        protected virtual AsyncRetryPolicy<IFileSystemNode> DfsRetryPolicy { get; }

        public DeltaHub(IBroadcastManager broadcastManager,
            IPeerSettings peerSettings,
            IDfs dfs,
            ILogger logger)
        {
            _broadcastManager = broadcastManager;
            _peerId = peerSettings.PeerId;
            _dfs = dfs;
            _logger = logger;

            DfsRetryPolicy = Polly.Policy<IFileSystemNode>.Handle<Exception>()
               .WaitAndRetryAsync(4, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        /// <inheritdoc />
        public void BroadcastCandidate(CandidateDeltaBroadcast candidate)
        {
            Guard.Argument(candidate, nameof(candidate)).NotNull().Require(c => c.IsValid());
            _logger.Information("Broadcasting candidate delta {0}", candidate);

            if (!candidate.ProducerId.Equals(_peerId))
            {
                _logger.Warning($"{nameof(BroadcastCandidate)} " +
                    "should only be called by the producer of a candidate.");
                return;
            }

            var protocolMessage = candidate.ToProtocolMessage(_peerId, CorrelationId.GenerateCorrelationId());
            _broadcastManager.BroadcastAsync(protocolMessage).ConfigureAwait(false);

            _logger.Debug("Broadcast candidate {0} done.", candidate);
        }

        /// <inheritdoc />
        public void BroadcastFavouriteCandidateDelta(FavouriteDeltaBroadcast favourite)
        {
            Guard.Argument(favourite, nameof(favourite)).NotNull().Require(c => c.IsValid());
            
            var protocolMessage = favourite.ToProtocolMessage(_peerId, CorrelationId.GenerateCorrelationId());
            _broadcastManager.BroadcastAsync(protocolMessage).ConfigureAwait(false);

            _logger.Debug("Started broadcasting favourite candidate {0}", favourite);
        }

        /// <inheritdoc />
        public async Task<Cid> PublishDeltaToDfsAndBroadcastAddressAsync(Delta delta,
            CancellationToken cancellationToken = default)
        {
            var newAddress = await PublishDeltaToDfs(delta, cancellationToken).ConfigureAwait(false);
            await BroadcastNewDfsFileAddressAsync(newAddress.Id, delta.PreviousDeltaDfsHash).ConfigureAwait(false);
            return newAddress.Id;
        }

        private async Task<IFileSystemNode> PublishDeltaToDfs(Delta delta, CancellationToken cancellationToken)
        {
            try
            {
                var deltaAsArray = delta.ToByteArray();
                var dfsFileAddress = await DfsRetryPolicy.ExecuteAsync(
                    async c => await PublishDfsFileAsync(deltaAsArray, c).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);

                _logger.Debug("New delta published to DFS at address: {ipfsFileAddress}", dfsFileAddress);
                return dfsFileAddress;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to publish Delta {delta} to ipfs", delta);
                return null;
            }
        }

        private async Task BroadcastNewDfsFileAddressAsync(Cid dfsFileAddress, ByteString previousDeltaHash)
        {
            if (dfsFileAddress == null)
            {
                return;
            }

            try
            {
                _logger.Verbose(
                    "Broadcasting new delta dfs address {dfsAddress} for delta with previous delta hash {previousDeltaHash}",
                    dfsFileAddress, CidHelper.Cast(previousDeltaHash.ToByteArray()));

                var newDeltaHashOnDfs = new DeltaDfsHashBroadcast
                {
                    DeltaDfsHash = dfsFileAddress.ToArray().ToByteString(),
                    PreviousDeltaDfsHash = previousDeltaHash
                }.ToProtocolMessage(_peerId, CorrelationId.GenerateCorrelationId());

                await _broadcastManager.BroadcastAsync(newDeltaHashOnDfs).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to broadcast new dfs address {dfsAddress}");
            }
        }

        private async Task<IFileSystemNode> PublishDfsFileAsync(byte[] deltaAsBytes, CancellationToken cancellationToken)
        {
            using (var memoryStream = new MemoryStream())
            {
                await memoryStream.WriteAsync(deltaAsBytes, cancellationToken).ConfigureAwait(false);
                await memoryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                
                memoryStream.Seek(0, SeekOrigin.Begin);

                return await _dfs.FileSystem.AddAsync(memoryStream, cancel: cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
