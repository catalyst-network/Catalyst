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

using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Protocol.Common;

namespace Catalyst.Abstractions.P2P.IO.Messaging.Broadcast
{
    public interface IBroadcastManager
    {
        /// <summary>Broadcasts a message.</summary>
        /// <param name="protocolMessage">Any signed message.</param>
        Task BroadcastAsync(ProtocolMessage protocolMessage);

        /// <summary>Handles Incoming gossip.</summary>
        /// <param name="anySigned">Any signed message.</param>
        Task ReceiveAsync(ProtocolMessageSigned anySigned);

        /// <summary>
        /// Removes the temporary original signed broadcast message data once <see cref="IBroadcastObserver"/>
        /// HandleBroadcast has been called.
        /// </summary>
        /// <param name="correlationId">The correlation identifier.</param>
        void RemoveSignedBroadcastMessageData(ICorrelationId correlationId);
    }
}
