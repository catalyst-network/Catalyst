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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.IO.Messaging.Broadcast;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Util;
using Google.Protobuf;

namespace Catalyst.Core.Lib.P2P.IO.Messaging.Broadcast
{
    /// <summary>
    /// The Gossip Manager used to broadcast and receive gossip messages
    /// </summary>
    /// <seealso cref="IBroadcastManager" />
    public sealed class BroadcastManager : IBroadcastManager
    {
        /// <summary>The message factory</summary>
        private readonly IDtoFactory _dtoFactory;

        /// <summary>The peers</summary>
        private readonly IPeerRepository _peers;

        /// <summary>The pending requests</summary>
        private readonly IMemoryCache _pendingRequests;

        /// <summary>The entry options</summary>
        private readonly MemoryCacheEntryOptions _entryOptions;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>The peer client</summary>
        private readonly IPeerClient _peerClient;

        /// <summary>This signer is in-charge of adding an extra signature wrapping to the broadcast message</summary>
        private readonly IKeySigner _signer;

        /// <summary>This dictionary will store any original broadcast messages so they can be sent for rebroadcast</summary>
        private readonly ConcurrentDictionary<ICorrelationId, ProtocolMessageSigned> _incomingBroadcastSignatureDictionary;

        /// <summary>Initializes a new instance of the <see cref="BroadcastManager"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peers">The peers.</param>
        /// <param name="memoryCache">The memory cache.</param>
        /// <param name="peerClient">The peer client.</param>
        /// <param name="signer">The signature writer</param>
        public BroadcastManager(IPeerIdentifier peerIdentifier, IPeerRepository peers, IMemoryCache memoryCache, IPeerClient peerClient, IKeySigner signer)
        {
            _peerIdentifier = peerIdentifier;
            _pendingRequests = memoryCache;
            _peers = peers;
            _peerClient = peerClient;
            _signer = signer;
            _incomingBroadcastSignatureDictionary = new ConcurrentDictionary<ICorrelationId, ProtocolMessageSigned>();
            _dtoFactory = new DtoFactory();
            _entryOptions = new MemoryCacheEntryOptions()
               .AddExpirationToken(new CancellationChangeToken(new CancellationTokenSource(TimeSpan.FromMinutes(10)).Token));
        }

        private async Task BroadcastAsync(ProtocolMessageSigned signedMessage)
        {
            var protocolMessage = signedMessage.Message;

            if (protocolMessage.IsBroadCastMessage())
            {
                throw new NotSupportedException("Cannot broadcast a message which is already a gossip type");
            }

            var correlationId = protocolMessage.CorrelationId.ToCorrelationId();
            var gossipRequest = await GetOrCreateAsync(correlationId).ConfigureAwait(false);

            if (!CanBroadcast(gossipRequest))
            {
                return;
            }

            SendBroadcastMessages(protocolMessage, gossipRequest);
        }

        /// <inheritdoc/>
        public async Task BroadcastAsync(ProtocolMessage message)
        {
            var correlationId = message.CorrelationId.ToCorrelationId();
            bool containsOriginalMessage =
                _incomingBroadcastSignatureDictionary.ContainsKey(correlationId);

            if (containsOriginalMessage)
            {
                var originalSignedMessage =
                    _incomingBroadcastSignatureDictionary[correlationId];
                await BroadcastAsync(originalSignedMessage).ConfigureAwait(false);
            }
            else
            {
                // This means the user of this method is the broadcast originator
                // Required to wrap his own message in a signature
                var signature = _signer.Sign(message.ToByteArray());
                var protocolMessageSigned = new ProtocolMessageSigned
                {
                    Signature = signature.SignatureBytes.RawBytes.ToByteString(),
                    Message = message
                };
                await BroadcastAsync(protocolMessageSigned).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public async Task ReceiveAsync(ProtocolMessageSigned protocolSignedMessage)
        {
            var correlationId = protocolSignedMessage.Message.CorrelationId.ToCorrelationId();
            var gossipRequest = await GetOrCreateAsync(correlationId).ConfigureAwait(false);
            gossipRequest.IncrementReceivedCount();
            UpdatePendingRequest(correlationId, gossipRequest);
            _incomingBroadcastSignatureDictionary.GetOrAdd(correlationId, protocolSignedMessage);
        }

        /// <inheritdoc />
        public void RemoveSignedBroadcastMessageData(ICorrelationId correlationId)
        {
            if (_incomingBroadcastSignatureDictionary.ContainsKey(correlationId))
            {
                _incomingBroadcastSignatureDictionary.Remove(correlationId, out _);
            }
        }

        private void SendBroadcastMessages(ProtocolMessage message, BroadcastMessage broadcastMessage)
        {
            try
            {
                var peersToGossip = GetRandomPeers(Constants.MaxGossipPeersPerRound);
                var correlationId = message.CorrelationId.ToCorrelationId();

                foreach (var peerIdentifier in peersToGossip)
                {
                    _peerClient.SendMessage(_dtoFactory.GetDto(
                        message.ToProtocolMessage(peerIdentifier.PeerId, correlationId),
                        peerIdentifier,
                        _peerIdentifier,
                        correlationId)
                    );
                }

                var updateCount = (uint) peersToGossip.Count;
                if (updateCount <= 0)
                {
                    return;
                }

                broadcastMessage.BroadcastCount += updateCount;
                UpdatePendingRequest(correlationId, broadcastMessage);
            }
            catch (Exception e)
            {
                //@TODO log
            }
        }

        /// <summary>Gets the random peers.</summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        private List<IPeerIdentifier> GetRandomPeers(int count)
        {
            return _peers
               .AsQueryable()
               .Select(c => c.DocumentId).Shuffle().Take(count).Select(_peers.Get).Select(p => p.PeerIdentifier).ToList();
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
            _pendingRequests.Set(correlationId.Id, broadcastMessage, _entryOptions);
        }

        /// <summary>Gets the maximum gossip cycles.</summary>
        /// <param name="broadcastMessage"></param>
        /// <returns></returns>
        private uint GetMaxGossipCycles(BroadcastMessage broadcastMessage)
        {
            var peerNetworkSize = broadcastMessage.PeerNetworkSize;
            return (uint) (Math.Log(Math.Max(10, peerNetworkSize) / (double) Constants.MaxGossipPeersPerRound) /
                Math.Max(1, broadcastMessage.BroadcastCount / Constants.MaxGossipPeersPerRound));
        }

        /// <summary>Increments the received count.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        private async Task<BroadcastMessage> GetOrCreateAsync(ICorrelationId correlationId)
        {
            var request = await _pendingRequests.GetOrCreateAsync(correlationId.Id, async entry =>
            {
                entry.SetOptions(_entryOptions);
                var gossipRequest = await Task.FromResult(new BroadcastMessage
                {
                    ReceivedCount = 0,
                    BroadcastCount = 0,
                    PeerNetworkSize = _peers.GetAll().Count()
                }).ConfigureAwait(false);
                entry.Value = gossipRequest;
                return gossipRequest;
            }).ConfigureAwait(false);

            return request;
        }
    }
}
