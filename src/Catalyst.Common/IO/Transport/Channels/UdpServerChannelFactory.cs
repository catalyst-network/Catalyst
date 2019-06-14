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
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.Messaging.Broadcast;
using Catalyst.Common.IO.Handlers;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Transport.Channels
{
    public class UdpServerChannelFactory : UdpChannelFactoryBase, IUdpServerChannelFactory
    {
        private readonly IMessageCorrelationManager _messageCorrelationManager;
        private readonly IBroadcastManager _broadcastManager;
        private readonly IKeySigner _keySigner;
        private readonly IPeerSettings _peerSettings;

        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Ignored</param>
        /// <returns></returns>
        public IObservableChannel BuildChannel(IPAddress targetAddress = null,
            int targetPort = 0,
            X509Certificate2 certificate = null) =>
            BootStrapChannel(_peerSettings.BindAddress, _peerSettings.Port);

        public UdpServerChannelFactory(IMessageCorrelationManager messageCorrelationManager,
            IBroadcastManager broadcastManager,
            IKeySigner keySigner,
            IPeerSettings peerSettings)
        {
            _messageCorrelationManager = messageCorrelationManager;
            _broadcastManager = broadcastManager;
            _keySigner = keySigner;
            _peerSettings = peerSettings;
        }

        private List<IChannelHandler> _handlers;

        protected override List<IChannelHandler> Handlers => 
            _handlers ?? (_handlers = new List<IChannelHandler>
            {
                new ProtoDatagramParserHandler(),
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(new ProtocolMessageVerifyHandler(_keySigner), new ProtocolMessageSignHandler(_keySigner)),
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(new CorrelationHandler(_messageCorrelationManager), new CorrelatableHandler(_messageCorrelationManager)),
                new BroadcastHandler(_broadcastManager),
                new ObservableServiceHandler()
            });
    }
}
