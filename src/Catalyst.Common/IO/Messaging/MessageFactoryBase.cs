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
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Common.IO.Messaging
{
    /// <summary>
    /// The base class to handle building of AnySigned messages
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public abstract class MessageFactoryBase<TMessage>
        where TMessage : class, IMessage<TMessage>
    {
        /// <summary>Gets the message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns>AnySigned message</returns>
        public virtual AnySigned GetMessage(TMessage message,
            IPeerIdentifier recipient,
            IPeerIdentifier sender,
            MessageTypes messageType,
            Guid correlationId = default)
        {
            var messageDto = GetMessageDto(message, recipient, sender);

            if (messageType == MessageTypes.Ask)
            {
                return BuildAskMessage(messageDto);
            }

            if (messageType == MessageTypes.Tell)
            {
                return BuildTellMessage(messageDto, correlationId);   
            }

            throw new ArgumentException();
        }

        /// <summary>Gets the message dto.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <returns></returns>
        protected abstract IMessageDto<TMessage> GetMessageDto(TMessage message, IPeerIdentifier recipient, IPeerIdentifier sender);

        /// <summary>Builds the tell message.</summary>
        /// <param name="dto">The dto.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns>AnySigned message</returns>
        private AnySigned BuildTellMessage(IMessageDto<TMessage> dto, Guid correlationId)
        {
            return correlationId == default
                ? throw new ArgumentException("Correlation ID cannot be null for a tell message")
                : dto.Message.ToAnySigned(dto.Sender.PeerId, correlationId);
        }

        /// <summary>Builds the ask message.</summary>
        /// <param name="dto">The dto.</param>
        /// <returns>AnySigned message</returns>
        private AnySigned BuildAskMessage(IMessageDto<TMessage> dto)
        {
            return dto.Message.ToAnySigned(dto.Sender.PeerId, Guid.NewGuid());
        }

        /// <summary>Builds the gossip message.</summary>
        /// <param name="dto">The dto.</param>
        /// <returns>AnySigned message</returns>
        protected AnySigned BuildGossipMessage(IMessageDto<TMessage> dto)
        {
            return dto.Message.ToAnySigned(dto.Sender.PeerId, Guid.NewGuid());
        }
    }
}
