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
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Outbound;
using Catalyst.Protocol.Common;
using DotNetty.Codecs.Protobuf;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace Catalyst.Common.IO.Outbound
{
    public class TcpClientChannelFactory : ITcpClientChannelFactory
    {
        private const int BackLogValue = 100;
        
        public IObservableSocket BuildChannel(IPAddress targetAddress = null, 
            int targetPort = 0,
            X509Certificate2 certificate = null)
        {
            var channelHandlers = new List<IChannelHandler>
            {
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(ProtocolMessage.Parser)
            };

            var channelHandler = new OutboundChannelInitializerBase<ISocketChannel>(channelHandlers,
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

            return new ObservableSocket(
                Observable.Empty<IChanneledMessage<ProtocolMessage>>(), 
                channel);
        }
    }

    public class TcpClient : ClientBase, ITcpClient
    {
        protected TcpClient(ITcpClientChannelFactory channelFactory, ILogger logger) 
            : base(channelFactory, logger) { }
    }
}
