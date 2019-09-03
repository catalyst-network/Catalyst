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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Core.IO.Transport.Bootstrapping;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Catalyst.Core.IO.Transport.Channels
{
    public abstract class TcpClientChannelFactory : ITcpClientChannelFactory
    {
        private readonly int _backLogValue;

        protected abstract Func<List<IChannelHandler>> HandlerGenerationFunction { get; }
        protected TcpClientChannelFactory(int backLogValue = 100) { _backLogValue = backLogValue; }
        
        public abstract Task<IObservableChannel> BuildChannel(IEventLoopGroupFactory eventLoopGroupFactory,
            IPAddress targetAddress,
            int targetPort,
            X509Certificate2 certificate = null);

        protected async Task<IChannel> Bootstrap(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress targetAddress, 
            int targetPort,
            X509Certificate2 certificate = null)
        {
            var channelHandler = new ClientChannelInitializerBase<ISocketChannel>(HandlerGenerationFunction,
                handlerEventLoopGroupFactory,
                targetAddress,
                certificate);

            return await new Bootstrap()
               .Group(handlerEventLoopGroupFactory.GetOrCreateSocketIoEventLoopGroup())
               .ChannelFactory(() => new TcpSocketChannel())
               .Option(ChannelOption.SoBacklog, _backLogValue)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .Handler(channelHandler)
               .ConnectAsync(targetAddress, targetPort)
               .ConfigureAwait(false);
        }
    }
}
