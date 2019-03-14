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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog.Extensions.Logging;
using ILogger = Serilog.ILogger;
using LogLevel = DotNetty.Handlers.Logging.LogLevel;
using Catalyst.Node.Common.Helpers.IO.Inbound;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public class P2PMessaging : IP2PMessaging, IDisposable
    {
        private readonly IPeerSettings _settings;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly X509Certificate2 _certificate;
        private IChannel _clientChannel;
        private MultithreadEventLoopGroup _clientEventLoopGroup;
        private IChannel _serverChannel;
        private MultithreadEventLoopGroup _serverParentGroup;
        private MultithreadEventLoopGroup _serverWorkerGroup;

        public IPeerIdentifier Identifier { get; }

        static P2PMessaging()
        {
            //Find a better way to do this at some point
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider());
        }

        public P2PMessaging(IPeerSettings settings, 
            ICertificateStore certificateStore,
            ILogger logger)
        {
            _settings = settings;
            _logger = logger;
            _certificate = certificateStore.ReadOrCreateCertificateFile(settings.PfxFileName);
            _cancellationSource = new CancellationTokenSource();

            Identifier = new PeerIdentifier(settings);

            var longRunningTasks = new [] {RunP2PServerAsync(), RunP2PClientAsync()};
            Task.WaitAll(longRunningTasks);
        }

        private async Task RunP2PServerAsync()
        {
            _logger.Information("P2P server starting");

            var encoder = new StringEncoder(Encoding.UTF8);
            var decoder = new StringDecoder(Encoding.UTF8);
            var serverHandler = new SecureTcpMessageServerHandler();
            _serverParentGroup = new MultithreadEventLoopGroup(1);
            _serverWorkerGroup = new MultithreadEventLoopGroup();

            _serverChannel = await new TcpServer(
                    _settings.Port,
                    _settings.BindAddress,
                    _serverParentGroup,
                    _serverWorkerGroup
            ).StartServer(
                _certificate,
                new InboundChannelInitializer<ISocketChannel>(channel => {},
                    encoder,
                    decoder,
                    serverHandler,
                    _certificate
                )
            );
        }

        private async Task RunP2PClientAsync()
        {
            _logger.Information("P2P client starting");
            _clientEventLoopGroup = new MultithreadEventLoopGroup();
 
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
                                new ClientTlsSettings(_settings.EndPoint.ToString())));
                    }
                    pipeline.AddLast(new LoggingHandler(LogLevel.DEBUG));
                    pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                    pipeline.AddLast(new StringEncoder(), new StringDecoder(), new SecureTcpMessageClientHandler());
                }));

            _clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(_settings.BindAddress, _settings.Port));
        }

        public void Stop()
        {
            _cancellationSource.Cancel();
        }

        public async Task<bool> PingAsync(IPeerIdentifier targetNode)
        {
            return await Task.FromResult(true);
        }

        public async Task BroadcastMessageAsync(string message)
        {
            await _clientChannel.WriteAndFlushAsync(message + Environment.NewLine);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Information("P2P Messaging service is closing");
                _cancellationSource?.Dispose();
                try
                {
                    _serverChannel.CloseAsync();
                    _clientChannel.CloseAsync();
                }
                finally
                {
                    _clientEventLoopGroup.ShutdownGracefullyAsync().Wait(1000);
                    Task.WaitAll(_serverParentGroup.ShutdownGracefullyAsync(), 
                        _serverWorkerGroup.ShutdownGracefullyAsync());
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
