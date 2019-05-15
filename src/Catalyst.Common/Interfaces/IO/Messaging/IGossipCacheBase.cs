using System;
using System.Collections.Generic;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Outbound;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    public interface IGossipCacheBase
    {
        /// <summary>Determines whether this instance can gossip the specified correlation identifier.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns><c>true</c> if this instance can gossip the specified correlation identifier; otherwise, <c>false</c>.</returns>
        bool CanGossip(Guid correlationId);

        /// <summary>Gets the current position.</summary>
        /// <returns></returns>
        int GetCurrentPosition();

        /// <summary>Gets the gossip count.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns></returns>
        int GetGossipCount(Guid correlationId);

        /// <summary>Increments the gossip count.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="updateCount">The update count.</param>
        void IncrementGossipCount(Guid correlationId, int updateCount);
        
        /// <summary>Increments the received count.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="increment">The increment.</param>
        void IncrementReceivedCount(Guid correlationId, int increment);

        /// <summary>Adds the pending request.</summary>
        /// <param name="request">The request.</param>
        void AddPendingRequest(PendingRequest request);

        /// <summary>Gets the sorted peers.</summary>
        /// <returns></returns>
        List<IPeerIdentifier> GetSortedPeers();
    }
}
