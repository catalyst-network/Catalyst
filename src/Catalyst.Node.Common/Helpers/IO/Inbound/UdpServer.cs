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
using System.Net.Sockets;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public interface IUdpServer
    {
        IBootstrap UdpListener { get; set; }
        IChannel Channel { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="channelInitializer"></param>
        /// <returns></returns>
        IUdpServer Bootstrap(IChannelHandler channelInitializer);

        Task<IUdpServer> StartServer(IPAddress listenAddress, int port);
        Task Shutdown();
    }

    public abstract class UdpServer : AbstractIo, IUdpServer
    {
        public IBootstrap UdpListener { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        public UdpServer(ILogger logger) : base(logger) { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="channelInitializer"></param>
        /// <returns></returns>
        public IUdpServer Bootstrap(IChannelHandler channelInitializer)
        {
            UdpListener = new Bootstrap();
            ((DotNetty.Transport.Bootstrapping.Bootstrap) UdpListener)
               .Group(WorkerEventLoop)
               .ChannelFactory(() => new SocketDatagramChannel(AddressFamily.InterNetwork))
               .Option(ChannelOption.SoBroadcast, true)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .Handler(channelInitializer);
            return this;
        }

        public async Task<IUdpServer> StartServer(IPAddress listenAddress, int port)
        {
            Channel = await UdpListener.BindAsync(listenAddress, port).ConfigureAwait(false);
            return this;
        }
    }
}
