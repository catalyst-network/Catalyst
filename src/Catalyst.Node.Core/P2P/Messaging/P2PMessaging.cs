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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Channels.Sockets;
using Serilog.Extensions.Logging;
using ILogger = Serilog.ILogger;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.IO.Outbound;
using Catalyst.Node.Common.P2P;
using Catalyst.Protocol.Common;
using Dawn;
using DotNetty.Buffers;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Node.Core.P2P.Messaging
{
    public class P2PMessaging : IP2PMessaging, IDisposable
    {
        private IUdpServer _udpServer;
        private readonly ILogger _logger;
        private readonly IPeerSettings _settings;
        private readonly X509Certificate2 _certificate;
        private readonly AnyTypeClientHandler _anyTypeClientHandler;
        private readonly AnyTypeServerHandler _anyTypeServerHandler;
        private readonly CancellationTokenSource _cancellationSource;
        public IPeerIdentifier Identifier { get; }
        public IDictionary<int, ISocketClient> OpenedClients { get; }
        public IObservable<IChanneledMessage<AnySigned>> InboundMessageStream { get; }
        public IObservable<IChanneledMessage<AnySigned>> OutboundMessageStream { get; }

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
            _anyTypeServerHandler = new AnyTypeServerHandler();
            InboundMessageStream = _anyTypeServerHandler.MessageStream;
            OpenedClients = new ConcurrentDictionary<int, ISocketClient>();
            var longRunningTasks = new[]
            {
                NodeListenerAsync()
            };
            Task.WaitAll(longRunningTasks);
        }

        private async Task NodeListenerAsync()
        {
            _logger.Debug("P2P server starting");

            var handlers = new List<IChannelHandler>
            {
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(AnySigned.Parser),
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                _anyTypeServerHandler
            };

            try
            {
                _udpServer = await new UdpServer(_logger)
                   .Bootstrap(new InboundChannelInitializer<IChannel>(channel => { },
                        handlers)
                    ).StartServer(_settings.BindAddress, _settings.Port);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                Dispose();
                throw;
            }
        }

        public async Task<int> PeerConnectAsync(IPEndPoint peerEndPoint)
        {
            _logger.Debug("P2P client starting");

            var handlers = new List<IChannelHandler>
            {
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(AnySigned.Parser),
                new AnyTypeClientHandler()
            };
            
            var peerSocket = await new UdpClient()
               .Bootstrap(new OutboundChannelInitializer<IChannel>(channel => { },
                    handlers,
                    peerEndPoint.Address
                )).ConnectClient(
                    peerEndPoint.Address,
                    peerEndPoint.Port
                );

            // var peerSocketHashCode = peerSocket.Channel.RemoteAddress.GetHashCode();

            OpenedClients.TryAdd(peerEndPoint.GetHashCode(), peerSocket);
            return peerEndPoint.GetHashCode();
        }

        public async Task BroadcastMessageAsync(int peerSocketClientId, IByteBufferHolder datagramPacket)
        {
            OpenedClients.TryGetValue(peerSocketClientId, out ISocketClient socketClient);
            Guard.Argument(socketClient).NotNull();
            
            try
            {
                await socketClient.Channel.WriteAndFlushAsync(datagramPacket).ConfigureAwait(false);
            }
            finally
            {
                await socketClient.Channel.CloseAsync().ConfigureAwait(false);
                OpenedClients.Remove(peerSocketClientId);
            }      
        }

        public async Task SendMessageToPeers(IEnumerable<IPeerIdentifier> peers, IChanneledMessage<AnySigned> message)
        {
            await message.Context.WriteAndFlushAsync(message.Payload);
        }
        
        public void Stop()
        {
            _cancellationSource.Cancel();
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _udpServer?.Shutdown();
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
