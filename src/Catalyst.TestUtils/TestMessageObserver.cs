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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Dto;
using Catalyst.Common.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using NSubstitute;
using Serilog;

namespace Catalyst.TestUtils
{
    public class TestMessageObserver<TProto> : MessageObserverBase,
        IP2PMessageObserver, IRpcResponseObserver, IRpcRequestObserver
        where TProto : IMessage, IMessage<TProto>
    {
        public IObservable<IRpcClientMessageDto<IMessage>> MessageResponseStream { private set; get; }

        public IObserver<TProto> SubstituteObserver { get; }
        public IPeerIdentifier PeerIdentifier { get; }
        
        public TestMessageObserver(ILogger logger) : base(logger, typeof(TProto).ShortenedProtoFullName())
        {
            SubstituteObserver = Substitute.For<IObserver<TProto>>();
            PeerIdentifier = Substitute.For<IPeerIdentifier>();
        }

        public override void OnError(Exception exception) { SubstituteObserver.OnError(exception); }
        
        public void HandleResponse(IObserverDto<ProtocolMessage> messageDto)
        {
            SubstituteObserver.OnNext(messageDto.Payload.FromProtocolMessage<TProto>());
        }

        public override void OnNext(IObserverDto<ProtocolMessage> messageDto)
        {
            SubstituteObserver.OnNext(messageDto.Payload.FromProtocolMessage<TProto>());
        }
        
        public IMessage HandleRequest(IObserverDto<ProtocolMessage> messageDto)
        {
            return messageDto.Payload.FromProtocolMessage<TProto>();
        }
                
        public override void OnCompleted() { SubstituteObserver.OnCompleted(); }
        
        public void SendChannelContextResponse(IMessageDto<TProto> messageDto) { throw new NotImplementedException(); }
    }
}
