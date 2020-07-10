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
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Lib.IO.Transport.Channels;
using Catalyst.Core.Lib.Rpc.IO.Exceptions;
using Catalyst.Core.Modules.Rpc.Client.IO.Observers;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using MultiFormats;

namespace Catalyst.Core.Modules.Rpc.Client.Tests.UnitTests
{
    public sealed class RpcClientTests
    {
        public RpcClientTests()
        {
            _testScheduler = new TestScheduler();
            _logger = Substitute.For<ILogger>();
            _peerIdentifier = MultiAddressHelper.GetAddress("Test");

            _channelFactory = Substitute.For<ITcpClientChannelFactory>();
            _clientEventLoopGroupFactory = Substitute.For<ITcpClientEventLoopGroupFactory>();

            _mockSocketReplySubject = new ReplaySubject<ProtocolMessage>(1, _testScheduler);
            var mockChannel = Substitute.For<IChannel>();
            var mockEventStream = _mockSocketReplySubject.AsObservable();
            var observableChannel = new ObservableChannel(mockEventStream, mockChannel);

            _channelFactory.BuildChannelAsync(_clientEventLoopGroupFactory, Arg.Any<MultiAddress>(),
                Arg.Any<X509Certificate2>()).Returns(observableChannel);

            _rpcClientConfig = Substitute.For<IRpcClientConfig>();
            _rpcClientConfig.NodeId = "0";
            _rpcClientConfig.PfxFileName = "pfx";
            _rpcClientConfig.Address = new MultiAddress("/ip4/127.0.0.1/tcp/4001/ipfs/18n3naE9kBZoVvgYMV6saMZdwu2yu3QMzKa2BDkb5C5pcuhtrH1G9HHbztbbxA8tGmf4");
        }

        private readonly ILogger _logger;

        private readonly TestScheduler _testScheduler;

        private readonly MultiAddress _peerIdentifier;

        private readonly ITcpClientChannelFactory _channelFactory;

        private readonly ITcpClientEventLoopGroupFactory _clientEventLoopGroupFactory;

        private readonly IRpcClientConfig _rpcClientConfig;

        private readonly ReplaySubject<ProtocolMessage> _mockSocketReplySubject;

        [Test]
        public async Task SubscribeToResponse_Should_Not_Return_Invalid_Response()
        {
            var nodeRpcClientFactory = new RpcClientFactory(_channelFactory, _clientEventLoopGroupFactory,
                new List<IRpcResponseObserver> { new GetVersionResponseObserver(_logger) });
            var nodeRpcClient = await nodeRpcClientFactory.GetClientAsync(null, _rpcClientConfig);
            var receivedResponse = false;
            var targetVersionResponse = new VersionResponse { Version = "1.2.3.4" };
            var protocolMessage =
                targetVersionResponse.ToProtocolMessage(_peerIdentifier, CorrelationId.GenerateCorrelationId());

            _mockSocketReplySubject.OnNext(protocolMessage);
            nodeRpcClient.SubscribeToResponse<GetDeltaResponse>(response => receivedResponse = true);
            _testScheduler.Start();

            receivedResponse.Should().BeFalse();
        }

        [Test]
        public async Task SubscribeToResponse_Should_Return_Response()
        {
            var nodeRpcClientFactory = new RpcClientFactory(_channelFactory, _clientEventLoopGroupFactory,
                new List<IRpcResponseObserver> { new GetVersionResponseObserver(_logger) });
            var nodeRpcClient = await nodeRpcClientFactory.GetClientAsync(null, _rpcClientConfig);
            VersionResponse returnedVersionResponse = null;
            var targetVersionResponse = new VersionResponse { Version = "1.2.3.4" };
            var protocolMessage =
                targetVersionResponse.ToProtocolMessage(_peerIdentifier, CorrelationId.GenerateCorrelationId());

            _mockSocketReplySubject.OnNext(protocolMessage);
            nodeRpcClient.SubscribeToResponse<VersionResponse>(response => returnedVersionResponse = response);
            _testScheduler.Start();

            targetVersionResponse.Should().Be(returnedVersionResponse);
        }

        [Test]
        public async Task SubscribeToResponse_Without_Response_Handler_Should_Throw_Exception()
        {
            var nodeRpcClientFactory = new RpcClientFactory(_channelFactory, _clientEventLoopGroupFactory,
                new List<IRpcResponseObserver>());
            var nodeRpcClient = await nodeRpcClientFactory.GetClientAsync(null, _rpcClientConfig);
            var targetVersionResponse = new VersionResponse { Version = "1.2.3.4" };
            var protocolMessage =
                targetVersionResponse.ToProtocolMessage(_peerIdentifier, CorrelationId.GenerateCorrelationId());

            Assert.Throws<ResponseHandlerDoesNotExistException>(() =>
            {
                _mockSocketReplySubject.OnNext(protocolMessage);
                nodeRpcClient.SubscribeToResponse<VersionResponse>(response => { });
                _testScheduler.Start();
            });
        }
    }
}
