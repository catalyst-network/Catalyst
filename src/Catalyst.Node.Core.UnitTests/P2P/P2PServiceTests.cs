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
using System.Threading.Tasks;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Inbound;
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Common.UnitTests.TestUtils;
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
using NSubstitute.ReceivedExtensions;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTest.P2P
{
    public sealed class P2PServiceTests : ConfigFileBasedTest
    {
        private readonly IConfigurationRoot _config;
        private readonly IPeerIdentifier _pid;
        private readonly Guid _guid;
        private readonly ILogger _logger;
        private readonly PingRequest _pingRequest;

        public P2PServiceTests(ITestOutputHelper output) : base(output)
        {
            _config = SocketPortHelper.AlterConfigurationToGetUniquePort(new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
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
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var fakeContext = Substitute.For<IChannelHandlerContext>();
                var fakeChannel = Substitute.For<IChannel>();
                fakeContext.Channel.Returns(fakeChannel);
                var channeledAny = new ChanneledAnySigned(fakeContext, _pingRequest.ToAnySigned(_pid.PeerId, _guid));
                var observableStream = new[] {channeledAny}.ToObservable();
            
                var handler = new PingRequestAskHandler(_pid, _logger);
                handler.StartObserving(observableStream);
            
                fakeContext.Channel.ReceivedWithAnyArgs(1)
                   .WriteAndFlushAsync(new PingResponse().ToAnySigned(_pid.PeerId, _guid));
            }
        }
        
        // [Fact]
        // public void PingRequestIsRegisteredInReputationCache()
        // {
        //     var container = ContainerBuilder.Build();
        //     using (container.BeginLifetimeScope(CurrentTestName))
        //     {
        //         var fakeContext = Substitute.For<IChannelHandlerContext>();
        //         var handler = new PingRequestAskHandler(_pid, _logger);
        //         var channeledAny = new ChanneledAnySigned(fakeContext, _pingRequest.ToAnySigned(_pid.PeerId, _guid));
        //
        //         handler.HandleMessage(channeledAny);
        //         
        //         handler.ReputableCache.ReceivedWithAnyArgs(1);
        //     }
        // }
        
        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void CanReceivePingRequests()
        {
            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var p2PService = container.Resolve<IP2PService>();
                var serverObserver = new AnySignedMessageObserver(0, _logger);

                using (p2PService.MessageStream.Subscribe(serverObserver))
                {
                    var peerSettings = new PeerSettings(_config);
                    var targetHost = new IPEndPoint(peerSettings.BindAddress, peerSettings.Port);
                    var peerClient = new PeerClient(targetHost, container.Resolve<IEnumerable<IP2PMessageHandler>>());

                    var datagramEnvelope = new P2PMessageFactoryBase<PingResponse, P2PMessages>().GetMessageInDatagramEnvelope(
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
                }
            }
        }
    }
}
