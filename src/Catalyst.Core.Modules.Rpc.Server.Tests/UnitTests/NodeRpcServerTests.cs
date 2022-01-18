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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
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
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.EventLoop;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Transport.Channels;
using Catalyst.Modules.Network.Dotnetty.IO.Messaging.Dto;
using Catalyst.Modules.Network.Dotnetty.IO.Observers;

namespace Catalyst.Core.Modules.Rpc.Server.Tests.UnitTests
{
    public sealed class RpcServerTests
    {
        public RpcServerTests()
        {
            var logger = Substitute.For<ILogger>();
            _testScheduler = new TestScheduler();
            _rpcServerSettings = Substitute.For<IRpcServerSettings>();
            _peerId = MultiAddressHelper.GetAddress(nameof(RpcServerTests));
            _channelHandlerContext = Substitute.For<IChannelHandlerContext>();
            _mockSocketReplySubject = new ReplaySubject<IObserverDto<ProtocolMessage>>(1, _testScheduler);

            var tcpServerEventLoopGroupFactory = Substitute.For<ITcpServerEventLoopGroupFactory>();
            var tcpServerChannelFactory = Substitute.For<ITcpServerChannelFactory<IObserverDto<ProtocolMessage>>>();
            tcpServerChannelFactory.BuildChannelAsync(tcpServerEventLoopGroupFactory, Arg.Any<MultiAddress>(),
                Arg.Any<X509Certificate2>()).Returns(ObservableHelpers.MockRpcObservableChannel(_mockSocketReplySubject));

            var certificateStore = Substitute.For<ICertificateStore>();

            // ReSharper disable once CollectionNeverUpdated.Local
            List<IRpcRequestObserver> requestHandlers = new();

            _rpcServer = new RpcServer(_rpcServerSettings, logger, tcpServerChannelFactory, certificateStore,
                requestHandlers, tcpServerEventLoopGroupFactory);
        }

        private readonly ReplaySubject<IObserverDto<ProtocolMessage>> _mockSocketReplySubject;
        private readonly TestScheduler _testScheduler;
        private readonly MultiAddress _peerId;
        private readonly RpcServer _rpcServer;
        private readonly IRpcServerSettings _rpcServerSettings;
        private readonly IChannelHandlerContext _channelHandlerContext;

        [Test]
        public void Get_Settings_From_RpcServer_Should_Return_Settings()
        {
            _rpcServer.Settings.Should().Be(_rpcServerSettings);
        }

        [Test]
        public async Task Subscribe_To_Message_Stream_Should_Return_VersionRequest()
        {
            await _rpcServer.StartAsync();

            VersionRequest returnedVersionRequest = null;
            var targetVersionRequest = new VersionRequest { Query = true };

            var protocolMessage =
                targetVersionRequest.ToProtocolMessage(_peerId, CorrelationId.GenerateCorrelationId());

            var observerDto = new ObserverDto(_channelHandlerContext, protocolMessage);

            _rpcServer.MessageStream
               .Where(x => x.Payload != null && x.Payload.TypeUrl == typeof(VersionRequest).ShortenedProtoFullName())
               .SubscribeOn(_testScheduler)
               .Subscribe(request => returnedVersionRequest = request.Payload.FromProtocolMessage<VersionRequest>());

            _mockSocketReplySubject.OnNext(observerDto);

            _testScheduler.Start();

            targetVersionRequest.Should().Be(returnedVersionRequest);
        }
    }
}
