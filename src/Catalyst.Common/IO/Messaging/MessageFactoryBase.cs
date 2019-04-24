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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Common.IO.Messaging
{
    public abstract class MessageFactoryBase<TMessage, TMessageType>
        where TMessage : class, IMessage<TMessage>
        where TMessageType : class, IEnumerableMessageType
    {
        /// <summary>
        ///     Switch returns message or throws an exception for unknown message type
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public abstract AnySigned GetMessage(IMessageDto<TMessage, TMessageType> dto);
        
        protected AnySigned BuildTellMessage(IMessageDto<TMessage, TMessageType> dto)
        {
            return dto.Message.ToAnySigned(dto.Sender.PeerId, Guid.NewGuid());
        }

        protected AnySigned BuildAskMessage(IMessageDto<TMessage, TMessageType> dto)
        {
            return dto.Message.ToAnySigned(dto.Sender.PeerId, Guid.NewGuid());
        }

        protected AnySigned BuildGossipMessage(IMessageDto<TMessage, TMessageType> dto)
        {
            return dto.Message.ToAnySigned(dto.Sender.PeerId, Guid.NewGuid());
        }
    }
}
