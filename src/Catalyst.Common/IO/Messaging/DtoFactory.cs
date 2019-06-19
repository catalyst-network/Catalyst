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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Dto;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Common.IO.Messaging
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
        public IMessageDto GetDto<T>(IMessage<T> message,
            IPeerIdentifier senderPeerIdentifier,
            IPeerIdentifier recipientPeerIdentifier,
            Guid correlationId = default) where T : IMessage<T>
        {
            //@TODO if IMessagerequest guid should be defualt
            if (message.Descriptor.Name.EndsWith(MessageTypes.Request.Name))
            {
                return BuildRequestMessage(message, senderPeerIdentifier);
            }
            
            if (message.Descriptor.Name.EndsWith(MessageTypes.Response.Name))
            {
                return BuildResponseMessage(message, senderPeerIdentifier, recipientPeerIdentifier, correlationId);   
            }

            if (message.Descriptor.Name.EndsWith(MessageTypes.Broadcast.Name))
            {
                return BuildBroadcastMessage(message, senderPeerIdentifier);
            }

            throw new ArgumentException();
        }

        /// <summary>Builds the tell message.</summary>
        /// <param name="message">The dto.</param>
        /// <param name="recipientPeerIdentifier"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId">The correlation id of the originating message.</param>
        /// <returns>ProtocolMessage message</returns>
        private IMessageDto BuildResponseMessage(IMessage message, IPeerIdentifier recipientPeerIdentifier, IPeerIdentifier senderPeerIdentifier, Guid correlationId)
        {
            Guard.Argument(correlationId, nameof(correlationId)).NotDefault();
            return new MessageDto(message, senderPeerIdentifier, correlationId, recipientPeerIdentifier);
        }

        /// <summary>Builds the ask message.</summary>
        /// <param name="message">The dto.</param>
        /// <param name="senderPeerIdentifier"></param>
        /// <returns>ProtocolMessage message</returns>
        private IMessageDto BuildRequestMessage(IMessage message, IPeerIdentifier senderPeerIdentifier)
        {
            return new MessageDto(message, senderPeerIdentifier, Guid.NewGuid());
        }

        /// <summary>Builds the gossip message.</summary>
        /// <param name="message">The dto.</param>
        /// <param name="senderPeerIdentifier"></param>
        /// <returns>ProtocolMessage message</returns>
        private IMessageDto BuildBroadcastMessage(IMessage message, IPeerIdentifier senderPeerIdentifier)
        {
            return new MessageDto(message, senderPeerIdentifier, Guid.NewGuid());
        }
    }
}
