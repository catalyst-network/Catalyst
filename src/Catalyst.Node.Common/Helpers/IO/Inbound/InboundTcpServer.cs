/*
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

using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Libuv;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public class InboundTcpServer : IInboundTcpServer
    {
        private const int SoLinger = 0;
        private const int SoBacklog = 100;

        private bool _disposed;
        private IChannel _channel;
        private readonly int _port;
        private readonly IPAddress _listenAddress;
        private readonly ServerBootstrap _bootstrap;
        private readonly IInboundSession _inboundSession;
        private readonly IEventLoopGroup _workerEventLoopGroup;
        private readonly IEventLoopGroup _supervisorEventLoopGroup;
        private readonly ISessionHandler _sessionHandler;
        
        /// <summary>
        ///     Creates a DotNetty tcp server.
        /// </summary>
        /// <returns></returns>
        public InboundTcpServer(IPAddress listenAddress, int port, IInboundSession inboundSession, ISessionHandler sessionHandler)
        {
            _port = port;
            _listenAddress = listenAddress;
            _inboundSession = inboundSession;
            _sessionHandler = sessionHandler;
            _bootstrap = new ServerBootstrap();
            var dispatcher = new DispatcherEventLoopGroup();
            _supervisorEventLoopGroup = dispatcher;
            _workerEventLoopGroup = new WorkerEventLoopGroup(dispatcher);
        }

        /// <summary>
        ///     Call this in a try statement with a finally catch calling StopAsync/Dispose
        /// </summary>
        /// <param name="x509Certificate"></param>
        /// <returns></returns>
        public async Task StartAsync(X509Certificate x509Certificate)
        {
            Init();
            _bootstrap.ChildHandler(new InboundSessionInitializer(_inboundSession, _sessionHandler, x509Certificate));
            _channel = await _bootstrap.BindAsync(_listenAddress, _port);   
        }

        /// <summary>
        ///     Call this in a try statement with a finally catch calling StopAsync/Dispose
        /// </summary>
        /// <returns></returns>
        public async Task StartAsync()
        {
            Init();
            _bootstrap.ChildHandler(new InboundSessionInitializer(_inboundSession, _sessionHandler));
            _channel = await _bootstrap.BindAsync(_listenAddress, _port).ConfigureAwait(false);
        }

        /// <summary>
        ///     Most channel options derive from standard unix socket flags.
        /// 
        ///     SoReuseaddr: https://stackoverflow.com/questions/3229860/what-is-the-meaning-of-so-reuseaddr-setsockopt-option-linux
        ///     SoKeepalive: https://en.wikipedia.org/wiki/Keepalive
        ///     TcpNodelay: https://www.extrahop.com/company/blog/2016/tcp-nodelay-nagle-quickack-best-practices
        ///     SoLinger: https://stackoverflow.com/questions/3757289/tcp-option-so-linger-zero-when-its-required
        ///     SoBacklog: https://stackoverflow.com/questions/36594400/what-is-backlog-in-tcp-connections
        /// </summary>
        private void Init()
        {
            _bootstrap.Group(_supervisorEventLoopGroup, _workerEventLoopGroup);
            _bootstrap.Option(ChannelOption.SoReuseaddr, false);
            _bootstrap.Option(ChannelOption.SoKeepalive, true);
            _bootstrap.Option(ChannelOption.TcpNodelay, true);
            _bootstrap.Option(ChannelOption.SoLinger, SoLinger);
            _bootstrap.Option(ChannelOption.AutoRead, false);
            _bootstrap.Option(ChannelOption.SoBacklog, SoBacklog);
            _bootstrap.Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default);
            _bootstrap.ChannelFactory(() => new TcpServerSocketChannel(AddressFamily.InterNetwork));
            _bootstrap.Handler(new LoggingHandler("SRV-LSTN"));
        }
        
        public async Task StopAsync()
        {
            if (_channel != null)
            {
                await _channel.CloseAsync().ConfigureAwait(false);
            }
            if (_supervisorEventLoopGroup != null && _workerEventLoopGroup != null)
            {
                await _supervisorEventLoopGroup.ShutdownGracefullyAsync().ConfigureAwait(false);
                await _workerEventLoopGroup.ShutdownGracefullyAsync().ConfigureAwait(false);
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                StopAsync().ConfigureAwait(false);
                _disposed = true;
            }
        }
    }
}
