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
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Util;
using Catalyst.Protocol.Deltas;
using Dawn;
using Google.Protobuf;
using Multiformats.Hash;
using Polly;
using Polly.Retry;
using Serilog;

namespace Catalyst.Core.Consensus.Deltas
{
    /// <inheritdoc cref="IDeltaHub" />
    /// <inheritdoc cref="IDisposable" />
    public class DeltaHub : IDeltaHub
    {
        private readonly IBroadcastManager _broadcastManager;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IDfs _dfs;
        private readonly ILogger _logger;

        protected virtual AsyncRetryPolicy<string> DfsRetryPolicy { get; }

        public DeltaHub(IBroadcastManager broadcastManager,
            IPeerIdentifier peerIdentifier,
            IDfs dfs,
            ILogger logger)
        {
            _broadcastManager = broadcastManager;
            _peerIdentifier = peerIdentifier;
            _dfs = dfs;
            _logger = logger;

            DfsRetryPolicy = Policy<string>.Handle<Exception>()
               .WaitAndRetryAsync(4, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        /// <inheritdoc />
        public void BroadcastCandidate(CandidateDeltaBroadcast candidate)
        {
            Guard.Argument(candidate, nameof(candidate)).NotNull().Require(c => c.IsValid());
            _logger.Information("Broadcasting candidate delta {0}", candidate);

            if (!candidate.ProducerId.Equals(_peerIdentifier.PeerId))
            {
                _logger.Warning($"{nameof(BroadcastCandidate)} " +
                    $"should only be called by the producer of a candidate.");
                return;
            }

            var protocolMessage = candidate.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());
            _broadcastManager.BroadcastAsync(protocolMessage);

            _logger.Debug("Broadcast candidate {0} done.", candidate);
        }

        /// <inheritdoc />
        public void BroadcastFavouriteCandidateDelta(FavouriteDeltaBroadcast favourite)
        {
            Guard.Argument(favourite, nameof(favourite)).NotNull().Require(c => c.IsValid());
            var protocolMessage = favourite.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());
            _broadcastManager.BroadcastAsync(protocolMessage);

            _logger.Debug("Started broadcasting favourite candidate {0}", favourite);
        }

        /// <inheritdoc />
        public async Task<string> PublishDeltaToDfsAndBroadcastAddressAsync(Delta delta, CancellationToken cancellationToken = default)
        {
            var newAddress = await PublishDeltaToDfs(delta, cancellationToken).ConfigureAwait(false);
            await BroadcastNewDfsFileAddressAsync(newAddress, delta.PreviousDeltaDfsHash).ConfigureAwait(false);
            return newAddress;
        }

        private async Task<string> PublishDeltaToDfs(Delta delta, CancellationToken cancellationToken)
        {
            try
            {
                var deltaAsArray = delta.ToByteArray();
                var dfsFileAddress = await DfsRetryPolicy.ExecuteAsync(
                    async c => await PublishDfsFileAsync(deltaAsArray, cancellationToken: c).ConfigureAwait(false),
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

        private async Task BroadcastNewDfsFileAddressAsync(string dfsFileAddress, ByteString previousDeltaHash)
        {
            if (dfsFileAddress == null)
            {
                return;
            }

            try
            {
                _logger.Verbose("Broadcasting new delta dfs address {dfsAddress} for delta with previous delta hash {previousDeltaHash}",
                    dfsFileAddress, previousDeltaHash.AsBase32Address());

                var newDeltaHashOnDfs = new DeltaDfsHashBroadcast
                {
                    DeltaDfsHash = Multihash.Parse(dfsFileAddress).ToBytes().ToByteString(),
                    PreviousDeltaDfsHash = previousDeltaHash
                }.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());

                await _broadcastManager.BroadcastAsync(newDeltaHashOnDfs).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to broadcast new dfs address {dfsAddress}");
            }
        }

        private async Task<string> PublishDfsFileAsync(byte[] deltaAsBytes, CancellationToken cancellationToken)
        {
            using (var memoryStream = new MemoryStream())
            {
                await memoryStream.WriteAsync(deltaAsBytes, cancellationToken).ConfigureAwait(false);
                memoryStream.Flush();
                memoryStream.Seek(0, SeekOrigin.Begin);

                var address = await _dfs.AddAsync(memoryStream, cancellationToken: cancellationToken);

                return address;
            }
        }
    }
}
