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
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P.Messaging;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Common.IO.Messaging.Handlers
{
    /// <summary>
    /// Handler for Ask message where you want to manipulate reputation of the recipient depending if they respond/have a correlation.
    /// </summary>
    /// <typeparam name="TProto">The message</typeparam>
    /// <typeparam name="TReputableCache"></typeparam>
    /// <typeparam name="TCounterpartMessage">The counterpart message to the TProto message</typeparam>
    public abstract class ReputableResponseHandlerBase<TProto, TCounterpartMessage, TReputableCache>
        : CorrelatableMessageHandlerBase<TProto, TReputableCache>,
            IReputationAskHandler<TReputableCache>
        where TProto : class, IMessage<TProto>
        where TReputableCache : IMessageCorrelationCache
        where TCounterpartMessage : class, IMessage<TCounterpartMessage>
    {
        public TReputableCache ReputableCache { get; }

        /// <inheritdoc />
        /// <summary>Determines whether this instance [can execute next handler] the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <returns><c>true</c> if this instance [can execute next handler] the specified message; otherwise, <c>false</c>.</returns>
        public bool CanExecuteNextHandler(IChanneledMessage<AnySigned> message)
        {
            var otherMessage = ReputableCache.TryMatchResponse<TCounterpartMessage, TProto>(message.Payload);
            return otherMessage != null;
        }

        protected ReputableResponseHandlerBase(TReputableCache reputableCache,
            ILogger logger)
            : base(reputableCache, logger)
        {
            ReputableCache = reputableCache;
        }
        
        /// <summary>
        ///     Adds a new message to the correlation cache before we flush it away down the socket.
        /// </summary>
        /// <param name="message"></param>
        protected override void Handler(IChanneledMessage<AnySigned> message) { }
    }
}
