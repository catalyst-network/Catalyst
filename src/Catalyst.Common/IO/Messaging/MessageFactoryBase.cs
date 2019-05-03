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
using Catalyst.Common.Enums.Messages;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Common.IO.Messaging
{
    public abstract class MessageFactoryBase<TMessage>
        where TMessage : class, IMessage<TMessage>
    {
        /// <summary>Gets the message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <returns></returns>
        public abstract AnySigned GetMessage(TMessage message, IPeerIdentifier recipient, IPeerIdentifier sender, DtoMessageType messageType);
        
        protected AnySigned BuildTellMessage(IMessageDto<TMessage> dto)
        {
            return dto.Message.ToAnySigned(dto.Sender.PeerId, Guid.NewGuid());
        }

        protected AnySigned BuildAskMessage(IMessageDto<TMessage> dto)
        {
            return dto.Message.ToAnySigned(dto.Sender.PeerId, Guid.NewGuid());
        }

        protected AnySigned BuildGossipMessage(IMessageDto<TMessage> dto)
        {
            return dto.Message.ToAnySigned(dto.Sender.PeerId, Guid.NewGuid());
        }
    }
}
