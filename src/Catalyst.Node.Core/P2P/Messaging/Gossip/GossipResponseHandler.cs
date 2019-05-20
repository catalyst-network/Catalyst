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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Google.Protobuf;
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging.Gossip
{
    /// <inheritdoc cref="ReputableResponseHandlerBase{TProto,TCounterpartMessage,TReputableCache}"/>
    public abstract class GossipResponseHandler<TProto, TCounterpartMessage, TReputableCache> :
        ReputableResponseHandlerBase<TProto, TCounterpartMessage, TReputableCache>, IGossipMessageHandler
        where TProto : class, IMessage<TProto>
        where TReputableCache : IMessageCorrelationCache
        where TCounterpartMessage : class, IMessage<TCounterpartMessage>
    {
        /// <summary>The gossip message handler</summary>
        private readonly IGossipMessageHandler _gossipMessageHandler;

        /// <summary>Initializes a new instance of the <see cref="GossipResponseHandler{TProto,TCounterpartMessage,TReputableCache}"/> class.</summary>
        /// <param name="gossipMessageHandler">The gossip message handler.</param>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        protected GossipResponseHandler(IGossipMessageHandler gossipMessageHandler,
            TReputableCache correlationCache,
            ILogger logger) : base(correlationCache, logger)
        {
            _gossipMessageHandler = gossipMessageHandler;
        }

        /// <inheritdoc cref="ReputableResponseHandlerBase{TProto,TCounterpartMessage,TReputableCache}"/>
        public override void HandleMessage(IChanneledMessage<AnySigned> message)
        {
            base.HandleMessage(message);
            Handle(message);
        }

        /// <inheritdoc cref="IGossipMessageHandler"/>
        public void Handle(IChanneledMessage<AnySigned> message)
        {
            if (CanGossip(message))
            {
                _gossipMessageHandler.Handle(message);
            }
        }

        /// <inheritdoc cref="IGossipMessageHandler"/>
        public IGossipCache GossipCache => _gossipMessageHandler.GossipCache;

        /// <summary>Determines whether this instance can gossip the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <returns><c>true</c> if this instance can gossip the specified message; otherwise, <c>false</c>.</returns>
        public abstract bool CanGossip(IChanneledMessage<AnySigned> message);
    }
}
