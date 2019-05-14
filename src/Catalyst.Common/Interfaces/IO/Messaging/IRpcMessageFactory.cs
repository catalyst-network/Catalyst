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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Common.Interfaces.IO.Messaging
{
    /// <summary>
    /// The RPC message factory
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public interface IRpcMessageFactory<TMessage> where TMessage : class, IMessage
    {
        /// <summary>Gets the message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns>AnySigned message</returns>
        AnySigned GetMessage(TMessage message,
            IPeerIdentifier recipient,
            IPeerIdentifier sender,
            MessageTypes messageType,
            Guid correlationId = default);
    }
}
