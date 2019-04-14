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
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO;
using DotNetty.Buffers;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public static class MessageFactory<TMessage> where TMessage : class, IMessage<TMessage>
    {
        public static IByteBufferHolder GetMessage(IMessageDto<TMessage> dto)
        {
            switch (dto.Type)
            {
                case MessageType.PingRequest:
                    return BuildAskMessage(dto);
                case MessageType.PingResponse:                    
                    return BuildTellMessage(dto);
                case MessageType.Transaction:                    
                    return BuildGossipMessage(dto);
                default:
                    throw new ArgumentException();
            }
        }

        private static IByteBufferHolder BuildTellMessage(IMessageDto<TMessage> dto)
        {
            return DatagramFactory.Create(
                dto.Message.ToAnySigned(dto.PeerIdentifier.PeerId, Guid.NewGuid()),
                dto.Destination
            );
        }

        private static IByteBufferHolder BuildAskMessage(IMessageDto<TMessage> dto)
        {
            return DatagramFactory.Create(
                dto.Message.ToAnySigned(dto.PeerIdentifier.PeerId, Guid.NewGuid()),
                dto.Destination
            );
        }

        private static IByteBufferHolder BuildGossipMessage(IMessageDto<TMessage> dto)
        {
            return DatagramFactory.Create(
                dto.Message.ToAnySigned(dto.PeerIdentifier.PeerId, Guid.NewGuid()),
                dto.Destination
            );
        }
    }
}
