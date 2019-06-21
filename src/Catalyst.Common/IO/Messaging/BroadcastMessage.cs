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

namespace Catalyst.Common.IO.Messaging
{
    /// <summary>
    /// Represents a gossip request to the gossip cache
    /// </summary>
    /// <seealso cref="CorrelatableMessage" />
    public sealed class BroadcastMessage
    {
        /// <summary>Gets or sets the gossip count.</summary>
        /// <value>The amount of messages sent due to gossiping.</value>
        public uint BroadcastCount { get; set; }

        /// <summary>Gets or sets the received count.</summary>
        /// <value>The amount of times the message has been received.</value>
        public uint ReceivedCount { get; set; }

        /// <summary>Gets or sets the size of the peer network.</summary>
        /// <value>The size of the peer network at the moment of creating this request.</value>
        public int PeerNetworkSize { get; set; }
    }
}
