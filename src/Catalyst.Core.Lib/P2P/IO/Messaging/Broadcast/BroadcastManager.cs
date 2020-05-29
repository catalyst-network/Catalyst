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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Serilog;
using MultiFormats;

namespace Catalyst.Core.Lib.P2P.IO.Messaging.Broadcast
{
    /// <summary>
    ///     The Gossip Manager used to broadcast and receive gossip messages
    /// </summary>
    /// <seealso cref="Catalyst.Abstractions.P2P.IO.Messaging.Broadcast.IBroadcastManager" />
    public sealed class BroadcastManager : IBroadcastManager
    {
        /// <summary>The peers</summary>
        private readonly IPeerRepository _peers;

        private readonly SigningContext _signingContext;

        /// <summary>The pending requests</summary>
        private readonly IMemoryCache _pendingRequests;

        /// <summary>The entry options</summary>
        private readonly Func<MemoryCacheEntryOptions> _entryOptions;

        /// <summary>The peer identifier</summary>
        private readonly MultiAddress _peerId;

        /// <summary>The peer client</summary>
        private readonly ILibP2PPeerClient _peerClient;

        /// <summary>This signer is in-charge of adding an extra signature wrapping to the broadcast message</summary>
        private readonly IKeySigner _signer;

        /// <summary>This dictionary will store any original broadcast messages so they can be sent for rebroadcast</summary>
        private readonly ConcurrentDictionary<ICorrelationId, ProtocolMessage> _incomingBroadcastSignatureDictionary;

        private readonly ILogger _logger;

        /// <summary>The maximum peers the node can gossip to for a single message, per gossip cycle</summary>
        public static int MaxGossipPeersPerRound => 3;

        /// <summary>The maximum peers a broadcast originator can gossip to for a single message, per gossip cycle.</summary>
        public static int BroadcastOwnerMaximumGossipPeersPerRound => 10;

        /// <summary>Initializes a new instance of the <see cref="BroadcastManager" /> class.</summary>
        /// <param name="peers">The peers.</param>
        /// <param name="peerSettings">Peer settings</param>
        /// <param name="memoryCache">The memory cache.</param>
        /// <param name="peerClient">The peer client.</param>
        /// <param name="signer">The signature writer</param>
        /// <param name="logger"></param>
        public BroadcastManager(IPeerRepository peers,
            IPeerSettings peerSettings,
            IMemoryCache memoryCache,
            ILibP2PPeerClient peerClient,
            IKeySigner signer,
            ILogger logger)
        {
            _logger = logger;
            _peerId = peerSettings.Address;
            _pendingRequests = memoryCache;
            _peers = peers;
            _signingContext = new SigningContext
            {
                NetworkType = peerSettings.NetworkType,
                SignatureType = SignatureType.ProtocolPeer
            };
            _peerClient = peerClient;
            _signer = signer;
            _incomingBroadcastSignatureDictionary = new ConcurrentDictionary<ICorrelationId, ProtocolMessage>();
            _entryOptions = () => new MemoryCacheEntryOptions()
               .AddExpirationToken(
                    new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token));
        }

        private async Task BroadcastInnerAsync(ProtocolMessage signedMessage)
        {
            var innerMessage = signedMessage.FromProtocolMessage<ProtocolMessage>();

            if (innerMessage.IsBroadCastMessage())
            {
                throw new NotSupportedException("Cannot broadcast a message which is already a gossip type");
            }

            var correlationId = innerMessage.CorrelationId.ToCorrelationId();
            var gossipRequest = await GetOrCreateAsync(correlationId).ConfigureAwait(false);

            if (!CanBroadcast(gossipRequest))
            {
                return;
            }

            SendBroadcastMessages(signedMessage, gossipRequest);
        }

