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
using System.IO;
using System.Net;
using System.Reactive.Linq;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Node.Core.UnitTests.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.P2P
{
    public sealed class P2PServiceTests : ConfigFileBasedTest
    {
        private readonly Guid _guid;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _pid;
        private IContainer _container;
        private readonly PingRequest _pingRequest;
        private readonly IConfigurationRoot _config;

        public P2PServiceTests(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Test)))
               .Build(), CurrentTestName);
            _pid = PeerIdentifierHelper.GetPeerIdentifier("im_a_key");
            _guid = Guid.NewGuid();
            _logger = Substitute.For<ILogger>();
            _pingRequest = new PingRequest();

            ConfigureContainerBuilder(_config, true, true);
        }

        [Fact]
        public void DoesResolveIp2PServiceCorrectly()
        {
            _container = ContainerBuilder.Build();
            using (var scope = _container.BeginLifetimeScope(CurrentTestName))
            {
                var p2PService = _container.Resolve<IP2PService>();
                Assert.NotNull(p2PService);
                p2PService.Should().BeOfType(typeof(P2PService));
                p2PService.Dispose();
                scope.Dispose();
            }
        }

        [Fact]
        public void CanReceiveEventsFromSubscribedStream()
        {
            _container = ContainerBuilder.Build();
            using (_container.BeginLifetimeScope(CurrentTestName))
            {
                var fakeContext = Substitute.For<IChannelHandlerContext>();
                var fakeChannel = Substitute.For<IChannel>();
                fakeContext.Channel.Returns(fakeChannel);
                var channeledAny = new ChanneledAnySigned(fakeContext, _pingRequest.ToAnySigned(_pid.PeerId, _guid));
                var observableStream = new[] { channeledAny }.ToObservable();

                var handler = new PingRequestHandler(_pid, _logger);
                handler.StartObserving(observableStream);

                fakeContext.Channel.ReceivedWithAnyArgs(1)
                   .WriteAndFlushAsync(new PingResponse().ToAnySigned(_pid.PeerId, _guid));
            }
        }

        [Fact]
        public void CanReceivePingResponse()
        {
            AnySigned message = null;
            var testHandler = new TestP2PMessageHandler<PingResponse>((anySigned) => message = anySigned);
            var protoDatagramChannelHandler = new ProtoDatagramChannelHandler();
            testHandler.StartObserving(protoDatagramChannelHandler.MessageStream);

            EmbeddedChannel channel = new EmbeddedChannel(
                protoDatagramChannelHandler
            );

            var pingResponse = new PingResponse()
               .ToAnySigned(PeerIdHelper.GetPeerId("Any"), Guid.NewGuid()).ToDatagram(new IPEndPoint(IPAddress.Any, 5050));

            channel.WriteInbound(pingResponse);

            message.Should().NotBeNull();
            message.TypeUrl.Should().Be(PingResponse.Descriptor.ShortenedFullName());

        }

        [Fact]
        public void CanReceiveNeighbourRequests()
        {
            AnySigned message = null;
            var testHandler = new TestP2PMessageHandler<PeerNeighborsResponse>((anySigned) => message = anySigned);
            var protoDatagramChannelHandler = new ProtoDatagramChannelHandler();
            testHandler.StartObserving(protoDatagramChannelHandler.MessageStream);

            EmbeddedChannel channel = new EmbeddedChannel(
                protoDatagramChannelHandler
            );

            var pingResponse = new PeerNeighborsResponse()
               .ToAnySigned(PeerIdHelper.GetPeerId("Any"), Guid.NewGuid()).ToDatagram(new IPEndPoint(IPAddress.Any, 5050));

            channel.WriteInbound(pingResponse);

            message.Should().NotBeNull();
            message.TypeUrl.Should().Be(PeerNeighborsResponse.Descriptor.ShortenedFullName());
        }
    }
}
