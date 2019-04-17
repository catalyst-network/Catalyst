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

using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.IO;
using Catalyst.Node.Common.Interfaces.IO.Inbound;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public abstract class TcpServer : AbstractIo, ITcpServer
    {
        public IServerBootstrap Server { get; set; }
        private readonly IEventLoopGroup _supervisorEventLoop;

        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        protected TcpServer(ILogger logger) : base(logger)
        {
            _supervisorEventLoop = new MultithreadEventLoopGroup();
        }

        public void Bootstrap(IChannelHandler channelInitializer, IPAddress listenAddress, int port)
        {
            Channel = new ServerBootstrap()
               .Group(_supervisorEventLoop, childGroup: WorkerEventLoop)
               .ChannelFactory(() => new TcpServerSocketChannel())
               .Option(ChannelOption.SoBacklog, BackLogValue)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .ChildHandler(channelInitializer)
               .BindAsync(listenAddress, port)
               .GetAwaiter()
               .GetResult();
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
