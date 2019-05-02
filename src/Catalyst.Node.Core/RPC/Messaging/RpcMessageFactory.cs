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
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;

namespace Catalyst.Node.Core.Rpc.Messaging
{
    public sealed class RpcMessageFactory<TMessage, TMessageType>
        : MessageFactoryBase<TMessage, TMessageType>
        where TMessage : class, IMessage<TMessage>
        where TMessageType : class, IEnumerableMessageType
    {
        public override AnySigned GetMessage(IMessageDto<TMessage, TMessageType> dto)
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

            if (RpcMessages.GetPeerListRequest.Equals(dto.Type))
            {
                return BuildAskMessage(dto);
            }

            if (RpcMessages.GetPeerListResponse.Equals(dto.Type))
            {
                return BuildTellMessage(dto);
            }

            if (RpcMessages.PeerListCountRequest.Equals(dto.Type))
            {
                return BuildAskMessage(dto);
            }

            if (RpcMessages.PeerListCountResponse.Equals(dto.Type))
            {
                return BuildTellMessage(dto);
            }


            if (RpcMessages.RemovePeerRequest.Equals(dto.Type))
            {
                return BuildAskMessage(dto);
            }

            if (RpcMessages.RemovePeerResponse.Equals(dto.Type))
            {
                return BuildTellMessage(dto);
            }
            
            throw new ArgumentException("unknown message type");
        }
    }
}
