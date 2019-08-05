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

using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Rpc;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Catalyst.Node.Rpc.Client
{
    public sealed class NodeRpcClientFactory : INodeRpcClientFactory
    {
        private readonly ITcpClientChannelFactory _channelFactory;
        private readonly IEnumerable<IRpcResponseObserver> _handlers;
        private readonly ITcpClientEventLoopGroupFactory _clientEventLoopGroupFactory;

        public NodeRpcClientFactory(ITcpClientChannelFactory channelFactory,
            ITcpClientEventLoopGroupFactory clientEventLoopGroupFactory,
            IEnumerable<IRpcResponseObserver> handlers)
        {
            _clientEventLoopGroupFactory = clientEventLoopGroupFactory;
            _channelFactory = channelFactory;
            _handlers = handlers;
        }

        public async Task<INodeRpcClient> GetClient(X509Certificate2 certificate, IRpcNodeConfig nodeConfig)
        {
            var nodeRpcClient = new NodeRpcClient(_channelFactory, certificate, nodeConfig, _handlers, _clientEventLoopGroupFactory);

            await nodeRpcClient.StartAsync();
            return nodeRpcClient;
        }
    }
}
