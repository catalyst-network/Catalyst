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
using Catalyst.Protocol.Wire;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Lib.IO.Observers
{
    public abstract class RequestObserverBase<TProtoReq, TProtoRes> : MessageObserverBase, IRequestMessageObserver
        where TProtoReq : IMessage<TProtoReq> where TProtoRes : IMessage<TProtoRes>
    {
        public IPeerSettings PeerSettings { get; }

        private readonly ILibP2PPeerClient _peerClient;

        protected RequestObserverBase(ILogger logger, IPeerSettings peerSettings, ILibP2PPeerClient peerClient) : base(logger, typeof(TProtoReq).ShortenedProtoFullName())
        {
            Guard.Argument(typeof(TProtoReq), nameof(TProtoReq)).Require(t => t.IsRequestType(),
                t => $"{nameof(TProtoReq)} is not of type {MessageTypes.Request.Name}");
            PeerSettings = peerSettings;
            _peerClient = peerClient;
            logger.Verbose("{interface} instantiated", nameof(IRequestMessageObserver));
        }

        protected abstract TProtoRes HandleRequest(TProtoReq messageDto, IChannelHandlerContext channelHandlerContext, MultiAddress senderPeerId, ICorrelationId correlationId);

        public override void OnNext(ProtocolMessage message)
        {
            Logger.Verbose("Pre Handle Message Called");

            try
            {
                var correlationId = message.CorrelationId.ToCorrelationId();
                var recipientPeerId = new MultiAddress(message.PeerId);

                var response = HandleRequest(message.FromProtocolMessage<TProtoReq>(),
                    null,
                    recipientPeerId,
                    correlationId);

                //var responseDto = new MessageDto(
                //    response.ToProtocolMessage(PeerSettings.Address, correlationId),
                //    recipientPeerId);

                var responseMessage = response.ToProtocolMessage(PeerSettings.Address, correlationId);
                _peerClient.SendMessageAsync(responseMessage, recipientPeerId);

                //todo
                //messageDto.Context.Channel?.WriteAndFlushAsync(responseDto).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to process message");
            }
        }
    }
}
