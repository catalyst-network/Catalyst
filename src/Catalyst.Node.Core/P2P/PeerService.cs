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
using System.Reactive.Linq;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Node.Core.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Google.Protobuf;

namespace Catalyst.Node.Core.P2P
{
    public abstract class PeerService<TDiscovery> : UdpServer, IPeerService<TDiscovery> where TDiscovery : IPeerDiscovery
    {
        public TDiscovery Discovery { get; }
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { get; }

        public PeerService(IUdpServerEventLoopGroupFactory udpServerEventLoopGroupFactory,
            IUdpServerChannelFactory serverChannelFactory,
            TDiscovery peerDiscovery,
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
