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
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace Catalyst.Common.IO.Messaging.Dto
{
    public sealed class MessageDto<T> : DefaultAddressedEnvelope<T>, IMessageDto<T> where T : IMessage<T>
    {
        public Guid CorrelationId { get; }
        public MessageTypes MessageType { get; }
        public IPeerIdentifier RecipientPeerIdentifier { get; }
        public IPeerIdentifier SenderPeerIdentifier { get; }

        /// <summary>
        ///     Data transfer object to wrap up all parameters for sending protocol messages into a MessageFactors.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="correlationId"></param>
        /// <param name="recipientPeerIdentifier"></param>
        /// <param name="senderPeerIdentifier"></param>
        public MessageDto(T content,
            IPeerIdentifier senderPeerIdentifier,
            IPeerIdentifier recipientPeerIdentifier,
            Guid correlationId = default) : base(content, senderPeerIdentifier.IpEndPoint, recipientPeerIdentifier.IpEndPoint)
        {
            Guard.Argument(content, nameof(content)).Compatible<T>();
            Guard.Argument(recipientPeerIdentifier.IpEndPoint.Address, nameof(recipientPeerIdentifier.IpEndPoint.Address)).NotNull();
            Guard.Argument(recipientPeerIdentifier.Port, nameof(recipientPeerIdentifier.Port)).InRange(0, 65535);
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).Compatible<IPeerIdentifier>().NotNull();
            CorrelationId = correlationId;
            RecipientPeerIdentifier = recipientPeerIdentifier;
            SenderPeerIdentifier = senderPeerIdentifier;
        }
    }
}
