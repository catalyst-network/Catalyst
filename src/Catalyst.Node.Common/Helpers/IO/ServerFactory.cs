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
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;

namespace Catalyst.Node.Common.Helpers.IO
{
    public class ServerFactory
    {
        private const int SoLinger = 0;
        private const int SoBacklog = 100;
        
        private readonly IEventLoopGroup _workerGroup;
        private readonly IEventLoopGroup _supervisorGroup;
        private readonly X509Certificate _x509Certificate;

        public ServerFactory(IEventLoopGroup supervisorGroup)
        {
            _supervisorGroup = supervisorGroup;
        }
        
        public ServerFactory(IEventLoopGroup supervisorGroup, X509Certificate x509Certificate)
        {
            _supervisorGroup = supervisorGroup;
            _x509Certificate = x509Certificate;
        }
                
        public ServerFactory(IEventLoopGroup supervisorGroup, IEventLoopGroup workerGroup)
        {
            _supervisorGroup = supervisorGroup;
            _workerGroup = workerGroup;
        }
        
        public ServerFactory(IEventLoopGroup supervisorGroup, IEventLoopGroup workerGroup, X509Certificate x509Certificate)
        {
            _supervisorGroup = supervisorGroup;
            _workerGroup = workerGroup;
            _x509Certificate = x509Certificate;
        }

        /// <summary>
        ///     Factory to create a either a new tcp or udp DotNetty server.
        /// </summary>
        /// <param name="socketType"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public ServerBootstrap Create(SocketType socketType, int port)
        {
            switch (socketType)
            {
                case SocketType.Stream:
                    return CreateTcpServer(port);
                case SocketType.Dgram:
                    return CreateUdpServer(port);
                default:
                    throw new ArgumentException();
            }
        }

        private ServerBootstrap CreateUdpServer(int port)
        {
            var bootstrap = new Bootstrap();
            bootstrap
               .Group(_supervisorGroup)
               .Channel<SocketDatagramChannel>()
               .Option(ChannelOption.SoBroadcast, true)
               .Handler(new LoggingHandler("SRV-LSTN"))
               .Handler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    channel.Pipeline.AddLast("Quote", new QuoteOfTheMomentServerHandler());
                }));
        }
        
        private ServerBootstrap CreateTcpServer(int port)
        {        
            var server = new ServerBootstrap()
               .Group(_supervisorGroup, _workerGroup)
               .Option(ChannelOption.SoReuseaddr, false) // https: //stackoverflow.com/questions/3229860/what-is-the-meaning-of-so-reuseaddr-setsockopt-option-linux
               .Option(ChannelOption.SoKeepalive, true) // https://en.wikipedia.org/wiki/Keepalive
               .Option(ChannelOption.TcpNodelay, true) // https://www.extrahop.com/company/blog/2016/tcp-nodelay-nagle-quickack-best-practices/
               .Option(ChannelOption.SoLinger, SoLinger) // https://stackoverflow.com/questions/3757289/tcp-option-so-linger-zero-when-its-required
               .Option(ChannelOption.AutoRead, false)
               .Option(ChannelOption.SoBacklog, SoBacklog) // https://stackoverflow.com/questions/36594400/what-is-backlog-in-tcp-connections
               .Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
               .ChannelFactory(() => new TcpServerSocketChannel(AddressFamily.InterNetwork))
               .Handler(new LoggingHandler("SRV-LSTN"))
               .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    
                    if (_x509Certificate != null)
                    {
                        pipeline.AddLast("tls", TlsHandler.Server(_x509Certificate));                        
                    }
                    
                    pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                    pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                    pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                    pipeline.AddLast("echo", new EchoServerHandler());
                }));
                
            return server;
        }
    }
}