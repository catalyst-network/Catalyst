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

using System.Net;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public sealed class P2PMessageDto<TMessage, TMessageType>
        : IP2PMessageDto<TMessage, TMessageType>
        where TMessage : class, IMessage
        where TMessageType : class, IEnumerableMessageType
    {
        public TMessageType Type { get; }
        public TMessage Message { get; }
        public IPEndPoint Recipient { get; }
        public IPeerIdentifier Sender { get; }

        public P2PMessageDto(TMessageType type,
            TMessage message,
            IPEndPoint destination,
            IPeerIdentifier sender)
        {
            Guard.Argument(type, nameof(type)).HasValue();
            Guard.Argument(message, nameof(message)).NotNull().Compatible<TMessage>().HasValue();
            Guard.Argument(destination.Address, nameof(destination.Address)).NotNull().HasValue();
            Guard.Argument(destination.Port, nameof(destination.Port)).InRange(0, 65535);
            Guard.Argument(sender, nameof(sender)).Compatible<IPeerIdentifier>().NotNull().HasValue();
            Type = type;
            Message = message;
            Recipient = destination;
            Sender = sender;
        }
    }
}
