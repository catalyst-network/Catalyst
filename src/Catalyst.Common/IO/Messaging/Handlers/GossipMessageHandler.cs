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
    public class GossipMessageHandler<T> : MessageHandlerBase<T> where T : class, IMessage<T>
    {
        /// <summary>The maximum gossip peers</summary>
        private const int MaxGossipPeers = 5;

        private readonly IGossipCacheBase<T> _gossipCache;

        private readonly IP2PMessageFactory<T> _p2PMessageFactory;

        private readonly IPeerDiscovery _peerDiscovery;

        private readonly IPeerIdentifier _peerIdentifier;

        public GossipMessageHandler(ILogger logger, IPeerIdentifier peerIdentifier, IPeerDiscovery peerDiscovery, IGossipCacheBase<T> gossipCache, IP2PMessageFactory<T> p2PMessageFactory) : base(logger)
        {
            this._peerDiscovery = peerDiscovery;
            this._gossipCache = gossipCache;
            this._p2PMessageFactory = p2PMessageFactory;
            this._peerIdentifier = peerIdentifier;
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
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
        
        public void Gossip(IChanneledMessage<AnySigned> message)
        {
            int myPosition = _gossipCache.GetCurrentPosition();
            var gossipPeers = _peerDiscovery.Peers.ToList();
            var correlationId = message.Payload.CorrelationId.ToGuid();
            var channel = message.Context.Channel;

            if (gossipPeers.Count < 2)
            {
                return;
            }

            gossipPeers.Sort();
            gossipPeers.RemoveRange(0, myPosition);

            var gossipCount = _gossipCache.GetGossipCount(correlationId);
            var deserialised = message.Payload.FromAnySigned<T>();
            var amountToGossip = Math.Min(MaxGossipPeers, MaxGossipPeers - gossipCount);
            IEnumerable<IPeerIdentifier> peerIdentifiers =
                gossipPeers.Skip(gossipCount + amountToGossip).Take(amountToGossip).ToList();

            foreach (var peerIdentifier in peerIdentifiers)
            {
                var datagramEnvelope = _p2PMessageFactory.GetMessageInDatagramEnvelope(deserialised, peerIdentifier, _peerIdentifier,
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
