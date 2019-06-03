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
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Serilog;
using Catalyst.Common.Interfaces.P2P.Messaging.Gossip;
using Catalyst.Node.Core.P2P.Messaging.Gossip;

namespace Catalyst.Node.Core.P2P
{
    public sealed class P2PService
        : UdpServer,
            IP2PService
    {
        public IPeerDiscovery Discovery { get; }
        public IObservable<IChanneledMessage<AnySigned>> MessageStream { get; }

        public P2PService(IPeerSettings settings,
            IPeerDiscovery peerDiscovery,
            IEnumerable<IP2PMessageHandler> messageHandlers,
            ICorrelationManager correlationManager,
            IGossipManager gossipManager)
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            Discovery = peerDiscovery;
            var protoDatagramChannelHandler = new ProtoDatagramChannelHandler();
            var gossipHandler = new GossipHandler(gossipManager);

            var allMessagesStream = 
                protoDatagramChannelHandler.MessageStream.Merge(gossipHandler.MessageStream);
            MessageStream = allMessagesStream;

            var messageHandlerList = messageHandlers.ToList();
            messageHandlerList.ForEach(h => h.StartObserving(allMessagesStream));

            IList<IChannelHandler> channelHandlers = new List<IChannelHandler>
            {
                protoDatagramChannelHandler,
                new CorrelationHandler(correlationManager),
                gossipHandler
            };

            Bootstrap(new InboundChannelInitializerBase<IChannel>(channel => { },
                channelHandlers
            ), settings.BindAddress, settings.Port);

            peerDiscovery.StartObserving(MessageStream);
        }
    }
}
