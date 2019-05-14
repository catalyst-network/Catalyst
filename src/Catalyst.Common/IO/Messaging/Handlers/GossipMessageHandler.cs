using System;
using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Messaging.Handlers
{
    public class GossipMessageHandler<TProto> : IGossipMessageHandler where TProto : class, IMessage<TProto>
    {
        /// <summary>The gossip cache</summary>
        private readonly IGossipCacheBase _gossipCache;

        /// <summary>The peer 2 peer message factory</summary>
        private readonly IP2PMessageFactory<TProto> _messageFactory;

        /// <summary>The peer discovery</summary>
        private readonly IPeerDiscovery _peerDiscovery;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="GossipMessageHandler{T}"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerDiscovery">The peer discovery.</param>
        /// <param name="gossipCache">The gossip cache.</param>
        /// <param name="messageFactory">The message factory.</param>
        public GossipMessageHandler(IPeerIdentifier peerIdentifier,
            IPeerDiscovery peerDiscovery,
            IGossipCacheBase gossipCache,
            IP2PMessageFactory<TProto> messageFactory)
        {
            this._peerDiscovery = peerDiscovery;
            this._gossipCache = gossipCache;
            this._messageFactory = messageFactory;
            this._peerIdentifier = peerIdentifier;
        }

        public void StartGossip(IChanneledMessage<AnySigned> message)
        {
            var correlationId = message.Payload.CorrelationId.ToGuid();
            if (_gossipCache.CanGossip(correlationId))
            {
                if (_gossipCache.GetGossipCount(correlationId) == -1)
                {
                    var request = new PendingRequest
                    {
                        SentAt = DateTime.Now,
                        Recipient = new PeerIdentifier(message.Payload.PeerId),
                        Content = message.Payload
                    };
                    _gossipCache.AddPendingRequest(request);
                }
                else
                {
                    _gossipCache.IncrementReceivedCount(correlationId, 1);
                }

                Gossip(message);
            }
        }

        /// <summary>Gossips the specified message.</summary>
        /// <param name="message">The message.</param>
        private void Gossip(IChanneledMessage<AnySigned> message)
        {
            int myPosition = _gossipCache.GetCurrentPosition();
            var gossipPeers = _gossipCache.GetSortedPeers();
            var correlationId = message.Payload.CorrelationId.ToGuid();
            var channel = message.Context.Channel;

            if (gossipPeers.Count < 2)
            {
                return;
            }

            gossipPeers.Remove(_peerIdentifier);

            gossipPeers.RemoveRange(0, myPosition);

            var gossipCount = _gossipCache.GetGossipCount(correlationId);
            var deserialised = message.Payload.FromAnySigned<TProto>();
            var amountToGossip = Math.Min(Constants.MaxGossipPeers, Constants.MaxGossipPeers - gossipCount);
            List<IPeerIdentifier> peerIdentifiers =
                gossipPeers.Skip(gossipCount * amountToGossip).Take(amountToGossip).ToList();

            if (peerIdentifiers.Count < amountToGossip)
            {

            }

            foreach (var peerIdentifier in peerIdentifiers)
            {
                var datagramEnvelope = _messageFactory.GetMessageInDatagramEnvelope(deserialised, peerIdentifier, _peerIdentifier,
                    MessageTypes.Gossip, correlationId);
                channel.WriteAndFlushAsync(datagramEnvelope);
            }

            var updateCount = peerIdentifiers.Count();
            if (updateCount > 0)
            {
                _gossipCache.IncrementGossipCount(correlationId, updateCount);
            }
        }
    }
}
