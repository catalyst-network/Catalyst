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
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Node.Common.Interfaces.Modules.KeySigner;
using Catalyst.Node.Common.Interfaces.Modules.Mempool;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.Interfaces.Rpc;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Core.RPC.Handlers;
using DotNetty.Codecs.Protobuf;
using Serilog;

namespace Catalyst.Node.Core.RPC
{
    public class NodeRpcServer : TcpServer, INodeRpcServer
    {
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationSource;
        private readonly X509Certificate2 _certificate;
        private readonly GetInfoRequestHandler _infoRequestHandler;
        private readonly GetVersionRequestHandler _versionRequestHandler;
        private readonly GetMempoolRequestHandler _mempoolRequestHandler;
        private readonly SignMessageRequestHandler _signMessageRequestHandler;
        private readonly VerifyMessageRequestHandler _verifyMessageRequestHandler;

        public IRpcServerSettings Settings { get; }
        public IObservable<IChanneledMessage<AnySigned>> MessageStream { get; }

        public NodeRpcServer(IRpcServerSettings settings,
            ILogger logger,
            ICertificateStore certificateStore,
            IMempool mempool,
            IKeySigner keySigner,
            IPeerSettings peerSettings) : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            _logger = logger;
            Settings = settings;
            _cancellationSource = new CancellationTokenSource();
            _certificate = certificateStore.ReadOrCreateCertificateFile(settings.PfxFileName);

            var anyTypeSignedServerHandler = new AnyTypeSignedServerHandler();
            MessageStream = anyTypeSignedServerHandler.MessageStream;

            //todo: these handlers need to be instantiated by Autofac to avoid bringing dependencies
            //all over the RpcServer constructor, and get a proper context for the loggers
            //https://github.com/catalyst-network/Catalyst.Node/issues/309
            IPeerIdentifier peerIdentifier = new PeerIdentifier(peerSettings);
            _infoRequestHandler = new GetInfoRequestHandler(MessageStream, peerIdentifier, Settings, logger);
            _versionRequestHandler = new GetVersionRequestHandler(MessageStream, peerIdentifier, logger);
            _mempoolRequestHandler = new GetMempoolRequestHandler(MessageStream, peerIdentifier, logger, mempool);
            _signMessageRequestHandler = new SignMessageRequestHandler(MessageStream, peerIdentifier, logger, keySigner);
            _verifyMessageRequestHandler = new VerifyMessageRequestHandler(MessageStream, peerIdentifier, logger, keySigner);
            
            Bootstrap(
                new InboundChannelInitializer<ISocketChannel>(channel => { },
                    new List<IChannelHandler>
                    {
                        new ProtobufVarint32FrameDecoder(),
                        new ProtobufDecoder(AnySigned.Parser),
                        new ProtobufVarint32LengthFieldPrepender(),
                        new ProtobufEncoder(),
                        anyTypeSignedServerHandler
                    },
                    _certificate
                ),
                Settings.BindAddress, Settings.Port
            );
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;
            _cancellationSource?.Dispose();
            _certificate?.Dispose();
            _infoRequestHandler?.Dispose();
            _versionRequestHandler.Dispose();
            _mempoolRequestHandler.Dispose();
            _signMessageRequestHandler.Dispose();
            _verifyMessageRequestHandler.Dispose();
        }
    }
}
