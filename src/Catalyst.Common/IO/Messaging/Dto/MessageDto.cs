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

using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.P2P;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Common.IO.Messaging.Dto
{
    public sealed class MessageDto<T> : IMessageDto<T> where T : IMessage<T>
    {
        public ICorrelationId CorrelationId { get; }
        public T Message { get; }
        public MessageTypes MessageType { get; }
        public IPeerIdentifier Recipient { get; }
        public IPeerIdentifier Sender { get; }

        /// <summary>
        ///     Data transfer object to wrap up all parameters for sending protocol messages into a MessageFactors.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="correlationId"></param>
        /// <param name="recipient"></param>
        /// <param name="sender"></param>
        public MessageDto(T message,
            IPeerIdentifier sender,
            IPeerIdentifier recipient,
            ICorrelationId correlationId)
        {
            Guard.Argument(message, nameof(message)).Compatible<T>();
            Guard.Argument(recipient.IpEndPoint.Address, nameof(recipient.IpEndPoint.Address)).NotNull();
            Guard.Argument(recipient.Port, nameof(recipient.Port)).InRange(0, 65535);
            Guard.Argument(sender, nameof(sender)).Compatible<IPeerIdentifier>().NotNull();
            MessageType = Enumeration.ParseEndsWith<MessageTypes>(Message.Descriptor.ShortenedFullName());
            CorrelationId = correlationId;
            Message = message;
            Recipient = recipient;
            Sender = sender;
        }
    }
}
