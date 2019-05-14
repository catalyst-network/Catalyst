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
using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Catalyst.Common.IO.Messaging
{
    public class GossipMessageCacheBase<TMessage> 
        : MessageCorrelationCacheBase, IGossipCacheBase<TMessage>
        where TMessage : class, IMessage<TMessage>
    {
        /// <summary>The maximum gossip count</summary>
        private const int MaxGossipCount = 10;

        /// <summary>The maximum gossip peers</summary>
        private const int MaxGossipPeers = 5;

        /// <summary>The peer discovery</summary>
        private readonly IPeerDiscovery _peerDiscovery;

        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        protected GossipMessageCacheBase(IPeerIdentifier peerIdentifier,
            IPeerDiscovery peerDiscovery,
            IMemoryCache cache,
            ILogger logger) : base(cache, logger, TimeSpan.FromMinutes(10))
        {
            this._peerDiscovery = peerDiscovery;
            this._peerIdentifier = peerIdentifier;
        }

        public override void AddPendingRequest(PendingRequest pendingRequest)
        {
            PendingRequests.Set(pendingRequest.Content.CorrelationId.ToGuid() + "gossip", pendingRequest, EntryOptions);
        }

        protected override PostEvictionDelegate GetInheritorDelegate()
        {
            Logger.Fatal("MessageCorrelationCache.GetInheritorDelegate() called without inheritor.");
            throw new NotImplementedException("Inheritors that uses the default constructor must implement the GetInheritorDelegate() method.");
        }

        public bool CanGossip(Guid correlationId)
        {
            var found = PendingRequests.TryGetValue(correlationId + "gossip", out PendingRequest request);

            // Request does not exist, we can gossip this message
            if (!found)
            {
                return true;
            }
            else
            {
                if (request.GossipCount < MaxGossipCount)
                {
                    return true;
                }
            }

            return false;
        }
        
        public void Gossip(IChannel channel, IP2PMessageFactory<TMessage> messageFactoryBase, IChanneledMessage<AnySigned> message, Guid correlationId)
        {
            int myPosition = GetCurrentPosition();
            var gossipPeers = _peerDiscovery.Peers.ToList();

            if (gossipPeers.Count < 2)
            {
                return;
            }

            gossipPeers.Sort();
            gossipPeers.RemoveRange(0, myPosition);

            var pendingRequest = GetPendingRequestValue(correlationId);
            var deserialised = message.Payload.FromAnySigned<TMessage>();
            var amountToGossip = Math.Min(MaxGossipPeers, MaxGossipPeers - pendingRequest.GossipCount);
            IEnumerable<IPeerIdentifier> peerIdentifiers =
                gossipPeers.Skip(pendingRequest.GossipCount * amountToGossip).Take(amountToGossip).ToList();

            foreach (var peerIdentifier in peerIdentifiers)
            {
                var datagramEnvelope = messageFactoryBase.GetMessageInDatagramEnvelope(deserialised, peerIdentifier, _peerIdentifier,
                    MessageTypes.Gossip, correlationId);
                channel.WriteAndFlushAsync(datagramEnvelope);
            }

            var updateCount = peerIdentifiers.Count();
            if (updateCount > 0)
            {
                pendingRequest.GossipCount += updateCount;
                UpdateGossip(correlationId, pendingRequest);
            }
        }

        public override TRequest TryMatchResponse<TRequest, TResponse>(AnySigned response)
        {
            Guard.Argument(response, nameof(response)).NotNull()
               .Require(r => typeof(TResponse).ShortenedProtoFullName().Equals(response.TypeUrl))
               .Require(r => typeof(TRequest).ShortenedProtoFullName().Equals(r.TypeUrl.GetRequestType()));

            var id = response.CorrelationId.ToGuid();
            var found = GetPendingRequestValue(id);

            if (found != null)
            {
                found.RecievedCount += 1;
                UpdateGossip(id, found);
            }

            return found?.Content.FromAnySigned<TRequest>();
        }

        private void UpdateGossip(Guid guid, PendingRequest request)
        {
            PendingRequests.Set(guid + "gossip", request);
        }

        private int GetCurrentPosition()
        {
            List<IPeerIdentifier> fullPeerList = new List<IPeerIdentifier>();
            fullPeerList.AddRange(_peerDiscovery.Peers.ToArray());
            fullPeerList.Add(_peerIdentifier);
            fullPeerList.Sort();
            int peerIdx = fullPeerList.IndexOf(_peerIdentifier);
            return peerIdx;
        }

        private PendingRequest GetPendingRequestValue(Guid guid)
        {
            PendingRequests.TryGetValue(guid + "gossip", out PendingRequest request);
            return request;
        }
    }
}
