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

using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Serilog;
using System;
using Catalyst.Common.Interfaces.IO.Messaging;
using NSubstitute;

namespace Catalyst.Node.Core.UnitTests.TestUtils
{
    public class TestP2PMessageHandler<TProto> : MessageHandlerBase<TProto>, IP2PMessageHandler where TProto : IMessage
    {
        private readonly Action<AnySigned> _action;

        public TestP2PMessageHandler(Action<AnySigned> action = null) : base(Substitute.For<ILogger>()) { _action = action; }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            _action?.Invoke(message.Payload);
        }
    }
}
