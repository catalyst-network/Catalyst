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
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Channels.Sockets;
using Serilog.Extensions.Logging;
using ILogger = Serilog.ILogger;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.IO.Outbound;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Common.Logging;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public class P2PMessaging : IP2PMessaging, IDisposable, IMessageStreamer<Any>
    {
        private readonly IPeerSettings _settings;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly X509Certificate2 _certificate;
        private readonly AnyTypeClientHandler _anyTypeClientHandler;
        private ISocketClient _socketClient;
        private ISocketServer _socketServer;

        public IPeerIdentifier Identifier { get; }
        public IObservable<Any> MessageStream { get; }

        static P2PMessaging()
        {
            //Find a better way to do this at some point
            DotNetty.Common.Internal.Logging.InternalLoggerFactory.DefaultFactory.AddProvider(new SerilogLoggerProvider());
        }

        public P2PMessaging(
            IPeerSettings settings, 
            ICertificateStore certificateStore,
            ILogger logger
        )
        {
            _settings = settings;
            _logger = logger;
            _certificate = certificateStore.ReadOrCreateCertificateFile(settings.PfxFileName);
            _cancellationSource = new CancellationTokenSource();

            Identifier = new PeerIdentifier(settings);
            _anyTypeClientHandler = new AnyTypeClientHandler();
            MessageStream = _anyTypeClientHandler.MessageStream;

            var longRunningTasks = new [] {RunP2PServerAsync(), RunP2PClientAsync()};
            Task.WaitAll(longRunningTasks);
        }

        private async Task RunP2PServerAsync()
        {
            _logger.Debug("P2P server starting");

            var handlers = new List<IChannelHandler>
            {
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(Any.Parser),
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new AnyTypeServerHandler()
            };

            try
            {
                _socketServer = await new TcpServer(_logger)
                   .Bootstrap(new InboundChannelInitializer<ISocketChannel>(channel => { },
                            handlers,
                            certificate: _certificate)
                ).StartServer(_settings.BindAddress, _settings.Port);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                Dispose();
            }
        }

        private async Task RunP2PClientAsync()
        {
            _logger.Debug("P2P client starting");

            var handlers = new List<IChannelHandler>
            {
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(Any.Parser),
                _anyTypeClientHandler
            };
            
            _socketClient = await new TcpClient(_logger)
               .Bootstrap(
                    new OutboundChannelInitializer<ISocketChannel>(channel => {},
                        handlers,
                        _settings.BindAddress,
                        _certificate
                    )
               )
               .ConnectClient(
                    _settings.BindAddress,
                    _settings.Port
               );
        }

        public void Stop()
        {
            _cancellationSource.Cancel();
        }

        public async Task<bool> PingAsync(IPeerIdentifier targetNode)
        {
            return await Task.FromResult(true);
        }

        public async Task BroadcastMessageAsync(Any msg)
        {
            await _socketClient.Channel.WriteAndFlushAsync(msg);
        }

        public async Task SendMessageToPeers(IEnumerable<IPeerIdentifier> peers, Any message)
        {

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _socketServer?.Shutdown();
            _cancellationSource?.Dispose();
            _certificate?.Dispose();
            _anyTypeClientHandler?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
