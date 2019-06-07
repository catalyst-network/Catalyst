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
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Gossip;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.IntegrationTests.P2P
{
    public sealed class PeerServiceIntegrationTest : SelfAwareTestBase, IDisposable
    {
        private readonly Guid _guid;
        private readonly ILogger _logger;
        private readonly IPeerIdentifier _pid;
        private readonly PingRequest _pingRequest;
        private readonly IConfigurationRoot _config;
        private readonly IUdpServerChannelFactory _udpServerServerChannelFactory;
        private readonly IUdpServerChannelFactory _udpClientChannelFactory;
        private readonly IPeerSettings _peerSettings;
        private readonly IKeySigner _keySigner;
        private readonly IPeerDiscovery _peerDiscovery;
        private readonly List<IP2PMessageHandler> _p2PMessageHandlers;
        private readonly ICorrelationManager _correlationManager;
        private readonly IGossipManager _gossipManager;
        private readonly IChannel _serverChannel;
        private readonly IChannel _clientChannel;
        private PeerService _peerService;

        public PeerServiceIntegrationTest(ITestOutputHelper output) : base(output)
        {
            _pid = PeerIdentifierHelper.GetPeerIdentifier("im_a_key");
            _guid = Guid.NewGuid();
            _logger = Substitute.For<ILogger>();
            _pingRequest = new PingRequest();

            _serverChannel = GetChannel($"Server:{CurrentTestName}");
            _udpServerServerChannelFactory = GetUdpChannelFactory(_serverChannel);

            _clientChannel = GetChannel($"Client:{CurrentTestName}");
            _udpClientChannelFactory = GetUdpChannelFactory(_clientChannel);

            _keySigner = Substitute.For<IKeySigner>();
            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            _peerSettings.Port.Returns(1234);

            _peerDiscovery = Substitute.For<IPeerDiscovery>();
            _p2PMessageHandlers = new List<IP2PMessageHandler>();
            _correlationManager = Substitute.For<ICorrelationManager>();
            _gossipManager = Substitute.For<IGossipManager>();
        }

        private IChannel GetChannel(string channelName)
        {
            var channelId = Substitute.For<IChannelId>();
            channelId.AsLongText().Returns(channelName);
            return new EmbeddedChannel(channelId);
        }

        public IUdpServerChannelFactory GetUdpChannelFactory(IChannel channel)
        {
            var udpChannelFactory = Substitute.For<IUdpServerChannelFactory>();
            var observableSocket =
                new ObservableSocket(Observable.Never<IChanneledMessage<ProtocolMessage>>(), channel);
            udpChannelFactory.BuildChannel().Returns(observableSocket);
            return udpChannelFactory;
        }

        [Fact]
        public async Task Can_receive_incoming_ping_responses()
        {
            var observer = new TestMessageHandler<PingResponse>(_logger);
            _p2PMessageHandlers.Add(observer);

            _peerService = new PeerService(_udpServerServerChannelFactory,
                _peerDiscovery,
                _p2PMessageHandlers, 
                _logger);

            var fakeContext = Substitute.For<IChannelHandlerContext>();
            fakeContext.Channel.Returns(_serverChannel);

            var protocolMessage = new PingResponse().ToAnySigned(_pid.PeerId, _guid);

            await _serverChannel.WriteAndFlushAsync(protocolMessage);

            await _peerService.MessageStream.WaitForEndOfDelayedStreamOnTaskPoolScheduler();
            
            observer.SubstituteObserver.Received().OnNext(Arg.Any<PingResponse>());
        }

        //[Fact]
        //[Trait(Traits.TestType, Traits.IntegrationTest)]
        //public async Task CanReceivePingRequests()
        //{
        //        var targetHost = new IPEndPoint(peerSettings.BindAddress, peerSettings.Port);

        //        var datagramEnvelope = new MessageFactory().GetDatagramMessage(new MessageDto(
        //                new PingRequest(),
        //                MessageTypes.Ask,
        //                new PeerIdentifier(ByteUtil.InitialiseEmptyByteArray(20), peerSettings.BindAddress,
        //                    peerSettings.Port),
        //                new PeerIdentifier(ByteUtil.InitialiseEmptyByteArray(20), peerSettings.BindAddress,
        //                    peerSettings.Port)
        //            ),
        //            Guid.NewGuid()
        //        );

        //        var peerClient = new PeerClient(_udpClientChannelFactory, targetHost);
        //        peerClient.SendMessage(datagramEnvelope);
        //        await peerService.MessageStream.WaitForItemsOnDelayedStreamOnTaskPoolScheduler();
                
        //        serverObserver.Received.LastOrDefault().Should().NotBeNull();
        //        serverObserver.Received.Last().Payload.TypeUrl.Should()
        //           .Be(PingRequest.Descriptor.ShortenedFullName());
        //        peerService.Dispose();
        //}

        //[Fact]
        //[Trait(Traits.TestType, Traits.IntegrationTest)]
        //public async Task CanReceiveNeighbourRequests()
        //{
        //    var peerSettings = new PeerSettings(_config);
        //    var targetHost = new IPEndPoint(peerSettings.BindAddress, peerSettings.Port);

        //    var datagramEnvelope = new MessageFactory().GetDatagramMessage(new MessageDto(
        //            new PeerNeighborsResponse(),
        //            MessageTypes.Tell,
        //            new PeerIdentifier(ByteUtil.InitialiseEmptyByteArray(20), peerSettings.BindAddress, peerSettings.Port),
        //            new PeerIdentifier(ByteUtil.InitialiseEmptyByteArray(20), peerSettings.BindAddress, peerSettings.Port)
        //        ),
        //        Guid.NewGuid()
        //    );

        //    var peerClient = new PeerClient(_udpClientChannelFactory, targetHost);
        //    peerClient.SendMessage(datagramEnvelope);
        //    await peerService.MessageStream.WaitForItemsOnDelayedStreamOnTaskPoolScheduler();

        //    serverObserver.Received.FirstOrDefault().Should().NotBeNull();
        //    serverObserver.Received.First().Payload.TypeUrl.Should().Be(PeerNeighborsResponse.Descriptor.ShortenedFullName());
        //    peerService.Dispose();
        //}

        public void Dispose()
        {
            _peerService?.Dispose();
        }
    }
}
