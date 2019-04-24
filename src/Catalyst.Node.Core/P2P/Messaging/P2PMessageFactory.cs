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
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public sealed class P2PMessageFactory<TMessage, TMessageType>
        : MessageFactoryBase<TMessage, TMessageType> 
        where TMessage : class, IMessage<TMessage>
        where TMessageType : class, IEnumerableMessageType
    {
        public IByteBufferHolder GetMessageInDatagramEnvelope(IMessageDto<TMessage, TMessageType> dto)
        {
            return GetMessage(dto).ToDatagram(dto.Recipient.IpEndPoint);
        }
        
        public override AnySigned GetMessage(IMessageDto<TMessage, TMessageType> dto)
        {
            if (P2PMessages.PingRequest.Equals(dto.Type))
            {
                return BuildAskMessage(dto);
            }
            
            if (P2PMessages.PingResponse.Equals(dto.Type))
            {
                return BuildTellMessage(dto);
            }
            
            if (P2PMessages.GetNeighbourRequest.Equals(dto.Type))
            {
                return BuildAskMessage(dto);
            }
            
            if (P2PMessages.GetNeighbourResponse.Equals(dto.Type))
            {
                return BuildTellMessage(dto);
            }
            
            if (P2PMessages.BroadcastTransaction.Equals(dto.Type))
            {
                return BuildGossipMessage(dto);
            }
            
            throw new ArgumentException("unknown message type");
        }
    }
}
