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
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.Rpc;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests
{
    public sealed class NodeRpcClientFactoryTests
    {
        public NodeRpcClientFactoryTests()
        {
            var channelFactory = Substitute.For<ITcpClientChannelFactory>();
            var clientEventLoopGroupFactory = Substitute.For<ITcpClientEventLoopGroupFactory>();
            _nodeRpcClientFactory = new NodeRpcClientFactory(channelFactory, clientEventLoopGroupFactory,
                new List<IRpcResponseObserver>());
        }

        private readonly NodeRpcClientFactory _nodeRpcClientFactory;

        [Fact]
        public void GetClient_Should_Return_NodeRpcClient()
        {
            var nodeRpcConfig = Substitute.For<IRpcNodeConfig>();
            nodeRpcConfig.HostAddress = IPAddress.Any;
            nodeRpcConfig.NodeId = "0";
            nodeRpcConfig.PfxFileName = "pfx";
            nodeRpcConfig.Port = 9000;
            var rpcClient = _nodeRpcClientFactory.GetClient(null, nodeRpcConfig);

            rpcClient.Should().BeAssignableTo<INodeRpcClient>();
        }
    }
}
