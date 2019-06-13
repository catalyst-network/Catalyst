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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using NSubstitute;
using Serilog;

namespace Catalyst.TestUtils
{
    public class TestMessageMessageObserver<TProto> : MessageObserverBase,
        IP2PMessageMessageObserver, IRpcResponseMessageObserver, IRpcRequestMessageObserver
        where TProto : IMessage, IMessage<TProto>
    {
        public IObserver<TProto> SubstituteObserver { get; }

        public TestMessageMessageObserver(ILogger logger) : base(logger)
        {
            SubstituteObserver = Substitute.For<IObserver<TProto>>();
        }

        public override void OnError(Exception exception) { SubstituteObserver.OnError(exception); }
        public override void StartObserving(IObservable<IProtocolMessageDto<ProtocolMessage>> messageStream) { throw new NotImplementedException(); }

        public override void OnNext(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            SubstituteObserver.OnNext(messageDto.Payload.FromProtocolMessage<TProto>());
        }
        
        public override void OnCompleted() { SubstituteObserver.OnCompleted(); }
    }
}
