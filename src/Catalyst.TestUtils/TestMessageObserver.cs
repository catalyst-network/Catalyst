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
using Catalyst.Common.Interfaces.P2P.Messaging.Dto;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using NSubstitute;
using Serilog;

namespace Catalyst.TestUtils
{
    public class TestMessageObserver<TProto> : MessageObserverBase,
        IP2PMessageObserver, IRpcResponseMessageObserver, IRpcRequestMessageObserver
        where TProto : IMessage, IMessage<TProto>
    {
        public IObserver<TProto> SubstituteObserver { get; }
        public IPeerIdentifier PeerIdentifier { get; }

        public TestMessageObserver(ILogger logger) : base(logger)
        {
            SubstituteObserver = Substitute.For<IObserver<TProto>>();
        }

        public override void OnError(Exception exception) { SubstituteObserver.OnError(exception); }
        
        public void HandleResponse(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            SubstituteObserver.OnNext(messageDto.Payload.FromProtocolMessage<TProto>());
        }

        public override void OnNext(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            SubstituteObserver.OnNext(messageDto.Payload.FromProtocolMessage<TProto>());
        }
        
        public IMessage HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            return messageDto.Payload.FromProtocolMessage<TProto>();
        }
                
        public override void OnCompleted() { SubstituteObserver.OnCompleted(); }

        public override void StartObserving(IObservable<IProtocolMessageDto<ProtocolMessage>> messageStream)
        {
            if (MessageSubscription != null)
            {
                throw new ReadOnlyException($"{GetType()} is already listening to a message stream");
            }

            var filterMessageType = typeof(TProto).ShortenedProtoFullName() ?? throw new ArgumentNullException(nameof(messageStream));
            
            MessageSubscription = messageStream
               .Where(m => m.Payload?.TypeUrl != null 
                 && m.Payload?.TypeUrl == filterMessageType 
                 && !m.Equals(NullObjects.ProtocolMessageDto) 
                )
               .SubscribeOn(TaskPoolScheduler.Default)
               .Subscribe(OnNext, OnError, OnCompleted);
        }

        public void SendChannelContextResponse(IMessageDto messageDto) { throw new NotImplementedException(); }
    }
}
