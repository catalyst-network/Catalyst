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
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Types;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Common.IO.Messaging.Dto
{
    /// <summary>
    /// The base class to handle building of ProtocolMessage messages
    /// </summary>
    public sealed class DtoFactory : IDtoFactory
    {
        /// <summary>Gets the message.</summary>
        /// <param name="message">Should be an IMessage with type.</param>
        /// <param name="recipientPeerIdentifier">The recipients PeerIdentifier</param>
        /// <returns>IMessageDto</returns>
        public IMessageDto<ProtocolMessage> GetDto(ProtocolMessage message,
            IPeerIdentifier recipientPeerIdentifier)
        {
            if (message.TypeUrl.EndsWith(MessageTypes.Request.Name))
            {
                return BuildRequestMessage(message, recipientPeerIdentifier);
            }
            
            if (message.TypeUrl.EndsWith(MessageTypes.Response.Name))
            {
                return BuildResponseMessage(message, recipientPeerIdentifier);   
            }

            if (message.TypeUrl.EndsWith(MessageTypes.Broadcast.Name))
            {
                return BuildBroadcastMessage(message, recipientPeerIdentifier);
            }

            throw new ArgumentException("Cannot resolve message type");
        }

        /// <summary>Builds the tell message.</summary>
        /// <param name="message">The dto.</param>
        /// <param name="recipientPeerIdentifier"></param>
        /// <returns>ProtocolMessage message</returns>
        private IMessageDto<ProtocolMessage> BuildResponseMessage(ProtocolMessage message,
            IPeerIdentifier recipientPeerIdentifier)
        {
            //Guard.Argument(correlationId.Id, nameof(correlationId)).NotDefault();
            
            return new MessageDto(message,
                recipientPeerIdentifier
            );
        }

        /// <summary>Builds the ask message.</summary>
        /// <param name="message">The dto.</param>
        /// <param name="recipientPeerIdentifier"></param>
        /// <returns>ProtocolMessage message</returns>
        private IMessageDto<ProtocolMessage> BuildRequestMessage(ProtocolMessage message,
            IPeerIdentifier recipientPeerIdentifier)
        {
            return new MessageDto(message,
                recipientPeerIdentifier
            );
        }

        /// <summary>Builds the gossip message.</summary>
        /// <param name="message">The dto.</param>
        /// <param name="recipientPeerIdentifier"></param>
        /// <returns>ProtocolMessage message</returns>
        private IMessageDto<ProtocolMessage> BuildBroadcastMessage(ProtocolMessage message,
            IPeerIdentifier recipientPeerIdentifier)
        {
            return new MessageDto(message,
                recipientPeerIdentifier
            );
        }
    }
}