        /// <inheritdoc />
        public async Task BroadcastAsync(ProtocolMessage message)
        {
            var correlationId = message.CorrelationId.ToCorrelationId();
            var containsOriginalMessage =
                _incomingBroadcastSignatureDictionary.ContainsKey(correlationId);

            if (containsOriginalMessage)
            {
                var originalSignedMessage =
                    _incomingBroadcastSignatureDictionary[correlationId];
                await BroadcastInnerAsync(originalSignedMessage).ConfigureAwait(false);
            }
            else
            {
                var wrappedMessage = message.ToProtocolMessage(_peerId);
                await BroadcastInnerAsync(wrappedMessage).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task ReceiveAsync(ProtocolMessage protocolSignedMessage)
        {
            var correlationId = protocolSignedMessage.CorrelationId.ToCorrelationId();
            var gossipRequest = await GetOrCreateAsync(correlationId).ConfigureAwait(false);
            gossipRequest.IncrementReceivedCount();
            _logger.Verbose("Received broadcast message {message} {gossipCount} times.", correlationId,
                gossipRequest.ReceivedCount);
            UpdatePendingRequest(correlationId, gossipRequest);
            _incomingBroadcastSignatureDictionary.GetOrAdd(correlationId, protocolSignedMessage);
        }

        /// <inheritdoc />
        public void RemoveSignedBroadcastMessageData(ICorrelationId correlationId)
        {
            _incomingBroadcastSignatureDictionary.TryRemove(correlationId, out _);
        }

        private void SendBroadcastMessages(ProtocolMessage message, BroadcastMessage broadcastMessage)
        {
            try
            {
                var innerMessage = message.FromProtocolMessage<ProtocolMessage>();
                var isOwnerOfBroadcast = innerMessage.PeerId == _peerId.ToString();

                if (isOwnerOfBroadcast)
                {
                    innerMessage = innerMessage.Sign(_signer, _signingContext);
                    message.Value = innerMessage.ToByteString();
                }

                // The fan out is how many peers to broadcast to
                var fanOut = isOwnerOfBroadcast
                    ? BroadcastOwnerMaximumGossipPeersPerRound
                    : (int)Math.Max(GetMaxGossipCycles(broadcastMessage), MaxGossipPeersPerRound);

                var peersToGossip = GetRandomPeers(fanOut);

                var correlationId = innerMessage.CorrelationId.ToCorrelationId();

                //CLEAN UP
                foreach (var peerIdentifier in peersToGossip)
                {
                    _logger.Verbose("Broadcasting message {message}", message);
                    var protocolMessage = message.Clone();
                    protocolMessage.PeerId = _peerId.ToString();
                    _peerClient.SendMessageAsync(
                        protocolMessage,
                        peerIdentifier
                    );
                }

                var updateCount = (uint)peersToGossip.Count;
                if (updateCount <= 0)
                {
                    return;
                }

                broadcastMessage.BroadcastCount += updateCount;
                UpdatePendingRequest(correlationId, broadcastMessage);
            }
            catch (Exception e)
            {
                _logger.Error(e, nameof(SendBroadcastMessages));
            }
        }

        /// <summary>Gets the random peers.</summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private List<MultiAddress> GetRandomPeers(int count)
        {
            return _peers.GetRandomPeers(count).Select(p => p.Address).ToList();
        }

        /// <summary>Determines whether this instance can gossip the specified correlation identifier.</summary>
        /// <param name="request">The gossip request</param>
        /// <returns><c>true</c> if this instance can gossip the specified correlation identifier; otherwise, <c>false</c>.</returns>
        private bool CanBroadcast(BroadcastMessage request)
        {
            return request.BroadcastCount < GetMaxGossipCycles(request);
        }

        /// <summary>Adds the gossip request.</summary>
        /// <param name="broadcastMessage">The gossip request.</param>
        /// <param name="correlationId">The message correlation ID</param>
        private void UpdatePendingRequest(ICorrelationId correlationId, BroadcastMessage broadcastMessage)
        {
            _pendingRequests.Set(correlationId.Id, broadcastMessage, _entryOptions());
        }

        /// <summary>Gets the maximum gossip cycles.</summary>
        /// <param name="broadcastMessage"></param>
        /// <returns></returns>
        private static uint GetMaxGossipCycles(BroadcastMessage broadcastMessage)
        {
            var peerNetworkSize = broadcastMessage.PeerNetworkSize;
            return (uint)(Math.Log(Math.Max(10, peerNetworkSize) / (double)MaxGossipPeersPerRound) /
                Math.Max(1, broadcastMessage.BroadcastCount / MaxGossipPeersPerRound));
        }

        /// <summary>Increments the received count.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        private async Task<BroadcastMessage> GetOrCreateAsync(ICorrelationId correlationId)
        {
            var request = await _pendingRequests.GetOrCreateAsync(correlationId.Id, async entry =>
            {
                entry.SetOptions(_entryOptions());
                var gossipRequest = await Task.FromResult(new BroadcastMessage
                {
                    ReceivedCount = 0,
                    BroadcastCount = 0,
                    PeerNetworkSize = _peers.Count()
                }).ConfigureAwait(false);
                entry.Value = gossipRequest;
                return gossipRequest;
            }).ConfigureAwait(false);

            return request;
        }
    }
}
