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
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Extensions;
using Catalyst.Core.P2P;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.IO.Observers
{
    public abstract class ResponseObserverBase<TProto> : MessageObserverBase, IResponseMessageObserver where TProto : IMessage<TProto>
    {
        protected ResponseObserverBase(ILogger logger, bool assertMessageNameCheck = true) : base(logger, typeof(TProto).ShortenedProtoFullName())
        {
            if (assertMessageNameCheck)
            {
                Guard.Argument(typeof(TProto), nameof(TProto)).Require(t => t.IsResponseType(),
                    t => $"{nameof(TProto)} is not of type {MessageTypes.Response.Name}");
            }
        }

        protected abstract void HandleResponse(TProto messageDto, IChannelHandlerContext channelHandlerContext, IPeerIdentifier senderPeerIdentifier, ICorrelationId correlationId);

        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Verbose("Pre Handle Message Called");
            try
            {
                HandleResponse(messageDto.Payload.FromProtocolMessage<TProto>(), messageDto.Context,
                    new PeerIdentifier(messageDto.Payload.PeerId), messageDto.Payload.CorrelationId.ToCorrelationId());
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to handle response message");
            }
        }
    }
}
