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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Common.IO.Messaging.Handlers
{
    /// <summary>
    /// Handles gossiping of a message
    /// </summary>
    /// <typeparam name="TProto">The type of the proto.</typeparam>
    /// <seealso cref="IGossipMessageHandler" />
    public sealed class GossipMessageHandler<TProto> : IGossipMessageHandler where TProto : class, IMessage<TProto>
    {
        /// <summary>The gossip cache</summary>
        private readonly IGossipCache _gossipCache;

        /// <summary>The peer 2 peer message factory</summary>
        private readonly IP2PMessageFactory<TProto> _messageFactory;
        
        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="GossipMessageHandler{T}"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="gossipCache">The gossip cache.</param>
        /// <param name="messageFactory">The message factory.</param>
        public GossipMessageHandler(IPeerIdentifier peerIdentifier,
            IGossipCache gossipCache,
            IP2PMessageFactory<TProto> messageFactory)
        {
            _gossipCache = gossipCache;
            _messageFactory = messageFactory;
            _peerIdentifier = peerIdentifier;
        }

        /// <inheritdoc/>
        public void Handle(IChanneledMessage<AnySigned> message)
        {
            var correlationId = message.Payload.CorrelationId.ToGuid();
            var gossipCount = _gossipCache.GetGossipCount(correlationId);
            var canGossip = _gossipCache.CanGossip(correlationId);

            if (gossipCount != -1)
            {
                _gossipCache.IncrementReceivedCount(correlationId, 1);
            }

            if (!canGossip)
            {
                return;
            }

            if (gossipCount == -1)
            {
                var request = new GossipRequest
                {
                    SentAt = DateTime.Now,
                    Recipient = new PeerIdentifier(message.Payload.PeerId),
                    Content = message.Payload,
                    ReceivedCount = 0,
                };
                _gossipCache.AddPendingRequest(request);
            }

            Gossip(message);
        }

        /// <summary>Gossips the specified message.</summary>
        /// <param name="message">The message.</param>
        private void Gossip(IChanneledMessage<AnySigned> message)
        {
            var peersToGossip = _gossipCache.GetRandomPeers(Constants.MaxGossipPeersPerRound);
            var deserialised = message.Payload.FromAnySigned<TProto>();
            var correlationId = message.Payload.CorrelationId.ToGuid();
            var channel = message.Context.Channel;

            foreach (var peerIdentifier in peersToGossip)
            {
                var datagramEnvelope = _messageFactory.GetMessageInDatagramEnvelope(deserialised, peerIdentifier, _peerIdentifier,
                    MessageTypes.Gossip, correlationId);
                channel.WriteAndFlushAsync(datagramEnvelope);
            }

            var updateCount = (uint) peersToGossip.Count;
            if (updateCount > 0)
            {
                _gossipCache.IncrementGossipCount(correlationId, updateCount);
            }
        }
    }
}
