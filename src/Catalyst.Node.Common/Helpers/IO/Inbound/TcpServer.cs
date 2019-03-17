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

using System;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public sealed class TcpServer : AbstractServer, IDisposable, ISocketServer
    {
        private const int BackLogValue = 100;

        private readonly ILogger _logger;
        private readonly IEventLoopGroup _workerEventLoop;
        private readonly IEventLoopGroup _supervisorEventLoop;
               
        /// <summary>
        ///     
        /// </summary>
        /// <param name="logger"></param>
        public TcpServer(ILogger logger)
        {
            _logger = logger;
            _supervisorEventLoop = new MultithreadEventLoopGroup();
            _workerEventLoop = new MultithreadEventLoopGroup();
        }

        public override ISocketServer Bootstrap(IChannelHandler channelInitializer)
        {
            
            Server = (IServerBootstrp) new ServerBootstrpServer()
               .Group(_supervisorEventLoop, _workerEventLoop)
               .Channel<TcpServerSocketChannel>()
               .Option(ChannelOption.SoBacklog, BackLogValue)
               .Handler(new LoggingHandler(LogLevel.INFO))
               .ChildHandler(channelInitializer);
            return this;
        }

        public async Task<ISocketServer> StartServer(IPAddress listenAddress, int port)
        {
            Channel = await Server.BindAsync(listenAddress, port).ConfigureAwait(false);
            return this;
        }
        
        public override async Task ShutdownServer()
        {
            if (Channel != null)
            {
                await Channel.CloseAsync().ConfigureAwait(false);
            }
            if (_supervisorEventLoop != null && _workerEventLoop != null)
            {
                await _supervisorEventLoop.ShutdownGracefullyAsync().ConfigureAwait(false);
                await _workerEventLoop.ShutdownGracefullyAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
               _logger.Information("Disposing TCP Server");
               Task.WaitAll(ShutdownServer());
            }
        }
    }
}
