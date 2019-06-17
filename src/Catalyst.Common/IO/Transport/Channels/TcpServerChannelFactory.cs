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
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Transport.Bootstrapping;
using Catalyst.Protocol.Common;
using DotNetty.Codecs.Protobuf;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Catalyst.Common.IO.Transport.Channels
{
    public abstract class TcpServerChannelFactory : ITcpServerChannelFactory
    {
        private readonly int _backLogValue;
        protected List<IChannelHandler> _handlers;
        private readonly IPeerSettings _peerSettings;

        protected abstract List<IChannelHandler> Handlers { get; }
        
        protected TcpServerChannelFactory(IPeerSettings peerSettings, int backLogValue = 100)
        {
            _peerSettings = peerSettings;
            _backLogValue = backLogValue;
        }

        /// <param name="handlerEventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Local TLS certificate</param>
        public IObservableChannel BuildChannel(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress = null,
            int targetPort = IPEndPoint.MinPort,
            X509Certificate2 certificate = null) => 
            Bootstrap(certificate, handlerEventLoopGroupFactory);

        private IObservableChannel Bootstrap(X509Certificate2 certificate, IEventLoopGroupFactory handlerEventLoopGroupFactory)
        {
            var supervisorLoopGroup = ((ITcpServerEventLoopGroupFactory) handlerEventLoopGroupFactory)
               .GetOrCreateSupervisorEventLoopGroup();
            var channelHandler = new ServerChannelInitializerBase<IChannel>(Handlers, handlerEventLoopGroupFactory, certificate);

            var channel = new ServerBootstrap()
               .Group(handlerEventLoopGroupFactory.GetOrCreateSocketIoEventLoopGroup(), supervisorLoopGroup)
               .ChannelFactory(() => new TcpServerSocketChannel())
               .Option(ChannelOption.SoBacklog, _backLogValue)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .ChildHandler(channelHandler)
               .BindAsync(_peerSettings.BindAddress, _peerSettings.Port)
               .GetAwaiter()
               .GetResult();

            var messageStream = channel.Pipeline.Get<ObservableServiceHandler>()?.MessageStream;

            return new ObservableChannel(messageStream, channel);
        }
    }
}
