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
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.IO.EventLoop;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.IO.Transport.Channels;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Transport.Channels;
using Catalyst.Core.Lib.Rpc;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.Rpc
{
    public sealed class NodeRpcServerTests
    {
        public NodeRpcServerTests()
        {
            _testScheduler = new TestScheduler();
            _rpcServerSettings = Substitute.For<IRpcServerSettings>();
            _logger = Substitute.For<ILogger>();
            _peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("", "", 0);
            _channelHandlerContext = Substitute.For<IChannelHandlerContext>();

            _clientEventLoopGroupFactory = Substitute.For<ITcpClientEventLoopGroupFactory>();

            _mockSocketReplySubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1, _testScheduler);
            var mockChannel = Substitute.For<IChannel>();
            var mockEventStream = _mockSocketReplySubject.AsObservable();
            var observableChannel = new ObservableChannel(mockEventStream, mockChannel);

            _tcpServerChannelFactory.BuildChannel(_clientEventLoopGroupFactory, Arg.Any<IPAddress>(), Arg.Any<int>(),
                Arg.Any<X509Certificate2>()).Returns(observableChannel);

            var certificateStore = Substitute.For<ICertificateStore>();
            var requestHandlers = new List<IRpcRequestObserver>();
            var tcpServerEventLoopGroupFactory = Substitute.For<ITcpServerEventLoopGroupFactory>();
            _nodeRpcServer = new NodeRpcServer(_rpcServerSettings, _logger, _tcpServerChannelFactory, certificateStore,
                requestHandlers, tcpServerEventLoopGroupFactory);
        }

        private readonly ITcpServerChannelFactory _tcpServerChannelFactory;
        private readonly ITcpClientEventLoopGroupFactory _clientEventLoopGroupFactory;
        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _mockSocketReplySubject;

        private readonly TestScheduler _testScheduler;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _peerIdentifier;
        private readonly NodeRpcServer _nodeRpcServer;
        private readonly IRpcServerSettings _rpcServerSettings;
        private readonly IChannelHandlerContext _channelHandlerContext;

        [Fact]
        public void Get_Settings_From_NodeRpcServer_Should_Return_Settings()
        {
            _nodeRpcServer.Settings.Should().Be(_rpcServerSettings);
        }

        [Fact]
        public void Subscribe_To_Message_Stream_Should_Return_VersionRequest()
        {
            VersionRequest returnedVersionRequest = null;
            var targetVersionRequest = new VersionRequest();

            var protocolMessage =
                targetVersionRequest.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());

            var observerDto = new ObserverDto(_channelHandlerContext, protocolMessage);

            var iDisposable = _nodeRpcServer.MessageStream
               .Where(x => x.Payload.TypeUrl == typeof(VersionRequest).ShortenedProtoFullName())
               .SubscribeOn(_testScheduler)
               .Subscribe(request => returnedVersionRequest = request.Payload.FromProtocolMessage<VersionRequest>());

            _mockSocketReplySubject.OnNext(observerDto);

            _testScheduler.Start();

            targetVersionRequest.Should().Be(returnedVersionRequest);
        }
    }
}
