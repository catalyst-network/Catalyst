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
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public sealed class P2PMessageFactory<TMessage>
        : MessageFactoryBase<TMessage> 
        where TMessage : class, IMessage<TMessage>
    {
        public P2PMessageFactory(IReputableCache messageCorrelationCache) : base(messageCorrelationCache) { }

        /// <summary>Gets the message in datagram envelope.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <returns></returns>
        public IByteBufferHolder GetMessageInDatagramEnvelope(TMessage message, IPeerIdentifier recipient, IPeerIdentifier sender, MessageTypes messageType, Guid correlationId = default)
        {
            return GetMessage(message, recipient, sender, messageType, correlationId).ToDatagram(recipient.IpEndPoint);
        }

        /// <inheritdoc />
        /// <summary>Gets the message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        /// <exception cref="T:System.ArgumentException">unknown message type</exception>
        public override AnySigned GetMessage(TMessage message, IPeerIdentifier recipient, IPeerIdentifier sender, MessageTypes messageType, Guid correlationId = default)
        {
            return messageType == MessageTypes.Gossip ? BuildGossipMessage(GetMessageDto(message, recipient, sender)) : base.GetMessage(message, recipient, sender, messageType, correlationId);
        }

        /// <inheritdoc />
        /// <summary>Gets the message dto.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <returns></returns>
        protected override IMessageDto<TMessage> GetMessageDto(TMessage message,
            IPeerIdentifier recipient,
            IPeerIdentifier sender)
        {
            return new P2PMessageDto<TMessage>(message, recipient, sender);
        }
    }
}
