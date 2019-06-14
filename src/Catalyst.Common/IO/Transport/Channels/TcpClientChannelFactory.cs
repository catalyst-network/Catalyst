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
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Transport.Bootstrapping;
using Catalyst.Protocol.Common;
using DotNetty.Codecs.Protobuf;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Catalyst.Common.IO.Transport.Channels
{
    public class TcpClientChannelFactory : ITcpClientChannelFactory
    {
        private readonly IKeySigner _keySigner;
        public TcpClientChannelFactory(IKeySigner keySigner) { _keySigner = keySigner; }
        
        private const int BackLogValue = 100;
        
        public IObservableChannel BuildChannel(IPAddress targetAddress = null, 
            int targetPort = IPEndPoint.MinPort,
            X509Certificate2 certificate = null)
        {
            var observableServiceHandler = new ObservableServiceHandler();

            var channelHandlers = new List<IChannelHandler>
            {
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(ProtocolMessageSigned.Parser),
                new CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>(new ProtocolMessageVerifyHandler(_keySigner), new ProtocolMessageSignHandler(_keySigner)),
                observableServiceHandler
            };

            var channelHandler = new ClientChannelInitializerBase<ISocketChannel>(channelHandlers,
                targetAddress,
                certificate);

            var channel = new Bootstrap()
               .Group(new MultithreadEventLoopGroup())
               .ChannelFactory(() => new TcpSocketChannel())
               .Option(ChannelOption.SoBacklog, BackLogValue)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .Handler(channelHandler)
               .ConnectAsync(targetAddress, targetPort)
               .GetAwaiter()
               .GetResult();

            return new ObservableChannel(
                observableServiceHandler.MessageStream, 
                channel);
        }
    }
}
