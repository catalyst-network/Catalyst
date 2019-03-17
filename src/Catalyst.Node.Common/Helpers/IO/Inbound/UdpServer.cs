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
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public class UdpServer : AbstractServer, ISocketServer, IDisposable
    {

        private readonly ILogger _logger;
        private readonly IEventLoopGroup _workerEventLoop;
        
        public new IChannel Channel { get; set; }
        public new IBootstrap Server { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public UdpServer(ILogger logger)
        {
            _logger = logger;
            _workerEventLoop = new MultithreadEventLoopGroup();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelInitializer"></param>
        /// <returns></returns>
        public override ISocketServer Bootstrap(IChannelHandler channelInitializer)
        {
            Server = (IBootstrap) new ServerBootstrpClient()
               .Group(_workerEventLoop)
               .Channel<SocketDatagramChannel>()
               .Option(ChannelOption.SoBroadcast, true)
               .Handler(new LoggingHandler(LogLevel.INFO))
               .Handler(channelInitializer);
            return this;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="listenAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
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
            if (_workerEventLoop != null)
            {
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
                _logger.Information("Disposing UDP Server");
                Task.WaitAll(ShutdownServer());
            }
        }
    }
}
