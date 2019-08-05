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
