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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Transport.Bootstrapping;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.EventLoop;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Transport.Channels;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using MultiFormats;

namespace Catalyst.Modules.Network.Dotnetty.IO.Transport.Channels
{
    public abstract class TcpServerChannelFactory : ITcpServerChannelFactory
    {
        private readonly int _backLogValue;

        protected abstract Func<List<IChannelHandler>> HandlerGenerationFunction { get; }
        
        protected TcpServerChannelFactory(int backLogValue = 100)
        {
            _backLogValue = backLogValue;
        }
        
        public abstract Task<IObservableChannel> BuildChannelAsync(IEventLoopGroupFactory eventLoopGroupFactory,
            MultiAddress address,
            X509Certificate2 certificate = null);

        protected async Task<IChannel> BootstrapAsync(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            MultiAddress address,
            X509Certificate2 certificate)
        {
            var supervisorLoopGroup = ((ITcpServerEventLoopGroupFactory) handlerEventLoopGroupFactory)
               .GetOrCreateSupervisorEventLoopGroup();
            var channelHandler = new ServerChannelInitializerBase<IChannel>(HandlerGenerationFunction, handlerEventLoopGroupFactory, certificate);

            return await new ServerBootstrap()
               .Group(handlerEventLoopGroupFactory.GetOrCreateSocketIoEventLoopGroup(), supervisorLoopGroup)
               .ChannelFactory(() => new TcpServerSocketChannel())
               .Option(ChannelOption.SoBacklog, _backLogValue)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .ChildHandler(channelHandler)
               .BindAsync(address.GetIpAddress(), address.GetPort())
               .ConfigureAwait(false);
        }
    }
}
