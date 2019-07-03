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
using System.Data;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Observers
{
    public abstract class ResponseObserverBase<TProto> : MessageObserverBase, IResponseMessageObserver where TProto : IMessage<TProto>
    {
        private readonly string _filterMessageType;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        protected ResponseObserverBase(ILogger logger) : base(logger)
        {
            Guard.Argument(typeof(TProto), nameof(TProto)).Require(t => t.IsResponseType(),
                t => $"{nameof(TProto)} is not of type {MessageTypes.Response.Name}");
            _filterMessageType = typeof(TProto).ShortenedProtoFullName();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageDto"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected abstract void HandleResponse(TProto messageDto, IChannelHandlerContext channelHandlerContext, IPeerIdentifier senderPeerIdentifier, ICorrelationId correlationId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageStream"></param>
        /// <exception cref="ReadOnlyException"></exception>
        public override void StartObserving(IObservable<IObserverDto<ProtocolMessage>> messageStream)
        {
            if (MessageSubscription != null)
            {
                throw new ReadOnlyException($"{GetType()} is already listening to a message stream");
            }

            MessageSubscription = messageStream
               .Where(m => m.Payload?.TypeUrl != null 
                 && m.Payload?.TypeUrl == _filterMessageType)
               .SubscribeOn(NewThreadScheduler.Default)
               .Subscribe(OnNext, OnError, OnCompleted);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageDto"></param>
        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Verbose("Pre Handle Message Called");
            HandleResponse(messageDto.Payload.FromProtocolMessage<TProto>(), messageDto.Context, new PeerIdentifier(messageDto.Payload.PeerId), messageDto.Payload.CorrelationId.ToCorrelationId());
        }
    }
}
