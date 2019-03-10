// /*
// * Copyright(c) 2019 Catalyst Network
// *
// * This file is part of Catalyst.Node<https: //github.com/catalyst-network/Catalyst.Node>
// *
// * Catalyst.Node is free software: you can redistribute it and/or modify
// * it under the terms of the GNU General Public License as published by
// * the Free Software Foundation, either version 2 of the License, or
// * (at your option) any later version.
// *
// * Catalyst.Node is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// * GNU General Public License for more details.
// * 
// * You should have received a copy of the GNU General Public License
// * along with Catalyst.Node.If not, see<https: //www.gnu.org/licenses/>.
// */
//
// using System;
// using System.Net;
// using System.Net.Security;
// using System.Security.Cryptography.X509Certificates;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using Catalyst.Node.Common.Interfaces;
// using DotNetty.Codecs;
// using DotNetty.Handlers.Logging;
// using DotNetty.Handlers.Tls;
// using DotNetty.Transport.Bootstrapping;
// using DotNetty.Transport.Channels;
// using DotNetty.Transport.Channels.Sockets;
// using Serilog;
//
// namespace Catalyst.Node.Core.P2P.Messaging
// {
//     public class P2PMessaging : IP2PMessaging<MultithreadEventLoopGroup>, IDisposable
//     {
//         private readonly IPeerSettings _settings;
//         private readonly ILogger _logger;
//         private readonly CancellationTokenSource _cancellationSource;
//
//         public P2PMessaging(IPeerSettings settings, 
//             ICertificateStore certificateStore,
//             ILogger logger)
//         {
//             _settings = settings;
//             _logger = logger;
//             var certificate = certificateStore.ReadOrCreateCertificateFile(settings.PfxFileName);
//             _cancellationSource = new CancellationTokenSource();
//
//             RunP2PServerAsync(certificate, _cancellationSource.Token);
//             RunP2PClientAsync(certificate, _cancellationSource.Token);
//         }
//
//         private async Task RunP2PServerAsync(X509Certificate2 certificate, CancellationToken cancellationSourceToken)
//         {
//             var encoder = new StringEncoder(Encoding.UTF8);
//             var decoder = new StringDecoder(Encoding.UTF8);
//             var serverHandler = new SecureTcpMessageServerHandler();
//
//             try
//             {
//                 var bootstrap = new ServerBootstrap();
//                 bootstrap
//                    .Group(bossGroup, workerGroup)
//                    .Channel<TcpServerSocketChannel>()
//                    .Option(ChannelOption.SoBacklog, 100)
//                    .Handler(new LoggingHandler(LogLevel.INFO))
//                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
//                     {
//                         IChannelPipeline pipeline = channel.Pipeline;
//                         if (tlsCertificate != null)
//                         {
//                             pipeline.AddLast(TlsHandler.Server(tlsCertificate));
//                         }
//
//                         pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
//                         pipeline.AddLast(STRING_ENCODER, STRING_DECODER, SERVER_HANDLER);
//                     }));
//
//                 IChannel bootstrapChannel = await bootstrap.BindAsync(_settings.Port);
//
//                 Console.ReadLine();
//
//                 await bootstrapChannel.CloseAsync();
//             }
//             finally
//             {
//                 Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
//             }
//         }
//
//         private async Task RunP2PClientAsync(X509Certificate2 certificate, CancellationToken cancellationSourceToken)
//         {
//             try
//             {
//                 var bootstrap = new Bootstrap();
//                 bootstrap
//                    .Group(group)
//                    .Channel<TcpSocketChannel>()
//                    .Option(ChannelOption.TcpNodelay, true)
//                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
//                     {
//                         IChannelPipeline pipeline = channel.Pipeline;
//
//                         if (cert != null)
//                         {
//                             pipeline.AddLast(new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
//                         }
//
//                         pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
//                         pipeline.AddLast(new StringEncoder(), new StringDecoder(), new SecureTcpMessagerClientHandler());
//                     }));
//
//                 IChannel bootstrapChannel = await bootstrap.ConnectAsync(new IPEndPoint(_settings.BindAddress, _settings.Port));
//
//                 for (; ; )
//                 {
//                     string line = Console.ReadLine();
//                     if (string.IsNullOrEmpty(line))
//                     {
//                         continue;
//                     }
//
//                     try
//                     {
//                         await bootstrapChannel.WriteAndFlushAsync(line + "\r\n");
//                     }
//                     catch
//                     {
//                     }
//                     if (string.Equals(line, "bye", StringComparison.OrdinalIgnoreCase))
//                     {
//                         await bootstrapChannel.CloseAsync();
//                         break;
//                     }
//                 }
//
//                 await bootstrapChannel.CloseAsync();
//             }
//             finally
//             {
//                 group.ShutdownGracefullyAsync().Wait(1000);
//             }
//         }
//
//         public async Task Stop()
//         {
//             _cancellationSource.Cancel();
//         }
//
//         public bool Ping(IPeerIdentifier targetNode) { throw new System.NotImplementedException(); }
//
//         protected virtual void Dispose(bool disposing)
//         {
//             if (disposing)
//             {
//                 _cancellationSource?.Dispose();
//             }
//         }
//
//         public void Dispose()
//         {
//             Dispose(true);
//         }
//     }
//
//     internal class SecureTcpMessagerClientHandler : SimpleChannelInboundHandler<string>
//     {
//         protected override void ChannelRead0(IChannelHandlerContext contex, string msg) => Console.WriteLine(msg);
//
//         public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
//         {
//             Console.WriteLine(DateTime.Now.Millisecond);
//             Console.WriteLine(e.StackTrace);
//             contex.CloseAsync();
//         }
//     }
// }
