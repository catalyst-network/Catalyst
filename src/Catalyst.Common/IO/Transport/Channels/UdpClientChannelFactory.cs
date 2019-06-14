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
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Handlers;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO.Transport.Channels
{
    public class UdpClientChannelFactory : UdpChannelFactoryBase, IUdpClientChannelFactory
    {
        private readonly IKeySigner _keySigner;
        private readonly IMessageCorrelationManager _correlationManager;
        
        public UdpClientChannelFactory(IKeySigner keySigner, IMessageCorrelationManager correlationManager)
        {
            _keySigner = keySigner;
            _correlationManager = correlationManager;
        }

        protected override List<IChannelHandler> Handlers =>
            new List<IChannelHandler>
            {
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(new ProtoDatagramDecoderHandler(), new ProtoDatagramEncoderHandler()),
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(new ProtocolMessageVerifyHandler(_keySigner), new ProtocolMessageSignHandler(_keySigner)),
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(new CorrelationHandler(_correlationManager), new CorrelationHandler(_correlationManager))
            };

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress"></param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Ignored</param>
        /// <returns></returns>
        public IObservableChannel BuildChannel(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress = null,
            int targetPort = IPEndPoint.MinPort,
            X509Certificate2 certificate = null)
        {
            return BootStrapChannel(handlerEventLoopGroupFactory, address: targetAddress);
        }
    }
}
