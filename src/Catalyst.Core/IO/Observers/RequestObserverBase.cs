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
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.P2P;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Core.IO.Observers
{
    public abstract class RequestObserverBase<TProtoReq, TProtoRes> : MessageObserverBase, IRequestMessageObserver
        where TProtoReq : IMessage<TProtoReq> where TProtoRes : IMessage<TProtoRes>
    {
        public IPeerIdentifier PeerIdentifier { get; }

        protected RequestObserverBase(ILogger logger, IPeerIdentifier peerIdentifier) : base(logger, typeof(TProtoReq).ShortenedProtoFullName())
        {
            Guard.Argument(typeof(TProtoReq), nameof(TProtoReq)).Require(t => t.IsRequestType(),
                t => $"{nameof(TProtoReq)} is not of type {MessageTypes.Request.Name}");
            PeerIdentifier = peerIdentifier;
            logger.Verbose("{interface} instantiated", nameof(IRequestMessageObserver));
        }

        protected abstract TProtoRes HandleRequest(TProtoReq messageDto, IChannelHandlerContext channelHandlerContext, IPeerIdentifier senderPeerIdentifier, ICorrelationId correlationId);

        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Verbose("Pre Handle Message Called");

            try
            {
                var correlationId = messageDto.Payload.CorrelationId.ToCorrelationId();
                var recipientPeerIdentifier = new PeerIdentifier(messageDto.Payload.PeerId);

                var response = HandleRequest(messageDto.Payload.FromProtocolMessage<TProtoReq>(),
                    messageDto.Context,
                    recipientPeerIdentifier,
                    correlationId);

                var responseDto = new MessageDto(
                    response.ToProtocolMessage(PeerIdentifier.PeerId, correlationId),
                    recipientPeerIdentifier);

                messageDto.Context.Channel.WriteAndFlushAsync(responseDto);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to process message");
            }
        }
    }
}
