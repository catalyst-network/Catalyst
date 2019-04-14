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
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public interface IMessageDto<TMessage> where TMessage : class, IMessage
    {
        MessageType Type { get; }
        TMessage Message { get; }
        IPeerIdentifier PeerIdentifier { get; }
        IPEndPoint Destination { get; }
    }

    public class MessageDto<TMessage> : IMessageDto<TMessage> where TMessage : class, IMessage
    {
        public MessageType Type { get; }
        public TMessage Message { get; }
        public IPEndPoint Destination { get; }
        public IPeerIdentifier PeerIdentifier { get; }

        public MessageDto(MessageType type,
            TMessage message,
            IPEndPoint destination,
            IPeerIdentifier peerIdentifier)
        {
            Guard.Argument(type, nameof(type)).HasValue();
            Guard.Argument(message, nameof(message)).NotNull().Compatible<TMessage>().HasValue();
            Guard.Argument(destination.Address, nameof(destination.Address)).NotNull().HasValue();
            Guard.Argument(destination.Port, nameof(destination.Port)).InRange(0, 65535);
            Guard.Argument(peerIdentifier, nameof(peerIdentifier)).Compatible<IPeerIdentifier>().NotNull().HasValue();
            Type = type;
            Message = message;
            Destination = destination;
            PeerIdentifier = peerIdentifier;
        }
    }
}
