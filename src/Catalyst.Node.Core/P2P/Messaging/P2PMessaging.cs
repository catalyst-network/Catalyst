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
using Serilog;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public class P2PMessaging : IP2PMessaging<MultithreadEventLoopGroup>, IDisposable
    {
        private readonly IPeerSettings _settings;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationSource;
        private X509Certificate2 _certificate;

        public P2PMessaging(IPeerSettings settings, 
            ICertificateStore certificateStore,
            ILogger logger)
        {
            _settings = settings;
            _logger = logger;
            _certificate = certificateStore.ReadOrCreateCertificateFile(settings.PfxFileName);
            _cancellationSource = new CancellationTokenSource();

            RunP2PServerAsync();
            RunP2PClientAsync();
        }

        private async Task RunP2PServerAsync()
        {
            var encoder = new StringEncoder(Encoding.UTF8);
            var decoder = new StringDecoder(Encoding.UTF8);
            var serverHandler = new SecureTcpMessageServerHandler();
            var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup();

            try
            {


                var bootstrap = new ServerBootstrap();
                bootstrap
                   .Group(bossGroup, workerGroup)
                   .Channel<TcpServerSocketChannel>()
                   .Option(ChannelOption.SoBacklog, 100)
                   .Handler(new LoggingHandler(LogLevel.INFO))
                   .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (_certificate != null)
                        {
                            pipeline.AddLast(TlsHandler.Server(_certificate));
                        }

                        pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                        pipeline.AddLast(encoder, decoder, serverHandler);
                    }));

                var bootstrapChannel = await bootstrap.BindAsync(_settings.Port);

                while (!_cancellationSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(5000, _cancellationSource.Token);
                    Console.WriteLine(@"P2P server is alive.");
                }

                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
                _cancellationSource.Cancel();
            }
        }

        private async Task RunP2PClientAsync()
        {
            var group = new MultithreadEventLoopGroup();
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                   .Group(group)
                   .Channel<TcpSocketChannel>()
                   .Option(ChannelOption.TcpNodelay, true)
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

                        pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                        pipeline.AddLast(new StringEncoder(), new StringDecoder(), new SecureTcpMessageClientHandler());
                    }));

                var bootstrapChannel = await bootstrap.ConnectAsync(new IPEndPoint(_settings.BindAddress, _settings.Port));

                var line = Console.ReadLine();
                for (; ; )
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    try
                    {
                        await bootstrapChannel.WriteAndFlushAsync(line + "\r\n");
                    }
                    catch
                    {
                    }

                    if (!string.Equals(line, "bye", StringComparison.OrdinalIgnoreCase)) continue;

                    await bootstrapChannel.CloseAsync();
                    break;
                }

                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                group.ShutdownGracefullyAsync().Wait(1000);
                _cancellationSource.Cancel();
            }
        }

        public async Task Stop()
        {
            _cancellationSource.Cancel();
        }

        public bool Ping(IPeerIdentifier targetNode) { throw new System.NotImplementedException(); }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationSource?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
