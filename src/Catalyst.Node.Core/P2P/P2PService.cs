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
using System.Reflection;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.IO;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public sealed class P2PService : UdpServer, IP2PService
    {
        private readonly PingRequestHandler _pingRequestHandler;
        private readonly PingResponseHandler _pingResponseHandler;
        private readonly TransactionHandler _transactionHandler;
        private readonly ISocketClientRegistry<IPeerClient> _socketClientRegistry;

        public IPeerDiscovery Discovery { get; }
        public IObservable<IChanneledMessage<AnySigned>> MessageStream { get; }

        public P2PService(IPeerSettings settings,
            IPeerDiscovery peerDiscovery)
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            Discovery = peerDiscovery;
            _socketClientRegistry = new SocketClientRegistry<IPeerClient>();

            IPeerIdentifier peerIdentifier = new PeerIdentifier(settings);
            var protoDatagramChannelHandler = new ProtoDatagramChannelHandler();

            MessageStream = protoDatagramChannelHandler.MessageStream;
            _pingRequestHandler = new PingRequestHandler(MessageStream, peerIdentifier, Logger);
            _pingResponseHandler = new PingResponseHandler(MessageStream, Logger);
            _transactionHandler = new TransactionHandler(MessageStream, Logger);

            IList<IChannelHandler> channelHandlers = new List<IChannelHandler>
            {
                protoDatagramChannelHandler
            };

            Bootstrap(new InboundChannelInitializer<IChannel>(channel => { },
                channelHandlers
            ), settings.BindAddress, settings.Port);
        }
    }
}
