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
using System.Reactive.Subjects;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests
{
    public sealed class NodeRpcClientTests
    {
        public NodeRpcClientTests()
        {
            _testScheduler = new TestScheduler();
            _peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("", "", 0);
            _channelHandlerContext = Substitute.For<IChannelHandlerContext>();

            var channelFactory = Substitute.For<ITcpClientChannelFactory>();
            var clientEventLoopGroupFactory = Substitute.For<ITcpClientEventLoopGroupFactory>();
            var nodeRpcClientFactory = new NodeRpcClientFactory(channelFactory, clientEventLoopGroupFactory,
                new List<IRpcResponseObserver>());

            _mockSocketReplySubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1, _testScheduler);
            var mockChannel = Substitute.For<IChannel>();
            var mockEventStream = _mockSocketReplySubject.AsObservable();
            var observableChannel = new ObservableChannel(mockEventStream, mockChannel);

            channelFactory.BuildChannel(clientEventLoopGroupFactory, Arg.Any<IPAddress>(), Arg.Any<int>(),
                Arg.Any<X509Certificate2>()).Returns(observableChannel);

            var nodeRpcConfig = Substitute.For<IRpcNodeConfig>();
            nodeRpcConfig.HostAddress = IPAddress.Any;
            nodeRpcConfig.NodeId = "0";
            nodeRpcConfig.PfxFileName = "pfx";
            nodeRpcConfig.Port = 9000;

            _nodeRpcClient = nodeRpcClientFactory.GetClient(null, nodeRpcConfig);
        }

        private readonly TestScheduler _testScheduler;

        private readonly IPeerIdentifier _peerIdentifier;

        private readonly IChannelHandlerContext _channelHandlerContext;

        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _mockSocketReplySubject;

        private readonly INodeRpcClient _nodeRpcClient;

        [Fact]
        public void SubscribeToResponse_Should_Not_Return_Invalid_Response()
        {
            var receivedResponse = false;
            var targetVersionResponse = new VersionResponse {Version = "1.2.3.4"};

            var protocolMessage =
                targetVersionResponse.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());

            var observerDto = new ObserverDto(_channelHandlerContext, protocolMessage);

            _mockSocketReplySubject.OnNext(observerDto);

            _nodeRpcClient.SubscribeToResponse<GetDeltaResponse>(response => receivedResponse = true);

            _testScheduler.Start();

            receivedResponse.Should().BeFalse();
        }

        [Fact]
        public void SubscribeToResponse_Should_Return_Response()
        {
            VersionResponse returnedVersionResponse = null;
            var targetVersionResponse = new VersionResponse {Version = "1.2.3.4"};

            var protocolMessage =
                targetVersionResponse.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());

            var observerDto = new ObserverDto(_channelHandlerContext, protocolMessage);

            _mockSocketReplySubject.OnNext(observerDto);

            _nodeRpcClient.SubscribeToResponse<VersionResponse>(response => returnedVersionResponse = response);

            _testScheduler.Start();

            targetVersionResponse.Should().Be(returnedVersionResponse);
        }
    }
}
