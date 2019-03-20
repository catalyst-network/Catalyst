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
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using DotNetty.Codecs;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.P2P.Messaging;
using DotNetty.Codecs.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Serilog;

namespace Catalyst.Node.Core.RPC
{
    public class RpcServer : IRpcServer, IDisposable
    {
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly X509Certificate2 _certificate;
        private readonly IRpcServerSettings _settings;
        private ISocketServer _rpcSocketServer;
        public IRpcServerSettings Settings { get; set; }

        public RpcServer(IRpcServerSettings settings, ILogger logger, ICertificateStore certificateStore)
        {
            _logger = logger;
            _settings = settings;
            _cancellationSource = new CancellationTokenSource();
            _certificate = certificateStore.ReadOrCreateCertificateFile(settings.PfxFileName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task RunServerAsync()
        {
            _logger.Information("Rpc Server Starting!");

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
                _rpcSocketServer = await new TcpServer(_logger)
                   .Bootstrap(new InboundChannelInitializer<ISocketChannel>(
                        channel => { },
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rpcSocketServer?.Shutdown();
                _cancellationSource?.Dispose();
                _certificate?.Dispose(); 
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}