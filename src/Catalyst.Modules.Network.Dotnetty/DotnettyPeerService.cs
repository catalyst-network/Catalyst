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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.EventLoop;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Transport.Channels;
using Catalyst.Modules.Network.Dotnetty.IO.Transport;
using Catalyst.Protocol.Wire;
using Serilog;

namespace Catalyst.Modules.Network.Dotnetty.P2P
{
    public sealed class DotnettyPeerService : UdpServer, IPeerService
    {
        private readonly IEnumerable<IP2PMessageObserver> _messageHandlers;
        private readonly IPeerSettings _peerSettings;
        private readonly IHealthChecker _healthChecker;
        public IPeerDiscovery Discovery { get; }
        public IObservable<ProtocolMessage> MessageStream { get; private set; }

        public DotnettyPeerService(IUdpServerEventLoopGroupFactory udpServerEventLoopGroupFactory,
            IUdpServerChannelFactory serverChannelFactory,
            IPeerDiscovery peerDiscovery,
            IEnumerable<IP2PMessageObserver> messageHandlers,
            IPeerSettings peerSettings,
            ILogger logger,
            IHealthChecker healthChecker)
            : base(serverChannelFactory, logger, udpServerEventLoopGroupFactory)
        {
            _messageHandlers = messageHandlers;
            _peerSettings = peerSettings;
            _healthChecker = healthChecker;
            Discovery = peerDiscovery;
        }

        public override async Task StartAsync()
        {
            await StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var observableChannel = await ChannelFactory.BuildChannelAsync(EventLoopGroupFactory, _peerSettings.Address).ConfigureAwait(false);
            Channel = observableChannel.Channel;

            MessageStream = observableChannel.MessageStream;
            _messageHandlers.ToList()
               .ForEach(h => h.StartObserving(MessageStream));
            Discovery?.DiscoveryAsync();
            _healthChecker.Run();
        }
    }
}
