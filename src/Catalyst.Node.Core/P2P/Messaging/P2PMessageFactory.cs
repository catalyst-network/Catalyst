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
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public sealed class P2PMessageFactory<TMessage>
        : MessageFactoryBase<TMessage> 
        where TMessage : class, IMessage<TMessage>
    {
        /// <summary>Gets the message in datagram envelope.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <returns></returns>
        public IByteBufferHolder GetMessageInDatagramEnvelope(TMessage message, IPeerIdentifier recipient, IPeerIdentifier sender, DtoMessageType messageType)
        {
            return GetMessage(message, recipient, sender, messageType).ToDatagram(recipient.IpEndPoint);
        }

        /// <summary>Gets the message.</summary>
        /// <param name="message">The message.</param>
        /// <param name="recipient">The recipient.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">unknown message type</exception>
        public override AnySigned GetMessage(TMessage message, IPeerIdentifier recipient, IPeerIdentifier sender, DtoMessageType messageType)
        {
            var dto = new P2PMessageDto<TMessage>(message, recipient, sender);

            switch (messageType)
            {
                case DtoMessageType.Ask:
                    return BuildAskMessage(dto);
                case DtoMessageType.Tell:
                    return BuildTellMessage(dto);
                case DtoMessageType.Gossip:
                    return BuildGossipMessage(dto);
            }

            throw new ArgumentException("unknown message type");
        }
    }
}
