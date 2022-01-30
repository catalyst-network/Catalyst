#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using System.Collections.Generic;

namespace Lib.P2P.PubSub
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPubSubService : IService, IPubSub
    {
        /// <summary>
        ///   The local peer.
        /// </summary>
        Peer LocalPeer { get; set; }

        /// <summary>
        ///   Sends and receives messages to other peers.
        /// </summary>
        List<IMessageRouter> Routers { get; set; }

        /// <summary>
        ///   Creates a message for the topic and data.
        /// </summary>
        /// <param name="topic">
        ///   The topic name/id.
        /// </param>
        /// <param name="data">
        ///   The payload of message.
        /// </param>
        /// <returns>
        ///   A unique published message.
        /// </returns>
        /// <remarks>
        ///   The <see cref="PublishedMessage.SequenceNumber"/> is a monitonically 
        ///   increasing unsigned long.
        /// </remarks>
        PublishedMessage CreateMessage(string topic, byte[] data);
    }
}
