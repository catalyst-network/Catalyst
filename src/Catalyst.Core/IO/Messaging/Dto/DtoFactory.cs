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
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Core.IO.Messaging.Dto
{
    /// <summary>
    /// The base class to handle building of ProtocolMessage messages
    /// </summary>
    public sealed class DtoFactory : IDtoFactory
    {
        /// <summary>Gets the message.</summary>
        /// <param name="message">Should be an IMessage with type.</param>
        /// <param name="senderPeerIdentifier">The senders PeerIdentifier</param>
        /// <param name="recipientPeerIdentifier">The recipients PeerIdentifier</param>
        /// <param name="correlationId">The correlation id of the originating message.</param>
        /// <returns>IMessageDto</returns>
        public IMessageDto<T> GetDto<T>(T message,
            IPeerIdentifier senderPeerIdentifier,
            IPeerIdentifier recipientPeerIdentifier,
            ICorrelationId correlationId = default) where T : IMessage<T>
        {
            if (message.Descriptor.Name.EndsWith(MessageTypes.Request.Name))
            {
                return BuildRequestMessage(message, senderPeerIdentifier, recipientPeerIdentifier);
            }
            
            if (message.Descriptor.Name.EndsWith(MessageTypes.Response.Name))
            {
                return BuildResponseMessage(message, senderPeerIdentifier, recipientPeerIdentifier, correlationId);   
            }

            if (message.Descriptor.Name.EndsWith(MessageTypes.Broadcast.Name) || message.Descriptor.Name == nameof(ProtocolMessage))
            {
                return BuildBroadcastMessage(message, senderPeerIdentifier, recipientPeerIdentifier, correlationId);
            }

            throw new ArgumentException("Cannot resolve message type");
        }

        /// <summary>Builds the tell message.</summary>
        /// <param name="message">The dto.</param>
        /// <param name="recipientPeerIdentifier"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId">The correlation id of the originating message.</param>
        /// <returns>ProtocolMessage message</returns>
        private IMessageDto<T> BuildResponseMessage<T>(T message, 
            IPeerIdentifier senderPeerIdentifier,
            IPeerIdentifier recipientPeerIdentifier,
            ICorrelationId correlationId) 
            where T : IMessage<T>
        {
            Guard.Argument(correlationId.Id, nameof(correlationId)).NotDefault();
            
            return new MessageDto<T>(message,
                senderPeerIdentifier,
                recipientPeerIdentifier,
                correlationId
            );
        }

        /// <summary>Builds the ask message.</summary>
        /// <param name="message">The dto.</param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="recipientPeerIdentifier"></param>
        /// <returns>ProtocolMessage message</returns>
        private IMessageDto<T> BuildRequestMessage<T>(T message,
            IPeerIdentifier senderPeerIdentifier,
            IPeerIdentifier recipientPeerIdentifier)
            where T : IMessage<T>
        {
            return new MessageDto<T>(message,
                senderPeerIdentifier,
                recipientPeerIdentifier,
                CorrelationId.GenerateCorrelationId()
            );
        }

        /// <summary>Builds the gossip message.</summary>
        /// <param name="message">The dto.</param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="recipientPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        /// <returns>ProtocolMessage message</returns>
        private IMessageDto<T> BuildBroadcastMessage<T>(T message,
            IPeerIdentifier senderPeerIdentifier,
            IPeerIdentifier recipientPeerIdentifier,
            ICorrelationId correlationId)
            where T : IMessage<T>
        {
            return new MessageDto<T>(message,
                senderPeerIdentifier,
                recipientPeerIdentifier,
                correlationId ?? CorrelationId.GenerateCorrelationId()
            );
        }
    }
}
