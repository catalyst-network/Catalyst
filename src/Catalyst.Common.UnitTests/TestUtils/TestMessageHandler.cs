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
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using NSubstitute;
using Serilog;

namespace Catalyst.Common.UnitTests.TestUtils
{
    public class TestMessageHandler<TProto> : MessageHandlerBase<TProto>,
        IP2PMessageHandler, IRpcResponseHandler, IRpcRequestHandler
        where TProto : IMessage, IMessage<TProto>
    {
        public IObserver<TProto> SubstituteObserver { get; }

        public TestMessageHandler(ILogger logger) : base(logger)
        {
            SubstituteObserver = Substitute.For<IObserver<TProto>>();
        }
        
        protected override void Handler(IChanneledMessage<ProtocolMessage> message)
        {
            SubstituteObserver.OnNext(message.Payload.FromAnySigned<TProto>());
        }

        public override void HandleError(Exception exception) { SubstituteObserver.OnError(exception); }
        public override void HandleCompleted() { SubstituteObserver.OnCompleted(); }
    }
}
