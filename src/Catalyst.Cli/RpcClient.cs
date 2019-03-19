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
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Shell;
using Catalyst.Node.Common.Interfaces;

using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

using Microsoft.Extensions.Configuration;

using Serilog;

namespace Catalyst.Cli
{
    /// <summary>
    /// This class provides a command line interface (CLI) application to connect to Catalyst Node.
    /// Through the CLI the node operator will be able to connect to any number of running nodes and run commands. 
    /// </summary>
    public class RpcClient : IRpcClient, IDisposable
    {
        
        private readonly ILogger _logger;      
        private readonly ICertificateStore _certificateStore;
        
        private MultithreadEventLoopGroup _clientEventLoopGroup;

        /// <summary>
        /// Intialize a new instance of RPClient by doing the following:
        /// 1- Get the settings from the config file
        /// 2- Create/Read the SSL Certificate
        /// 3- Start the client
        /// </summary>
        /// <param name="settings">an object of ClientSettings which reads the settings from config file section RPCClient</param>
        /// <param name="logger">a logger instance</param>
        /// <param name="certificateStore">certification store object to create/read the SSL certificate</param>
        public RpcClient(ILogger logger, ICertificateStore certificateStore)
        {
            _logger = logger;
            _certificateStore = certificateStore;
        }
        
        public async Task RunClientAsync()
        {
            _logger.Information("Rpc client starting");
            _clientEventLoopGroup = new MultithreadEventLoopGroup();
 
            // var bootstrap = new Bootstrap();
            // bootstrap
            //    .Group(_clientEventLoopGroup)
            //    .Channel<TcpSocketChannel>()
            //    .Option(ChannelOption.TcpNodelay, true)
            //    .Handler(new LoggingHandler(LogLevel.INFO))
            //    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
            //     {
            //         var pipeline = channel.Pipeline;
            //
            //         if (_certificate != null)
            //         {
            //             pipeline.AddLast(
            //                 new TlsHandler(stream => 
            //                         new SslStream(stream, true, (sender, certificate, chain, errors) => true), 
            //                     new ClientTlsSettings(_settings.EndPoint.ToString())));
            //         }
            //         pipeline.AddLast(new LoggingHandler(LogLevel.DEBUG));
            //         pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
            //         pipeline.AddLast(new StringEncoder(), new StringDecoder(), new SecureTcpMessageClientHandler());
            //     }));
            //
            // _clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(_settings.BindAddress, _settings.Port));
        }
        
        public async Task RunClientAsync(RpcNode node)
        {
            _logger.Information("Rpc client starting");
            _clientEventLoopGroup = new MultithreadEventLoopGroup();
            
            X509Certificate2 _certificate = _certificateStore.ReadOrCreateCertificateFile(node.PfxFileName);

            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                   .Group(_clientEventLoopGroup)
                   .Channel<TcpSocketChannel>()
                   .Option(ChannelOption.TcpNodelay, true)
                   .Handler(new LoggingHandler(LogLevel.INFO))
                   .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        var pipeline = channel.Pipeline;
            
                        if (_certificate != null)
                        {
                            pipeline.AddLast(
                                new TlsHandler(stream => 
                                        new SslStream(stream, true, (sender, certificate, chain, errors) => true), 
                                    new ClientTlsSettings(node.HostAddress.ToString())));
                        }
                        pipeline.AddLast(new LoggingHandler(LogLevel.DEBUG));
                        pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                        pipeline.AddLast(new StringEncoder(), new StringDecoder(), new RpClientHandler());
                    }));
            
                IChannel clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(node.HostAddress, node.Port));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
                
        }
        
        /*Implementing IDisposable */
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Information("disposing RpcClient");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}