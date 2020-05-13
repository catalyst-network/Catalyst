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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Protocol.Peer;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using MultiFormats;
using System.Net;

namespace Catalyst.Core.Lib.IO.Messaging.Dto
{
    public class BaseMessageDto<T> : DefaultAddressedEnvelope<T>, IMessageDto<T>
        where T : IMessage<T>
    {
        public ICorrelationId CorrelationId { get; }
        public MultiAddress RecipientPeerIdentifier { get; }
        public MultiAddress SenderPeerIdentifier { get; }

        /// <summary>
        ///     Data transfer object to wrap up all parameters for sending protocol messages into a MessageFactors.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="recipientPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected BaseMessageDto(T content,
            MultiAddress senderPeerIdentifier,
            MultiAddress recipientPeerIdentifier,
            ICorrelationId correlationId)
            : base(content, new IPEndPoint(IPAddress.Parse("10.1.1.1"), 10), new IPEndPoint(IPAddress.Parse("10.1.1.1"), 10))
        {
            //todo
            //Guard.Argument(senderPeerIdentifier.IpEndPoint.Address, nameof(senderPeerIdentifier.IpEndPoint.Address)).NotNull();
            //Guard.Argument(recipientPeerIdentifier.IpEndPoint.Address,
            //    nameof(recipientPeerIdentifier.IpEndPoint.Address)).NotNull();

            CorrelationId = correlationId;
            SenderPeerIdentifier = senderPeerIdentifier;
            RecipientPeerIdentifier = recipientPeerIdentifier;
        }
    }
}
