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

using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Rpc;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Catalyst.Node.Rpc.Client
{
    public sealed class NodeRpcClientFactory : INodeRpcClientFactory
    {
        private readonly ITcpClientChannelFactory _channelFactory;
        private readonly IEnumerable<IRpcResponseObserver> _handlers;
        private readonly IHandlerWorkerEventLoopGroupFactory _handlerWorkerEventLoopGroupFactory;

        public NodeRpcClientFactory(ITcpClientChannelFactory channelFactory,
            IEnumerable<IRpcResponseObserver> handlers,
            IHandlerWorkerEventLoopGroupFactory handlerWorkerEventLoopGroupFactory)
        {
            _channelFactory = channelFactory;
            _handlerWorkerEventLoopGroupFactory = handlerWorkerEventLoopGroupFactory;
            _handlers = handlers;
        }

        public INodeRpcClient GetClient(X509Certificate2 certificate, IRpcNodeConfig nodeConfig)
        {
            return new NodeRpcClient(_channelFactory, certificate, nodeConfig, _handlerWorkerEventLoopGroupFactory, _handlers);
        }
    }
}
