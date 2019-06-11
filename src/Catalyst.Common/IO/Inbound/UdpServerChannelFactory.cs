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

using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Gossip;
using Catalyst.Common.IO.Messaging.Handlers;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Inbound
{
    public class UdpServerChannelFactory : UdpChannelFactoryBase, IUdpServerChannelFactory
    {
        private readonly ICorrelationManager _correlationManager;
        private readonly IGossipManager _gossipManager;
        private readonly IKeySigner _keySigner;
        private readonly IPeerSettings _peerSettings;
        private readonly ObservableServiceHandler _observableServiceHandler;

        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Ignored</param>
        /// <returns></returns>
        public IObservableSocket BuildChannel(IPAddress targetAddress = null,
            int targetPort = 0,
            X509Certificate2 certificate = null) =>
            BootStrapChannel(_peerSettings.BindAddress, _peerSettings.Port);

        public UdpServerChannelFactory(ICorrelationManager correlationManager,
            IGossipManager gossipManager,
            IKeySigner keySigner,
            IPeerSettings peerSettings)
        {
            _correlationManager = correlationManager;
            _gossipManager = gossipManager;
            _keySigner = keySigner;
            _peerSettings = peerSettings;
            _observableServiceHandler = new ObservableServiceHandler();
        }

        protected override List<IChannelHandler> Handlers => 
            new List<IChannelHandler>
            {
                new ProtoDatagramHandler(),
                new CorrelationHandler(_correlationManager),
                new GossipHandler(_gossipManager),
                new SignatureHandler(_keySigner),
                _observableServiceHandler
            };
    }
}
