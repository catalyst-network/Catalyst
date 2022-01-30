#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.IO.Observers;
using Catalyst.Protocol.Wire;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using MultiFormats;
using Serilog;
using System;

namespace Catalyst.Core.Lib.Rpc.IO
{
    public abstract class RpcResponseObserver<TProto> : MessageObserverBase<IObserverDto<ProtocolMessage>>, IRpcResponseObserver
        where TProto : IMessage<TProto>
    {
        private static Func<IObserverDto<ProtocolMessage>, bool> FilterExpression = m => m?.Payload?.TypeUrl != null && m.Payload.TypeUrl == typeof(TProto).ShortenedProtoFullName();

        protected RpcResponseObserver(ILogger logger, bool assertMessageNameCheck = true) : base(logger, FilterExpression)
        {
            if (assertMessageNameCheck)
            {
                Guard.Argument(typeof(TProto), nameof(TProto)).Require(t => t.IsResponseType(),
                    t => $"{nameof(TProto)} is not of type {MessageTypes.Response.Name}");
            }
        }

        protected abstract void HandleResponse(TProto messageDto, IChannelHandlerContext channelHandlerContext, MultiAddress senderAddress, ICorrelationId correlationId);

        public void HandleResponseObserver(IMessage message,
            IChannelHandlerContext channelHandlerContext,
            MultiAddress sender,
            ICorrelationId correlationId)
        {
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(sender, nameof(sender)).NotNull();
            Guard.Argument(message, nameof(message)).NotNull("The message cannot be null");

            HandleResponse((TProto) message, channelHandlerContext, sender, correlationId);
        }

        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Verbose("Pre Handle Message Called");
            try
            {
                HandleResponse(messageDto.Payload.FromProtocolMessage<TProto>(), messageDto.Context,
                    messageDto.Payload.Address, messageDto.Payload.CorrelationId.ToCorrelationId());
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to handle response message");
            }
        }
    }
}
