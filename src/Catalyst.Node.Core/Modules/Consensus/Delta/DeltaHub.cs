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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Broadcast;
using Catalyst.Common.Protocol;
using Catalyst.Protocol.Delta;
using Dawn;
using Google.Protobuf;
using Polly;
using Polly.Retry;
using Serilog;

namespace Catalyst.Node.Core.Modules.Consensus.Delta
{
    /// <inheritdoc cref="IDeltaHub" />
    /// <inheritdoc cref="IDisposable" />
    public class DeltaHub : IDeltaHub, IDisposable
    {
        private readonly IBroadcastManager _broadcastManager;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly IDeltaVoter _deltaVoter;
        private readonly IDeltaElector _deltaElector;
        private readonly IDfs _dfs;
        private readonly ILogger _logger;
        private IDisposable _incomingCandidateSubscription;
        private IDisposable _incomingFavouriteCandidateSubscription;

        private static readonly AsyncRetryPolicy<string> IpfsRetryPolicy = Policy<string>
           .Handle<Exception>()
           .WaitAndRetryAsync(10, retryAttempt =>
                TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)));

        public DeltaHub(IBroadcastManager broadcastManager,
            IPeerIdentifier peerIdentifier,
            IDeltaVoter deltaVoter,
            IDeltaElector deltaElector,
            IDfs dfs,
            ILogger logger)
        {
            _broadcastManager = broadcastManager;
            _peerIdentifier = peerIdentifier;
            _deltaVoter = deltaVoter;
            _deltaElector = deltaElector;
            _dfs = dfs;
            _logger = logger;
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

            var protocolMessage = candidate.ToProtocolMessage(_peerIdentifier.PeerId, Guid.NewGuid());
            _broadcastManager.BroadcastAsync(protocolMessage);

            _logger.Debug("Started broadcasting candidate {0}", candidate);
        }

        /// <inheritdoc />
        public void BroadcastFavouriteCandidateDelta(byte[] previousDeltaDfsHash)
        {
            if (!_deltaVoter.TryGetFavouriteDelta(previousDeltaDfsHash, out var favourite))
            {
                _logger.Debug("No favourite delta has been retrieved for broadcast.");
                return;
            } 

            var protocolMessage = favourite.ToProtocolMessage(_peerIdentifier.PeerId, Guid.NewGuid());
            _broadcastManager.BroadcastAsync(protocolMessage);

            _logger.Debug("Started broadcasting favourite candidate {0}", favourite);
        }

        /// <inheritdoc />
        public void SubscribeToFavouriteCandidateStream(IObservable<FavouriteDeltaBroadcast> favouriteCandidateStream)
        {
            _incomingFavouriteCandidateSubscription = favouriteCandidateStream.Subscribe(_deltaElector);
            _logger.Debug("Subscribed to favourite candidate delta incoming stream.");
        }

        /// <inheritdoc />
        public void SubscribeToCandidateStream(IObservable<CandidateDeltaBroadcast> candidateStream)
        {
            _incomingCandidateSubscription = candidateStream.Subscribe(_deltaVoter);
            _logger.Debug("Subscribed to candidate delta incoming stream.");
        }

        /// <inheritdoc />
        public async Task<string> PublishDeltaToIpfsAsync(Protocol.Delta.Delta delta, CancellationToken cancellationToken = default)
        {
            Guard.Argument(delta, nameof(delta)).NotNull().Require(c => c.IsValid());

            // https://github.com/catalyst-network/Catalyst.Node/issues/537

            var deltaAsArray = delta.ToByteArray();
            var ipfsFileAddress = await IpfsRetryPolicy.ExecuteAsync(
                async c => await TryPublishIpfsFile(deltaAsArray, cancellationToken: c),
                cancellationToken);

            return ipfsFileAddress;
        }

        public async Task<string> TryPublishIpfsFile(byte[] deltaAsBytes, CancellationToken cancellationToken)
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

        /// <inheritdoc />
        public void SubscribeToDfsDeltaStream(IObservable<byte[]> dfsDeltaAddressStream) { throw new NotImplementedException(); }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _incomingCandidateSubscription?.Dispose();
            _incomingFavouriteCandidateSubscription?.Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
