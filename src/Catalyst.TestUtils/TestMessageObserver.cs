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
using Catalyst.Core.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Serilog;

namespace Catalyst.TestUtils
{
    public class TestMessageObserver<TProto> : MessageObserverBase,
        IP2PMessageObserver, IRpcResponseObserver, IRpcRequestObserver
        where TProto : IMessage, IMessage<TProto>
    {
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

        public void HandleResponseObserver(IMessage messageDto,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            throw new NotImplementedException();
        }

        public override void OnCompleted() { SubstituteObserver.OnCompleted(); }
    }
}
