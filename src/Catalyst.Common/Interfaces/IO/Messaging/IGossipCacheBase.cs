using System;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    public interface IGossipCacheBase<T> : IMessageCorrelationCache where T : class, IMessage<T>
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
    }
}
