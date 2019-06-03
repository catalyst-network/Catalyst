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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Gossip;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using System;

namespace Catalyst.Node.Core.P2P.Messaging.Gossip
{
    /// <summary>
    /// The Gossip Manager used to broadcast and receive gossip messages
    /// </summary>
    /// <seealso cref="IGossipManager" />
    public sealed class GossipManager : IGossipManager
    {
        /// <summary>The gossip cache</summary>
        private readonly IGossipManagerContext _gossipManagerContext;
            
        /// <summary>The message factory</summary>
        private readonly IMessageFactory _messageFactory;

        /// <summary>The peer client factory</summary>
        private readonly IPeerClientFactory _peerClientFactory;

        /// <summary>Initializes a new instance of the <see cref="GossipManager"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="gossipCache">The gossip cache.</param>
        public GossipManager(IPeerClientFactory peerClientFactory, IGossipManagerContext context)
        {
            _peerClientFactory = peerClientFactory;
            _gossipManagerContext = context;
            _messageFactory = new MessageFactory();
        }

        /// <inheritdoc/>
        public void Broadcast(AnySigned anySigned)
        {
            if (anySigned.CheckIfMessageIsGossip())
            {
                throw new NotSupportedException("Cannot broadcast a message which is already a gossip type");
            }

            Gossip(anySigned);
        }

        /// <inheritdoc/>
        public void IncomingGossip(AnySigned anySigned)
        {
            if (!anySigned.CheckIfMessageIsGossip())
            {
                throw new NotSupportedException("The Message is not a gossip type");
            }

            // TODO: Check Gossip inner signature and outer signature
            AnySigned originalGossipedMessage = AnySigned.Parser.ParseFrom(anySigned.Value);
            _gossipManagerContext.GossipCache.IncrementReceivedCount(originalGossipedMessage.CorrelationId.ToGuid(), 1);
        }

        /// <summary>Gossips the specified message.</summary>
        /// <param name="message">The message.</param>
        private void Gossip(AnySigned message)
        {
            var gossipCache = _gossipManagerContext.GossipCache;
            var correlationId = message.CorrelationId.ToGuid();
            var gossipCount = gossipCache.GetGossipCount(correlationId);
            var canGossip = gossipCache.CanGossip(correlationId);
            bool isInCache = gossipCount != -1;

            if (!canGossip)
            {
                return;
            }

            if (!isInCache)
            {
                var request = new GossipRequest
                {
                    SentAt = DateTime.Now,
                    Recipient = new PeerIdentifier(message.PeerId),
                    Content = message,
                    ReceivedCount = 1,
                };
                gossipCache.AddPendingRequest(request);
            }

            SendGossipMessages(message);
        }

        /// <summary>Sends gossips to random peers.</summary>
        /// <param name="message">The message.</param>
        private void SendGossipMessages(AnySigned message)
        {
            var gossipCache = _gossipManagerContext.GossipCache;
            var peersToGossip = gossipCache.GetRandomPeers(Constants.MaxGossipPeersPerRound);
            var correlationId = message.CorrelationId.ToGuid();

            foreach (var peerIdentifier in peersToGossip)
            {
                var datagramEnvelope = _messageFactory.GetDatagramMessage(new MessageDto(message,
                    MessageTypes.Gossip, peerIdentifier, _gossipManagerContext.PeerIdentifier), correlationId);
                 _peerClientFactory.Client.SendMessage(datagramEnvelope);
            }

            var updateCount = (uint) peersToGossip.Count;
            if (updateCount > 0)
            {
                gossipCache.IncrementGossipCount(correlationId, updateCount);
            }
        }
    }
}
