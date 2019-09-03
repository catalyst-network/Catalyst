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
using System.Net.Sockets;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Core.IO.Transport.Bootstrapping;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Catalyst.Core.IO.Transport.Channels
{
    public abstract class UdpChannelFactoryBase
    {
        protected abstract Func<List<IChannelHandler>> HandlerGenerationFunction { get; }

        protected async Task<IChannel> BootStrapChannel(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            IPAddress address,
            int port)
        {
            var channelHandler = new ServerChannelInitializerBase<IChannel>(HandlerGenerationFunction, handlerEventLoopGroupFactory);

            return await new Bootstrap()
               .Group(handlerEventLoopGroupFactory.GetOrCreateSocketIoEventLoopGroup())
               .ChannelFactory(() => new SocketDatagramChannel(AddressFamily.InterNetwork))
               .Option(ChannelOption.SoBroadcast, true)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .Handler(channelHandler)
               .BindAsync(address, port)
               .ConfigureAwait(false);
        }
    }
}
