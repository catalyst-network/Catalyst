using System;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Common.IO.Messaging.Handlers
{
    /// <summary>
    /// Handles gossiping of a message
    /// </summary>
    /// <typeparam name="TProto">The type of the proto.</typeparam>
    /// <seealso cref="IGossipMessageHandler" />
    public class GossipMessageHandler<TProto> : IGossipMessageHandler where TProto : class, IMessage<TProto>
    {
        /// <summary>The gossip cache</summary>
        private readonly IGossipCacheBase _gossipCache;

        /// <summary>The peer 2 peer message factory</summary>
        private readonly IP2PMessageFactory<TProto> _messageFactory;
        
        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>Initializes a new instance of the <see cref="GossipMessageHandler{T}"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="gossipCache">The gossip cache.</param>
        /// <param name="messageFactory">The message factory.</param>
        public GossipMessageHandler(IPeerIdentifier peerIdentifier,
            IGossipCacheBase gossipCache,
            IP2PMessageFactory<TProto> messageFactory)
        {
            _gossipCache = gossipCache;
            _messageFactory = messageFactory;
            _peerIdentifier = peerIdentifier;
        }

        /// <inheritdoc/>
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
                        Content = message.Payload,
                        ReceivedCount = 0
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

            if (gossipPeers.Count < 1)
            {
                return;
            }

            gossipPeers.Remove(_peerIdentifier);

            gossipPeers.RemoveRange(0, myPosition);

            var gossipCount = _gossipCache.GetGossipCount(correlationId);
            var deserialised = message.Payload.FromAnySigned<TProto>();
            var amountToGossip = Math.Min(Constants.MaxGossipPeers, Constants.MaxGossipPeers - gossipCount);

            var circularList = new CircularList<IPeerIdentifier>(gossipPeers);

            IPeerIdentifier[] peerIdentifiers =
                circularList.Skip(gossipCount * amountToGossip).Take(amountToGossip);
            foreach (var peerIdentifier in peerIdentifiers)
            {
                var datagramEnvelope = _messageFactory.GetMessageInDatagramEnvelope(deserialised, peerIdentifier, _peerIdentifier,
                    MessageTypes.Gossip, correlationId);
                channel.WriteAndFlushAsync(datagramEnvelope);
            }

            var updateCount = peerIdentifiers.Length;
            if (updateCount > 0)
            {
                _gossipCache.IncrementGossipCount(correlationId, updateCount);
            }
        }
    }
}
