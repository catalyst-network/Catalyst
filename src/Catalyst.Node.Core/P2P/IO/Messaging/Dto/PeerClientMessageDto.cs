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

using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.IPPN;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.IO.Messaging.Dto
{
    public interface IPeerClientMessageDto<T> where T : IMessage<T>
    {
        IPeerIdentifier Sender { get; set; }
        T Message { get; set; }
    }

    public sealed class PeerClientMessageDto<T> : IPeerClientMessageDto<T> where T : IMessage<T>
    {
        public IPeerIdentifier Sender { get; set; }
        public T Message { get; set; }

        public PeerClientMessageDto(T message, IPeerIdentifier sender)
        {
            Guard.Argument(message, nameof(message))
               .Require(message.GetType().Namespace.Contains("IPPN"));
            Message = message;
            Sender = sender;
        }
    }
}
