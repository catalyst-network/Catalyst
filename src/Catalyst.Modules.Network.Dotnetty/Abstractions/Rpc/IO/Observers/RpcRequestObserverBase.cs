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
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.IO.Messaging.Dto;
using Catalyst.Protocol.Wire;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using MultiFormats;
using Serilog;

namespace Catalyst.Modules.Network.Dotnetty.Rpc.IO.Observers
{
    public abstract class RpcRequestObserverBase<TProtoReq, TProtoRes> : MessageObserverBase<IObserverDto<ProtocolMessage>>, IRequestMessageObserver<IObserverDto<ProtocolMessage>>
        where TProtoReq : IMessage<TProtoReq> where TProtoRes : IMessage<TProtoRes>
    {
        public IPeerSettings PeerSettings { get; }

        private static Func<IObserverDto<ProtocolMessage>, bool> FilterExpression = m => m?.Payload?.TypeUrl != null && m.Payload.TypeUrl == typeof(TProtoReq).ShortenedProtoFullName();

        protected RpcRequestObserverBase(ILogger logger, IPeerSettings peerSettings) : base(logger, FilterExpression)
        {
            Guard.Argument(typeof(TProtoReq), nameof(TProtoReq)).Require(t => t.IsRequestType(),
                t => $"{nameof(TProtoReq)} is not of type {MessageTypes.Request.Name}");
            PeerSettings = peerSettings;
            logger.Verbose("{interface} instantiated", nameof(IRequestMessageObserver<IObserverDto<ProtocolMessage>>));
        }

        protected abstract TProtoRes HandleRequest(TProtoReq message, IChannelHandlerContext channelHandlerContext, MultiAddress sender, ICorrelationId correlationId);

        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Verbose("Pre Handle Message Called");

            try
            {
                var correlationId = messageDto.Payload.CorrelationId.ToCorrelationId();
                var recipientAddress = messageDto.Payload.Address;

                var response = HandleRequest(messageDto.Payload.FromProtocolMessage<TProtoReq>(),
                    messageDto.Context,
                    recipientAddress,
                    correlationId);

                MessageDto responseDto = new(
                    response.ToProtocolMessage(PeerSettings.Address, correlationId),
                    recipientAddress);

                messageDto.Context.Channel?.WriteAndFlushAsync(responseDto).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to process message");
            }
        }
    }
}
