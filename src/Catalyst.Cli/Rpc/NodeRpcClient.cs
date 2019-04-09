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
using System.Threading.Tasks;
using Catalyst.Cli.Handlers;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.IO.Outbound;
using Catalyst.Node.Common.Helpers.Network;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Protocol.Common;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Catalyst.Cli.Rpc
{
    /// <summary>
    /// This class provides a command line interface (CLI) application to connect to Catalyst Node.
    /// Through the CLI the node operator will be able to connect to any number of running nodes and run commands.
    /// </summary>
    public sealed class NodeRpcClient : TcpClient<TcpSocketChannel>, INodeRpcClient, IChanneledMessageStreamer<AnySigned>
    {
        private readonly GetInfoResponseHandler _getInfoResponseHandler;
        private readonly GetVersionResponseHandler _getVersionResponseHandler;
        private readonly GetMempoolResponseHandler _getMempoolResponseHandler;

        public IObservable<IChanneledMessage<AnySigned>> MessageStream { get; }

        /// <summary>
        ///     Initialize a new instance of RPClient by doing the following:
        /// 1- Get the settings from the config file
        /// 2- Create/Read the SSL Certificate
        /// 3- Start the client
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="nodeConfig">rpc node config</param>
        public NodeRpcClient(X509Certificate certificate,
            IRpcNodeConfig nodeConfig) : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            var anySignedTypeClientHandler = new AnySignedTypeClientHandler();
            MessageStream = anySignedTypeClientHandler.MessageStream;

            _getInfoResponseHandler = new GetInfoResponseHandler(MessageStream, Logger);
            _getVersionResponseHandler = new GetVersionResponseHandler(MessageStream, Logger);
            _getMempoolResponseHandler = new GetMempoolResponseHandler(MessageStream, Logger);

            IList<IChannelHandler> channelHandlers = new List<IChannelHandler>
            {
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(AnySigned.Parser),
                anySignedTypeClientHandler
            };

            Bootstrap(
                new OutboundChannelInitializer<ISocketChannel>(channel => { },
                    channelHandlers,
                    nodeConfig.HostAddress,
                    certificate
                ), EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port)
            );
        }

        /// <inheritdoc />
        public void SubscribeStream(IObserver<IChanneledMessage<AnySigned>> observer)
        {
            MessageStream.Subscribe(observer);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _getInfoResponseHandler.Dispose();
                _getVersionResponseHandler.Dispose();
                _getMempoolResponseHandler.Dispose();
            }

            base.Dispose();
        }
    }
}
