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

using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Dawn;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public sealed class P2PMessageDto<TMessage>
        : IMessageDto<TMessage>
        where TMessage : class, IMessage
    {
        public TMessage Message { get; }
        public IPeerIdentifier Recipient { get; }
        public IPeerIdentifier Sender { get; }

        public P2PMessageDto(TMessage message,
            IPeerIdentifier recipient,
            IPeerIdentifier sender)
        {
            Guard.Argument(message, nameof(message)).NotNull().Compatible<TMessage>().HasValue();
            Guard.Argument(recipient.IpEndPoint.Address, nameof(recipient.IpEndPoint.Address)).NotNull().HasValue();
            Guard.Argument(recipient.Port, nameof(recipient.Port)).InRange(0, 65535);
            Guard.Argument(sender, nameof(sender)).Compatible<IPeerIdentifier>().NotNull().HasValue();
            Message = message;
            Recipient = recipient;
            Sender = sender;
        }
    }
}
