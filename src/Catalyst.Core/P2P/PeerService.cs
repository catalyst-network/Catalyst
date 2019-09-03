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
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Core.IO.Transport;
using Catalyst.Protocol.Common;
using Serilog;

namespace Catalyst.Core.P2P
{
    public sealed class PeerService : UdpServer, IPeerService
    {
        private readonly IEnumerable<IP2PMessageObserver> _messageHandlers;
        private readonly IPeerSettings _peerSettings;
        private readonly IPeerHeartbeatChecker _heartbeatChecker;
        public IPeerDiscovery Discovery { get; }
        public IObservable<IObserverDto<ProtocolMessage>> MessageStream { get; private set; }

        public PeerService(IUdpServerEventLoopGroupFactory udpServerEventLoopGroupFactory,
            IUdpServerChannelFactory serverChannelFactory,
            IPeerDiscovery peerDiscovery,
            IEnumerable<IP2PMessageObserver> messageHandlers,
            IPeerSettings peerSettings,
            ILogger logger,
            IPeerHeartbeatChecker heartbeatChecker)
            : base(serverChannelFactory, logger, udpServerEventLoopGroupFactory)
        {
            _messageHandlers = messageHandlers;
            _peerSettings = peerSettings;
            _heartbeatChecker = heartbeatChecker;
            Discovery = peerDiscovery;
        }

        public override async Task StartAsync()
        {
            var observableChannel = await ChannelFactory.BuildChannel(EventLoopGroupFactory, _peerSettings.BindAddress, _peerSettings.Port);
            Channel = observableChannel.Channel;

            MessageStream = observableChannel.MessageStream;
            _messageHandlers.ToList()
               .ForEach(h => h.StartObserving(MessageStream));
            Discovery?.DiscoveryAsync();
            _heartbeatChecker.Run();
        }
    }
}
