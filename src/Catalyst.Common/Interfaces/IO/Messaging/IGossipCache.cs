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
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Outbound;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    public interface IGossipCache
    {
        /// <summary>Determines whether this instance can gossip the specified correlation identifier.</summary>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns><c>true</c> if this instance can gossip the specified correlation identifier; otherwise, <c>false</c>.</returns>
        bool CanGossip(Guid correlationId);
        
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
        
        /// <summary>Gets the random peers.</summary>
        /// <param name="count">The count.</param>
        /// <returns></returns>
        List<IPeerIdentifier> GetRandomPeers(int count);
    }
}
