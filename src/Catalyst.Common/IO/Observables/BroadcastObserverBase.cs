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
using Catalyst.Protocol.Common;
using Dawn;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Observables
{
    public abstract class BroadcastObserverBase<TProto> : MessageObserverBase, IBroadcastObserver where TProto : IMessage
    {
        private readonly string _filterMessageType;

        protected BroadcastObserverBase(ILogger logger) : base(logger)
        {
            Guard.Argument(typeof(TProto), nameof(TProto)).Require(t => t.IsBroadcastType(),
                t => $"{nameof(TProto)} is not of type {MessageTypes.Broadcast.Name}");
            _filterMessageType = typeof(TProto).ShortenedProtoFullName();
        }

        public abstract void HandleBroadcast(IObserverDto<ProtocolMessage> messageDto);

        public override void StartObserving(IObservable<IObserverDto<ProtocolMessage>> messageStream)
        {
            if (MessageSubscription != null)
            {
                throw new ReadOnlyException($"{GetType()} is already listening to a message stream");
            }
            
            MessageSubscription = messageStream
               .Where(m => m.Payload?.TypeUrl != null 
                 && m.Payload.TypeUrl == _filterMessageType)
               .SubscribeOn(NewThreadScheduler.Default)
               .Subscribe(OnNext, OnError, OnCompleted);
        }
        
        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            Logger.Verbose("Pre Handle Message Called");
            ChannelHandlerContext = messageDto.Context;
            HandleBroadcast(messageDto);
        }
    }
}
