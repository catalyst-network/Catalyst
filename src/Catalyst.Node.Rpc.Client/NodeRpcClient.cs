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
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.IO.Transport;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Network;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Transport;
using Catalyst.Protocol.Common;
using DotNetty.Codecs.Protobuf;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Serilog;

namespace Catalyst.Node.Rpc.Client
{
    /// <summary>
    ///     This class provides a command line interface (CLI) application to connect to Catalyst Node.
    ///     Through the CLI the node operator will be able to connect to any number of running nodes and run commands.
    /// </summary>
    internal sealed class NodeRpcClient : TcpClient, INodeRpcClient
    {
        /// <summary>
        ///     Initialize a new instance of RPClient
        /// </summary>
        /// <param name="channelFactory"></param>
        /// <param name="certificate"></param>
        /// <param name="nodeConfig">rpc node config</param>
        /// <param name="handlers"></param>
        public NodeRpcClient(ITcpClientChannelFactory channelFactory,
            X509Certificate2 certificate, 
            IRpcNodeConfig nodeConfig,
            IEnumerable<IRpcResponseMessageObserver> handlers) 
            : base(channelFactory, Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType))
        {
            var socket = channelFactory.BuildChannel(nodeConfig.HostAddress, nodeConfig.Port, certificate);
            handlers.ToList().ForEach(handler => handler.StartObserving(socket.MessageStream));
            Channel = socket.Channel;
        }
    }
}
