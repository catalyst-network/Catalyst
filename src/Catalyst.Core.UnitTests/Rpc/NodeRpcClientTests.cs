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
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.EventLoop;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.IO.Transport.Channels;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.IO.Transport.Channels;
using Catalyst.Core.Rpc;
using Catalyst.Core.Rpc.IO.Exceptions;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc
{
    public sealed class NodeRpcClientTests
    {
        public NodeRpcClientTests()
        {
            _testScheduler = new TestScheduler();
            _logger = Substitute.For<ILogger>();
            _peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test");
            _channelHandlerContext = Substitute.For<IChannelHandlerContext>();

            _channelFactory = Substitute.For<ITcpClientChannelFactory>();
            _clientEventLoopGroupFactory = Substitute.For<ITcpClientEventLoopGroupFactory>();

            _mockSocketReplySubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1, _testScheduler);
            var mockChannel = Substitute.For<IChannel>();
            var mockEventStream = _mockSocketReplySubject.AsObservable();
            var observableChannel = new ObservableChannel(mockEventStream, mockChannel);

            _channelFactory.BuildChannel(_clientEventLoopGroupFactory, Arg.Any<IPAddress>(), Arg.Any<int>(),
                Arg.Any<X509Certificate2>()).Returns(observableChannel);

            _rpcNodeConfig = Substitute.For<IRpcNodeConfig>();
            _rpcNodeConfig.HostAddress = IPAddress.Any;
            _rpcNodeConfig.NodeId = "0";
            _rpcNodeConfig.PfxFileName = "pfx";
            _rpcNodeConfig.Port = 9000;
        }

        private readonly ILogger _logger;

        private readonly TestScheduler _testScheduler;

        private readonly IPeerIdentifier _peerIdentifier;

        private readonly ITcpClientChannelFactory _channelFactory;

        private readonly ITcpClientEventLoopGroupFactory _clientEventLoopGroupFactory;

        private readonly IRpcNodeConfig _rpcNodeConfig;

        private readonly IChannelHandlerContext _channelHandlerContext;

        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _mockSocketReplySubject;

        [Fact]
        public async Task SubscribeToResponse_Should_Not_Return_Invalid_Response()
        {
            var nodeRpcClientFactory = new RpcClientFactory(_channelFactory, _clientEventLoopGroupFactory,
                new List<IRpcResponseObserver> {new GetVersionResponseObserver(_logger)});
            var nodeRpcClient = await nodeRpcClientFactory.GetClient(null, _rpcNodeConfig);
            var receivedResponse = false;
            var targetVersionResponse = new VersionResponse {Version = "1.2.3.4"};
            var protocolMessage =
                targetVersionResponse.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());
            var observerDto = new ObserverDto(_channelHandlerContext, protocolMessage);

            _mockSocketReplySubject.OnNext(observerDto);
            nodeRpcClient.SubscribeToResponse<GetDeltaResponse>(response => receivedResponse = true);
            _testScheduler.Start();

            receivedResponse.Should().BeFalse();
        }

        [Fact]
        public async Task SubscribeToResponse_Should_Return_Response()
        {
            var nodeRpcClientFactory = new RpcClientFactory(_channelFactory, _clientEventLoopGroupFactory,
                new List<IRpcResponseObserver> {new GetVersionResponseObserver(_logger)});
            var nodeRpcClient = await nodeRpcClientFactory.GetClient(null, _rpcNodeConfig);
            VersionResponse returnedVersionResponse = null;
            var targetVersionResponse = new VersionResponse {Version = "1.2.3.4"};
            var protocolMessage =
                targetVersionResponse.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());
            var observerDto = new ObserverDto(_channelHandlerContext, protocolMessage);

            _mockSocketReplySubject.OnNext(observerDto);
            nodeRpcClient.SubscribeToResponse<VersionResponse>(response => returnedVersionResponse = response);
            _testScheduler.Start();

            targetVersionResponse.Should().Be(returnedVersionResponse);
        }

        [Fact]
        public async Task SubscribeToResponse_Without_Response_Handler_Should_Throw_Exception()
        {
            var nodeRpcClientFactory = new RpcClientFactory(_channelFactory, _clientEventLoopGroupFactory,
                new List<IRpcResponseObserver>());
            var nodeRpcClient = await nodeRpcClientFactory.GetClient(null, _rpcNodeConfig);
            var targetVersionResponse = new VersionResponse {Version = "1.2.3.4"};
            var protocolMessage =
                targetVersionResponse.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());
            var observerDto = new ObserverDto(_channelHandlerContext, protocolMessage);

            var exception = Record.Exception(() =>
            {
                _mockSocketReplySubject.OnNext(observerDto);
                nodeRpcClient.SubscribeToResponse<VersionResponse>(response => { });
                _testScheduler.Start();
            });

            exception.Should().BeOfType<ResponseHandlerDoesNotExistException>();
        }
    }
}
