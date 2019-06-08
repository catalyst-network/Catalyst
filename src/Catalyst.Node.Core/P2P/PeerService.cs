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
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Protocol.Common;
using Serilog;

namespace Catalyst.Node.Core.P2P
{
    public sealed class PeerService : UdpServer, IPeerService
    {
        public IPeerDiscovery Discovery { get; }
        public IObservable<IChanneledMessage<ProtocolMessage>> MessageStream { get; }

        public PeerService(IUdpServerChannelFactory serverChannelFactory,
            IPeerDiscovery peerDiscovery,
            IEnumerable<IP2PMessageHandler> messageHandlers,
            ILogger logger)
            : base(serverChannelFactory, logger)
        {
            Discovery = peerDiscovery;
            var observableChannel = ChannelFactory.BuildChannel();
            Channel = observableChannel.Channel;

            MessageStream = observableChannel.MessageStream;
            messageHandlers.ToList()
               .ForEach(h => h.StartObserving(MessageStream));

            // peerDiscovery.StartObserving(MessageStream);
        }
    }
}
