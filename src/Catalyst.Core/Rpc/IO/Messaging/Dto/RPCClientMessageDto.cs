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

using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc.IO.Messaging.Dto;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Core.Rpc.IO.Messaging.Dto
{
    public sealed class RpcClientMessageDto<T> : IRpcClientMessageDto<T> where T : IMessage
    {
        public IPeerIdentifier Sender { get; set; }
        public T Message { get; set; }

        public RpcClientMessageDto(T message, IPeerIdentifier sender)
        {
            Guard.Argument(message, nameof(message))
               .Require(message.GetType().Namespace.Contains("Catalyst.Protocol.Rpc"));
            Message = message;
            Sender = sender;
        }
    }
}
