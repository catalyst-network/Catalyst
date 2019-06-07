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
using System.Net;
using Catalyst.Common.Interfaces.IO.Inbound;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;
using Serilog;

namespace Catalyst.Common.IO.Inbound
{
    public class TcpServerChannelFactory : ITcpServerChannelFactory
    {
        public IServerChannel BuildChannel() => new TcpServerSocketChannel();
    }

    public class TcpServer : SocketBase<IServerChannel>, ITcpServer
    {
        private const int BackLogValue = 100;

        private readonly IEventLoopGroup _supervisorEventLoop;

        protected TcpServer(ITcpServerChannelFactory tcpChannelFactory, ILogger logger)
            : base(tcpChannelFactory, logger)
        {
            _supervisorEventLoop = new MultithreadEventLoopGroup();
        }

        public void Bootstrap(IChannelHandler channelHandler, IPAddress listenAddress, int port)
        {
            Channel = new ServerBootstrap()
               .Group(_supervisorEventLoop, childGroup: WorkerEventLoop)
               .ChannelFactory(ChannelFactory.BuildChannel)
               .Option(ChannelOption.SoBacklog, BackLogValue)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .ChildHandler(channelHandler)
               .BindAsync(listenAddress, port)
               .GetAwaiter()
               .GetResult();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(true);
            if (!disposing)
            {
                return;
            }

            if (_supervisorEventLoop == null)
            {
                return;
            }

            var quietPeriod = TimeSpan.FromMilliseconds(100);
            _supervisorEventLoop
               .ShutdownGracefullyAsync(quietPeriod, 2 * quietPeriod);
        }
    }
}
