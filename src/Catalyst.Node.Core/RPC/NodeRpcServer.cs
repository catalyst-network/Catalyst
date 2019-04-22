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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Catalyst.Common.IO.Inbound;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Protocol.Common;
using Catalyst.Common.Interfaces.Rpc;
using DotNetty.Codecs.Protobuf;
using Serilog;

namespace Catalyst.Node.Core.RPC
{
    public class NodeRpcServer
        : TcpServer,
            INodeRpcServer
    {
        private readonly CancellationTokenSource _cancellationSource;
        private readonly X509Certificate2 _certificate;

        public IRpcServerSettings Settings { get; }
        public IObservable<IChanneledMessage<AnySigned>> MessageStream { get; }

        public NodeRpcServer(IRpcServerSettings settings,
            ILogger logger,
            ICertificateStore certificateStore,
            IEnumerable<IRpcRequestHandler> requestHandlers) : base(logger)
        {
            Settings = settings;
            _cancellationSource = new CancellationTokenSource();
            _certificate = certificateStore.ReadOrCreateCertificateFile(settings.PfxFileName);

            var anyTypeServerHandler = new AnyTypeSignedServerHandlerBase();
            MessageStream = anyTypeServerHandler.MessageStream;
            requestHandlers.ToList().ForEach(h => h.StartObserving(MessageStream));
           
            Bootstrap(
                new InboundChannelInitializerBase<ISocketChannel>(channel => { },
                    new List<IChannelHandler>
                    {
                        new ProtobufVarint32FrameDecoder(),
                        new ProtobufDecoder(AnySigned.Parser),
                        new ProtobufVarint32LengthFieldPrepender(),
                        new ProtobufEncoder(),
                        anyTypeServerHandler
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
        }
    }
}
