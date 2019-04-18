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
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.IO.Messaging;
using Catalyst.Node.Common.Interfaces.IO.Messaging;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Node.Core.Rpc.Messaging
{
    public class RpcMessageFactory<TMessage, TMessageType> : AbstractMessageFactory<TMessage, TMessageType>
        where TMessage : class, IMessage<TMessage>
        where TMessageType : class, IEnumerableMessageType
    {
        public override AnySigned GetMessage(IP2PMessageDto<TMessage, TMessageType> dto)
        {
            if (RpcMessages.GetInfoRequest.Equals(dto.Type))
            {
                return BuildAskMessage(dto);
            }
            
            if (RpcMessages.GetInfoResponse.Equals(dto.Type))
            {
                return BuildTellMessage(dto);
            }
            
            if (RpcMessages.GetMempoolRequest.Equals(dto.Type))
            {
                return BuildAskMessage(dto);
            }
            
            if (RpcMessages.GetMempoolResponse.Equals(dto.Type))
            {
                return BuildTellMessage(dto);
            }
            
            if (RpcMessages.GetVersionRequest.Equals(dto.Type))
            {
                return BuildAskMessage(dto);
            }
            
            if (RpcMessages.GetVersionResponse.Equals(dto.Type))
            {
                return BuildTellMessage(dto);
            }
            
            if (RpcMessages.SignMessageRequest.Equals(dto.Type))
            {
                return BuildAskMessage(dto);
            }
            
            if (RpcMessages.SignMessageResponse.Equals(dto.Type))
            {
                return BuildTellMessage(dto);
            }
            
            throw new ArgumentException("unknown message type");
        }
    }
}
