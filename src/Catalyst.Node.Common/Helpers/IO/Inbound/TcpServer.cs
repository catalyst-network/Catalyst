/*
* Copyright(c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node<https: //github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node.If not, see<https: //www.gnu.org/licenses/>.
*/

using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;
using Serilog;
using ServerBootstrap = Catalyst.Node.Common.Helpers.IO.ServerBootstrap;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    
    public sealed class TcpServer : AbstractServer
    {
        private readonly IEventLoopGroup _supervisorEventLoop;

        /// <summary>
        ///     
        /// </summary>
        /// <param name="logger"></param>
        public TcpServer(ILogger logger) : base(logger)
        {
            _supervisorEventLoop = new MultithreadEventLoopGroup(1);
        }

        public override ISocketServer Bootstrap(IChannelHandler channelInitializer)
        {
            Server = new ServerBootstrap();
            ((DotNetty.Transport.Bootstrapping.ServerBootstrap)Server)
               .Group(_supervisorEventLoop, WorkerEventLoop)
               .ChannelFactory(() => new TcpServerSocketChannel())
               .Option(ChannelOption.SoBacklog, BackLogValue)
               .Handler(new LoggingHandler(LogLevel.INFO))
               .ChildHandler(channelInitializer);
            return this;
        }
        
        public override async Task<ISocketServer> StartServer(IPAddress listenAddress, int port)
        {
            Channel = await Server.BindAsync(listenAddress, port).ConfigureAwait(false);
            return this;
        }
        
        public override async Task Shutdown()
        {
            await base.Shutdown().ConfigureAwait(false);
            
            if (_supervisorEventLoop != null)
            {
                await _supervisorEventLoop.ShutdownGracefullyAsync().ConfigureAwait(false);
            }
        }
    }
}
