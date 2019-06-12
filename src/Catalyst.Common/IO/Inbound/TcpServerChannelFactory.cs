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

using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Duplex;
using Catalyst.Common.IO.Inbound.Handlers;
using Catalyst.Common.IO.Outbound.Handlers;
using Catalyst.Protocol.Common;
using DotNetty.Codecs.Protobuf;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Catalyst.Common.IO.Inbound
{
    public class TcpServerChannelFactory : ITcpServerChannelFactory
    {
        private readonly IMessageCorrelationManager _correlationManger;
        private readonly IPeerSettings _peerSettings;
        private readonly IKeySigner _keySigner;
        private readonly ObservableServiceHandler _observableServiceHandler;
        private const int BackLogValue = 100;

        public TcpServerChannelFactory(IMessageCorrelationManager correlationManger,
            IPeerSettings peerSettings,
            IKeySigner keySigner)
        {
            _correlationManger = correlationManger;
            _peerSettings = peerSettings;
            _keySigner = keySigner;
            _observableServiceHandler = new ObservableServiceHandler();
        }

        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Local TLS certificate</param>
        public IObservableSocket BuildChannel(IPAddress targetAddress = null,
            int targetPort = 0, 
            X509Certificate2 certificate = null) => 
            Bootstrap(certificate);

        private IObservableSocket Bootstrap(X509Certificate2 certificate)
        {
            var supervisorEventLoop = new MultithreadEventLoopGroup();

            var channelHandler = new InboundChannelInitializerBase<IChannel>(Handlers, certificate);

            var channel = new ServerBootstrap()
               .Group(supervisorEventLoop, childGroup: new MultithreadEventLoopGroup())
               .ChannelFactory(() => new TcpServerSocketChannel())
               .Option(ChannelOption.SoBacklog, BackLogValue)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .ChildHandler(channelHandler)
               .BindAsync(_peerSettings.BindAddress, _peerSettings.Port)
               .GetAwaiter()
               .GetResult();

            return new ObservableSocket(_observableServiceHandler.MessageStream, channel);
        }

        private List<IChannelHandler> _handlers;

        protected List<IChannelHandler> Handlers =>
            _handlers ?? (_handlers = new List<IChannelHandler>
            {
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(ProtocolMessage.Parser),
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new MessageSignerDuplex(new ProtocolMessageVerifyHandler(_keySigner), new ProtocolMessageSignHandler(_keySigner)),
                new CorrelationHandler(_correlationManger),
                _observableServiceHandler
            });
    }
}
