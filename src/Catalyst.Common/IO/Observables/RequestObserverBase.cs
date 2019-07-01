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
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Observables
{
    public abstract class RequestObserverBase<TProtoReq, TProtoRes> : MessageObserverBase, IRequestMessageObserver<TProtoRes>
        where TProtoReq : IMessage<TProtoReq> where TProtoRes : IMessage<TProtoRes>
    {
        private readonly string _filterMessageType;
        public IPeerIdentifier PeerIdentifier { get; }
        
        protected RequestObserverBase(ILogger logger, IPeerIdentifier peerIdentifier) : base(logger)
        {
            Guard.Argument(typeof(TProtoReq), nameof(TProtoReq)).Require(t => t.IsRequestType(), 
                t => $"{nameof(TProtoReq)} is not of type {MessageTypes.Request.Name}");
            _filterMessageType = typeof(TProtoReq).ShortenedProtoFullName();
            PeerIdentifier = peerIdentifier;
        }

        protected abstract TProtoRes HandleRequest(IObserverDto<ProtocolMessage> messageDto);

        public override void StartObserving(IObservable<IObserverDto<ProtocolMessage>> messageStream)
        {
            if (MessageSubscription != null)
            {
                throw new ReadOnlyException($"{GetType()} is already listening to a message stream");
            }
            
            MessageSubscription = messageStream
               .Where(m => m.Payload?.TypeUrl != null 
                 && m.Payload?.TypeUrl == _filterMessageType)
               .SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(OnNext, OnError, OnCompleted);
        }
        
        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Verbose("Pre Handle Message Called");
            
            ChannelHandlerContext = messageDto.Context;
            
            //@TODO HandleRequest in try catch if catch send error message.
            var response = HandleRequest(messageDto);
            
            SendChannelContextResponse(new DtoFactory().GetDto(response,
                PeerIdentifier,
                new PeerIdentifier(messageDto.Payload.PeerId),
                messageDto.Payload.CorrelationId.ToGuid()
            ));
        }

        public void SendChannelContextResponse(IMessageDto<TProtoRes> messageDto)
        {   
            ChannelHandlerContext.Channel.WriteAndFlushAsync(messageDto);
        }
    }
}
