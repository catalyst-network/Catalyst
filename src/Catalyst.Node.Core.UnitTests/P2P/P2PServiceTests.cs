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
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Node.Common.Helpers.Config;
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO.Inbound;
using Catalyst.Node.Common.Helpers.Util;
using Catalyst.Node.Common.Interfaces.Messaging;
using Catalyst.Node.Common.Interfaces.P2P;
using Catalyst.Node.Common.P2P;
using Catalyst.Node.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Node.Core.P2P.Messaging;
using Catalyst.Node.Core.P2P.Messaging.Handlers;
using Catalyst.Node.Core.UnitTest.TestUtils;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public sealed class P2PServiceTests : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;

        public P2PServiceTests(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build(), CurrentTestName);

            ConfigureContainerBuilder(_config, true, true);
        }

        [Fact]
        public void DoesResolveIp2PServiceCorrectly()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var p2PService = container.Resolve<IP2PService>();
                Assert.NotNull(p2PService);
                p2PService.Should().BeOfType(typeof(P2PService));
                p2PService.Dispose();
                scope.Dispose();
            }
        }

        [Fact]
        public void CanReceiveEventsFromSubscribedStream()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var logger = container.Resolve<ILogger>();
                var fakeContext = Substitute.For<IChannelHandlerContext>();
                var fakeChannel = Substitute.For<IChannel>();
                var fakeReputationCache = Substitute.For<IReputableCache>();
                fakeContext.Channel.Returns(fakeChannel);

                var pingRequest = new PingRequest();
                var pid = PeerIdentifierHelper.GetPeerIdentifier("im_a_key");
                var cid = Guid.NewGuid();
                var channeledAny = new ChanneledAnySigned(fakeContext, pingRequest.ToAnySigned(pid.PeerId, Guid.NewGuid()));
            
                var observableStream = new[] {channeledAny}.ToObservable();
            
                var handler = new PingRequestHandler(pid, fakeReputationCache, logger);
                handler.StartObserving(observableStream);
            
                fakeContext.Channel.ReceivedWithAnyArgs(1)
                   .WriteAndFlushAsync(new PingResponse().ToAnySigned(pid.PeerId, cid));
            }          
        }
        
        [Fact]
        public void CanReceivePingRequests()
        {
            var container = ContainerBuilder.Build();
            using (var scope = container.BeginLifetimeScope(CurrentTestName))
            {
                var logger = container.Resolve<ILogger>();
                var p2PService = container.Resolve<IP2PService>();
                var serverObserver = new AnySignedMessageObserver(0, logger);

                using (p2PService.MessageStream.Subscribe(serverObserver))
                {
                    var peerSettings = new PeerSettings(_config);
                    var targetHost = new IPEndPoint(peerSettings.BindAddress, peerSettings.Port);
                    var peerClient = new PeerClient(targetHost, container.Resolve<IEnumerable<IP2PMessageHandler>>());

                    var datagramEnvelope = new P2PMessageFactory<PingResponse, P2PMessages>().GetMessageInDatagramEnvelope(
                        new P2PMessageDto<PingResponse, P2PMessages>(
                            P2PMessages.PingRequest,
                            new PingResponse(),
                            targetHost,
                            new PeerIdentifier(ByteUtil.InitialiseEmptyByteArray(20), peerSettings.BindAddress, peerSettings.Port)
                        )
                    );
                    
                    peerClient.SendMessage(datagramEnvelope).GetAwaiter().GetResult();
                    
                    var tasks = new IChanneledMessageStreamer<AnySigned>[]
                        {
                            p2PService, peerClient
                        }
                       .Select(async p => await p.MessageStream.FirstAsync(a => a != null && a != NullObjects.ChanneledAnySigned))
                       .ToArray();
                    Task.WaitAll(tasks, TimeSpan.FromMilliseconds(2000));

                    serverObserver.Received.Should().NotBeNull();
                    serverObserver.Received.Payload.TypeUrl.Should().Be(PingResponse.Descriptor.ShortenedFullName());
                    p2PService.Dispose();
                    scope.Dispose();
                }
            }
        }
    }
}
