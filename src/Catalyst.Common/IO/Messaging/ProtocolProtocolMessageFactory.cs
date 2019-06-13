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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Common.Interfaces.P2P.Messaging.Dto;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;

namespace Catalyst.Common.IO.Messaging
{
    /// <summary>
    /// The base class to handle building of ProtocolMessage messages
    /// </summary>
    public sealed class ProtocolProtocolMessageFactory : IProtocolMessageFactory
    {
        /// <summary>Gets the message.</summary>
        /// <param name="messageDto">The message.</param>
        /// <param name="correlationId">The originating message correlation message.</param>
        /// <returns>ProtocolMessage message</returns>
        public ProtocolMessage GetMessage(IMessageDto messageDto,
            Guid correlationId = default)
        {
            if (messageDto.MessageType == MessageTypes.Request)
            {
                return BuildRequestMessage(messageDto);
            }
            
            if (messageDto.MessageType == MessageTypes.Response)
            {
                return BuildResponseMessage(messageDto, correlationId);   
            }

            if (messageDto.MessageType == MessageTypes.Broadcast)
            {
                return BuildBroadcastMessage(messageDto);
            }

            throw new ArgumentException();
        }

        /// <summary>Builds the tell message.</summary>
        /// <param name="dto">The dto.</param>
        /// <param name="correlationId">The originating message correlation message.</param>
        /// <returns>ProtocolMessage message</returns>
        private ProtocolMessage BuildResponseMessage(IMessageDto dto, Guid correlationId)
        {
            return correlationId == default
                ? throw new ArgumentException("Correlation ID cannot be null for a tell message")
                : dto.Message.ToProtocolMessage(dto.Sender.PeerId, correlationId);
        }

        /// <summary>Builds the ask message.</summary>
        /// <param name="dto">The dto.</param>
        /// <returns>ProtocolMessage message</returns>
        private ProtocolMessage BuildRequestMessage(IMessageDto dto)
        {
            var messageContent = dto.Message.ToProtocolMessage(dto.Sender.PeerId, Guid.NewGuid());
            return messageContent;
        }

        /// <summary>Builds the gossip message.</summary>
        /// <param name="dto">The dto.</param>
        /// <returns>ProtocolMessage message</returns>
        private ProtocolMessage BuildBroadcastMessage(IMessageDto dto)
        {
            return dto.Message.ToProtocolMessage(dto.Sender.PeerId, Guid.NewGuid());
        }
    }
}
