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

using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Transport;
using Catalyst.Protocol.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Interfaces.P2P.Discovery;

namespace Catalyst.Core.Lib.P2P
{
    public sealed class PeerService : UdpServer, IPeerService
    {
        public IPeerDiscovery Discovery { get; }
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { get; }

        public PeerService(IUdpServerEventLoopGroupFactory udpServerEventLoopGroupFactory,
            IUdpServerChannelFactory serverChannelFactory,
            IPeerDiscovery peerDiscovery,
            IEnumerable<IP2PMessageObserver> messageHandlers,
            IPeerSettings peerSettings,
            ILogger logger)
            : base(serverChannelFactory, logger, udpServerEventLoopGroupFactory)
        {
            Discovery = peerDiscovery;
            var observableChannel = ChannelFactory.BuildChannel(EventLoopGroupFactory, peerSettings.BindAddress, peerSettings.Port);
            Channel = observableChannel.Channel;
            
            MessageStream = observableChannel.MessageStream;
            messageHandlers.ToList()
               .ForEach(h => h.StartObserving(MessageStream));
        }
    }
}
