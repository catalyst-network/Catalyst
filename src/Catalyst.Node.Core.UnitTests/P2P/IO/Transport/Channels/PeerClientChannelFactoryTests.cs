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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Correlation;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Node.Core.P2P.IO.Transport.Channels;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.IO.Transport.Channels
{
    public sealed class PeerClientChannelFactoryTests
    {
        public sealed class TestPeerClientChannelFactory : PeerClientChannelFactory
        {
            private readonly List<IChannelHandler> _handlers;

            public TestPeerClientChannelFactory(IKeySigner keySigner,
                IPeerMessageCorrelationManager correlationManager,
                IPeerIdValidator peerIdValidator)
                : base(keySigner, correlationManager, peerIdValidator)
            {
                _handlers = Handlers;
            }

            public IReadOnlyCollection<IChannelHandler> InheritedHandlers => _handlers;
        }

        private readonly IPeerMessageCorrelationManager _correlationManager;
        private readonly IBroadcastManager _gossipManager;
        private readonly IKeySigner _keySigner;
        private readonly TestPeerClientChannelFactory _factory;

        public PeerClientChannelFactoryTests()
        {
            _correlationManager = Substitute.For<IPeerMessageCorrelationManager>();
            _gossipManager = Substitute.For<IBroadcastManager>();
            _keySigner = Substitute.For<IKeySigner>();

            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.BindAddress.Returns(IPAddress.Parse("127.0.0.1"));
            peerSettings.Port.Returns(1234);

            var peerValidator = Substitute.For<IPeerIdValidator>();
            peerValidator.ValidatePeerIdFormat(Arg.Any<PeerId>()).Returns(true);

            _factory = new TestPeerClientChannelFactory(
                _keySigner,
                _correlationManager,
                peerValidator);
        }

        [Fact]
        public void PeerClientChannelFactory_should_have_correct_handlers()
        {
            _factory.InheritedHandlers.Count(h => h != null).Should().Be(5);
            var handlers = _factory.InheritedHandlers.ToArray();
            handlers[0].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[1].Should().BeOfType<PeerIdValidationHandler>();
            handlers[2].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[3].Should().BeOfType<CombinedChannelDuplexHandler<IChannelHandler, IChannelHandler>>();
            handlers[4].Should().BeOfType<ObservableServiceHandler>();
        }

        [Fact]
        public async Task PeerClientChannelFactory_should_put_the_correct_handlers_on_the_inbound_pipeline()
        {
            var testingChannel = new EmbeddedChannel("test".ToChannelId(),
                true, _factory.InheritedHandlers.ToArray());

            var senderId = PeerIdHelper.GetPeerId("sender");
            var correlationId = CorrelationId.GenerateCorrelationId();
            var protocolMessage = new PingRequest().ToProtocolMessage(senderId, correlationId);
            var signature = ByteUtil.GenerateRandomByteArray(FFI.GetSignatureLength());

            var signedMessage = new ProtocolMessageSigned
            {
                Message = protocolMessage,
                Signature = signature.ToByteString()
            };

            _keySigner.Verify(Arg.Is<ISignature>(s => s.SignatureBytes.RawBytes.SequenceEqual(signature)), Arg.Any<byte[]>())
               .Returns(true);

            var observer = new ProtocolMessageObserver(0, Substitute.For<ILogger>());
           
            var messageStream = ((ObservableServiceHandler) _factory.InheritedHandlers.Last()).MessageStream;
            
            using (messageStream.Subscribe(observer))
            {
                testingChannel.WriteInbound(signedMessage);
                _correlationManager.DidNotReceiveWithAnyArgs().TryMatchResponse(protocolMessage);
                await _gossipManager.DidNotReceiveWithAnyArgs().BroadcastAsync(null);
                _keySigner.ReceivedWithAnyArgs(1).Verify(null, null);

                await messageStream.WaitForItemsOnDelayedStreamOnTaskPoolSchedulerAsync();

                observer.Received.Count.Should().Be(1);
                observer.Received.Single().Payload.CorrelationId.ToCorrelationId().Id.Should().Be(correlationId.Id);
            }
        }
    }
}
