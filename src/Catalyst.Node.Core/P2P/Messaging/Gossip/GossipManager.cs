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
using System.Collections.Generic;
using System.Net;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Gossip;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;

namespace Catalyst.Node.Core.P2P.Messaging.Gossip
{
    /// <summary>
    /// The Gossip Manager used to broadcast and receive gossip messages
    /// </summary>
    /// <seealso cref="IGossipManager" />
    public class GossipManager : IGossipManager
    {
        /// <summary>The gossip cache</summary>
        private readonly IGossipCache _gossipCache;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>The message factory</summary>
        private readonly P2PMessageFactory _messageFactory;

        /// <summary>The peer settings</summary>
        private readonly IPeerSettings _peerSettings;

        /// <summary>Initializes a new instance of the <see cref="GossipManager"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="reputableCache">The reputable cache.</param>
        /// <param name="gossipCache">The gossip cache.</param>
        /// <param name="peerSettings">The peer settings for the channel</param>
        public GossipManager(IPeerIdentifier peerIdentifier,
            IReputableCache reputableCache,
            IGossipCache gossipCache,
            IPeerSettings peerSettings)
        {
            _gossipCache = gossipCache;
            _peerIdentifier = peerIdentifier;
            _messageFactory = new P2PMessageFactory(reputableCache);
            _peerSettings = peerSettings;
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

            // TODO: Check Gossip inner signature and outer signature #
            AnySigned originalGossipedMessage = AnySigned.Parser.ParseFrom(anySigned.Value);
            _gossipCache.IncrementReceivedCount(originalGossipedMessage.CorrelationId.ToGuid(), 1);
        }

        /// <summary>Gossips the specified message.</summary>
        /// <param name="message">The message.</param>
        private void Gossip(AnySigned message)
        {
            var correlationId = message.CorrelationId.ToGuid();
            var gossipCount = _gossipCache.GetGossipCount(correlationId);
            var canGossip = _gossipCache.CanGossip(correlationId);
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
                _gossipCache.AddPendingRequest(request);
            }

            SendGossipMessages(message);
        }

        /// <summary>Sends gossips to random peers.</summary>
        /// <param name="message">The message.</param>
        private void SendGossipMessages(AnySigned message)
        {
            var peersToGossip = _gossipCache.GetRandomPeers(Constants.MaxGossipPeersPerRound);
            var correlationId = message.CorrelationId.ToGuid();
            IPEndPoint ipEndpoint = new IPEndPoint(_peerSettings.BindAddress, _peerSettings.Port);

            // TODO: Peer client should only be initialized once and re-used throughout the whole lifecycle #447
            using (var peerClient = new PeerClient(ipEndpoint, new List<IP2PMessageHandler>(), this))
            {
                foreach (var peerIdentifier in peersToGossip)
                {
                    var datagramEnvelope = _messageFactory.GetMessageInDatagramEnvelope(new MessageDto(message,
                        MessageTypes.Gossip, peerIdentifier, _peerIdentifier), correlationId);
                    _ = peerClient.SendMessage(datagramEnvelope);
                }
            }

            var updateCount = (uint) peersToGossip.Count;
            if (updateCount > 0)
            {
                _gossipCache.IncrementGossipCount(correlationId, updateCount);
            }
        }
    }
}
