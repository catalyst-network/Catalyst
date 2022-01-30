#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Modules.Network.Dotnetty.IO.Observers;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using MultiFormats;
using NSubstitute;
using Serilog;

namespace Catalyst.TestUtils
{
    public class TestMessageObserver<TProto> : MessageObserverBase<ProtocolMessage>,
        IP2PMessageObserver
        where TProto : IMessage, IMessage<TProto>
    {
        private static Func<ProtocolMessage, bool> FilterExpression = m => m?.TypeUrl != null && m.TypeUrl == typeof(TProto).ShortenedProtoFullName();

        public IObserver<TProto> SubstituteObserver { get; }
        public MultiAddress Address { get; }
        
        public TestMessageObserver(ILogger logger) : base(logger, FilterExpression)
        {
            SubstituteObserver = Substitute.For<IObserver<TProto>>();
            Address = MultiAddressHelper.GetAddress();
        }

        public override void OnError(Exception exception) { SubstituteObserver.OnError(exception); }
        
        public void HandleResponse(ProtocolMessage message)
        {
            SubstituteObserver.OnNext(message.FromProtocolMessage<TProto>());
        }

        public override void OnNext(ProtocolMessage message)
        {
            SubstituteObserver.OnNext(message.FromProtocolMessage<TProto>());
        }

        public void HandleResponseObserver(IMessage message,
            MultiAddress address,
            ICorrelationId correlationId)
        {
            throw new NotImplementedException();
        }

        public override void OnCompleted() { SubstituteObserver.OnCompleted(); }
    }
}
