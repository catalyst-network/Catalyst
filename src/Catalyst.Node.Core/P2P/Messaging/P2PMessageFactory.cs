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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;
using DotNetty.Buffers;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public sealed class P2PMessageFactory
        : MessageFactory, IP2PMessageFactory
    {
        public P2PMessageFactory(IReputableCache messageCorrelationCache) : base(messageCorrelationCache) { }

        /// <inheritdoc />
        public IByteBufferHolder GetMessageInDatagramEnvelope(IMessageDto messageDto, Guid correlationId = default)
        {
            return GetMessage(messageDto, correlationId).ToDatagram(messageDto.Recipient.IpEndPoint);
        }

        /// <inheritdoc cref="MessageFactory" />
        public override AnySigned GetMessage(IMessageDto messageDto, Guid correlationId = default)
        {
            return messageDto.MessageType == MessageTypes.Gossip ? BuildGossipMessage(messageDto, correlationId) : base.GetMessage(messageDto, correlationId);
        }
    }
}
