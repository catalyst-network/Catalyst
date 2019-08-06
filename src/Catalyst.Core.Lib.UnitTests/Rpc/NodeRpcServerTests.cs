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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
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
using Catalyst.Core.Lib.Rpc;
using Catalyst.Core.Lib.UnitTests.Helpers;
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
            var logger = Substitute.For<ILogger>();
            _testScheduler = new TestScheduler();
            _rpcServerSettings = Substitute.For<IRpcServerSettings>();
            _peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("", "", 0);
            _channelHandlerContext = Substitute.For<IChannelHandlerContext>();
            _mockSocketReplySubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1, _testScheduler);

            var tcpServerEventLoopGroupFactory = Substitute.For<ITcpServerEventLoopGroupFactory>();
            var tcpServerChannelFactory = Substitute.For<ITcpServerChannelFactory>();
            tcpServerChannelFactory.BuildChannel(tcpServerEventLoopGroupFactory, Arg.Any<IPAddress>(), Arg.Any<int>(),
                Arg.Any<X509Certificate2>()).Returns(ObservableHelpers.MockObservableChannel(_mockSocketReplySubject));

            var certificateStore = Substitute.For<ICertificateStore>();
            var requestHandlers = new List<IRpcRequestObserver>();

            _nodeRpcServer = new NodeRpcServer(_rpcServerSettings, logger, tcpServerChannelFactory, certificateStore,
                requestHandlers, tcpServerEventLoopGroupFactory);
        }

        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _mockSocketReplySubject;
        private readonly TestScheduler _testScheduler;
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
        public async Task Subscribe_To_Message_Stream_Should_Return_VersionRequest()
        {
            await _nodeRpcServer.StartAsync();

            VersionRequest returnedVersionRequest = null;
            var targetVersionRequest = new VersionRequest {Query = true};

            var protocolMessage =
                targetVersionRequest.ToProtocolMessage(_peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());

            var observerDto = new ObserverDto(_channelHandlerContext, protocolMessage);

            _nodeRpcServer.MessageStream
               .Where(x => x.Payload != null && x.Payload.TypeUrl == typeof(VersionRequest).ShortenedProtoFullName())
               .SubscribeOn(_testScheduler)
               .Subscribe(request => returnedVersionRequest = request.Payload.FromProtocolMessage<VersionRequest>());

            _mockSocketReplySubject.OnNext(observerDto);

            _testScheduler.Start();

            targetVersionRequest.Should().Be(returnedVersionRequest);
        }
    }
}
