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

using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Google.Protobuf;

namespace Catalyst.Node.Core.Rpc.Messaging
{
    /// <summary>
    /// Handles generation of messages for RPC
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <seealso cref="MessageFactoryBase{TMessage}" />
    /// <seealso cref="IRpcMessageFactory{TMessage}" />
    public sealed class RpcMessageFactory<TMessage>
        : MessageFactoryBase<TMessage>, IRpcMessageFactory<TMessage> where TMessage : class, IMessage<TMessage>
    {
        /// <inheritdoc />
        protected override IMessageDto<TMessage> GetMessageDto(TMessage message,
            IPeerIdentifier recipient,
            IPeerIdentifier sender)
        {
            return new MessageDto<TMessage>(message, recipient, sender);
        }
    }
}
