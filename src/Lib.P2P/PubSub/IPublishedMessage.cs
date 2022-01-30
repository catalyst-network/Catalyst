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
    ///   A published message.
    /// </summary>
    /// <remarks>
    ///   The <see cref="IPubSub"/> is used to publish and subsribe to a message.
    /// </remarks>
    public interface IPublishedMessage : IDataBlock
    {
        /// <summary>
        ///   The sender of the message.
        /// </summary>
        /// <value>
        ///   The peer that sent the message.
        /// </value>
        Peer Sender { get; }

        /// <summary>
        ///   The topics of the message.
        /// </summary>
        /// <value>
        ///   All topics related to this message.
        /// </value>
        IEnumerable<string> Topics { get; }

        /// <summary>
        ///   The sequence number of the message.
        /// </summary>
        /// <value>
        ///   A sender unique id for the message.
        /// </value>
        byte[] SequenceNumber { get; }
    }
}
