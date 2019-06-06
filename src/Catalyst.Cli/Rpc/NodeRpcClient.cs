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
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.IO.Outbound;
using Catalyst.Common.Network;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Handlers;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace Catalyst.Cli.Rpc
{
    /// <summary>
    ///     This class provides a command line interface (CLI) application to connect to Catalyst Node.
    ///     Through the CLI the node operator will be able to connect to any number of running nodes and run commands.
    /// </summary>
    internal sealed class NodeRpcClient
        : TcpClient<TcpSocketChannel>,
            INodeRpcClient
    {
        /// <summary>
        ///     Initialize a new instance of RPClient
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="nodeConfig">rpc node config</param>
        public NodeRpcClient(X509Certificate certificate, 
            IRpcNodeConfig nodeConfig) 
            : base(Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            IList<IChannelHandler> channelHandlers = new List<IChannelHandler>
            {
                new ProtobufVarint32LengthFieldPrepender(),
                new ProtobufEncoder(),
                new ProtobufVarint32FrameDecoder(),
                new ProtobufDecoder(ProtocolMessage.Parser)
            };

            Bootstrap(
                new OutboundChannelInitializerBase<ISocketChannel>(channel => { },
                    channelHandlers,
                    nodeConfig.HostAddress,
                    certificate
                ), EndpointBuilder.BuildNewEndPoint(nodeConfig.HostAddress, nodeConfig.Port)
            );
        }
    }
}
