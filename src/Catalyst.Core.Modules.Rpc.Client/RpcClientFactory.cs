#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Abstractions.Rpc;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.EventLoop;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Transport.Channels;
using Catalyst.Modules.Network.Dotnetty.IO.Observers;
using Catalyst.Modules.Network.Dotnetty.Rpc;
using Catalyst.Protocol.Wire;

namespace Catalyst.Core.Modules.Rpc.Client
{
    public sealed class RpcClientFactory : IRpcClientFactory
    {
        private readonly ITcpClientChannelFactory<IObserverDto<ProtocolMessage>> _channelFactory;
        private readonly IEnumerable<IRpcResponseObserver> _handlers;
        private readonly ITcpClientEventLoopGroupFactory _clientEventLoopGroupFactory;

        public RpcClientFactory(ITcpClientChannelFactory<IObserverDto<ProtocolMessage>> channelFactory,
            ITcpClientEventLoopGroupFactory clientEventLoopGroupFactory,
            IEnumerable<IRpcResponseObserver> handlers)
        {
            _clientEventLoopGroupFactory = clientEventLoopGroupFactory;
            _channelFactory = channelFactory;
            _handlers = handlers;
        }

        public async Task<IRpcClient> GetClientAsync(X509Certificate2 certificate, IRpcClientConfig clientConfig)
        {
            RpcClient nodeRpcClient = new(_channelFactory, certificate, clientConfig, _handlers, _clientEventLoopGroupFactory);

            await nodeRpcClient.StartAsync();
            return nodeRpcClient;
        }
    }
}
