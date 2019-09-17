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

using System.Net;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.KeySigner;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.IO.Handlers;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using Catalyst.TestUtils.Protocol;
using DotNetty.Transport.Channels.Embedded;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.P2P.IO.Messaging.Broadcast
{
    public sealed class BroadcastHandlerTests
    {
        private readonly IBroadcastManager _fakeBroadcastManager;
        private readonly BroadcastHandler _broadcastHandler;
        private readonly IKeySigner _keySigner;
        private readonly ProtocolMessage _broadcastMessageSigned;
        private readonly SigningContext _signingContext;

        public BroadcastHandlerTests()
        {
            _keySigner = Substitute.For<IKeySigner>();
            _keySigner.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), default).ReturnsForAnyArgs(true);
            _fakeBroadcastManager = Substitute.For<IBroadcastManager>();
            _broadcastHandler = new BroadcastHandler(_fakeBroadcastManager);

            var fakeSignature = Substitute.For<ISignature>();
            fakeSignature.SignatureBytes.Returns(ByteUtil.GenerateRandomByteArray(Ffi.SignatureLength));

            _signingContext = DevNetPeerSigningContext.Instance;

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test");
            var innerMessage = new TransactionBroadcast();
            _broadcastMessageSigned = innerMessage
               .ToSignedProtocolMessage(peerIdentifier.PeerId, fakeSignature, _signingContext)
               .ToSignedProtocolMessage(peerIdentifier.PeerId, fakeSignature, _signingContext);
        }

        [Fact]
        public async Task Broadcast_Handler_Can_Notify_Manager_On_Incoming_Broadcast()
        {
            var recipientIdentifier = Substitute.For<IPeerIdentifier>();
            var fakeIp = IPAddress.Any;

            recipientIdentifier.Ip.Returns(fakeIp);
            recipientIdentifier.IpEndPoint.Returns(new IPEndPoint(fakeIp, 10));
            
            EmbeddedChannel channel = new EmbeddedChannel(
                new ProtocolMessageVerifyHandler(_keySigner, _signingContext),
                _broadcastHandler,
                new ObservableServiceHandler()
            );

            channel.WriteInbound(_broadcastMessageSigned);

            await _fakeBroadcastManager.Received(Quantity.Exactly(1))
               .ReceiveAsync(Arg.Any<ProtocolMessage>());
        }

        [Fact]
        public void Broadcast_Can_Execute_Proto_Handler()
        {
            var testScheduler = new TestScheduler();
            var handler = new TestMessageObserver<TransactionBroadcast>(Substitute.For<ILogger>());

            var protoDatagramChannelHandler = new ObservableServiceHandler(testScheduler);
            handler.StartObserving(protoDatagramChannelHandler.MessageStream);

            var channel = new EmbeddedChannel(new ProtocolMessageVerifyHandler(_keySigner, _signingContext), _broadcastHandler, protoDatagramChannelHandler);
            channel.WriteInbound(_broadcastMessageSigned);

            testScheduler.Start();

            handler.SubstituteObserver.Received(1).OnNext(Arg.Any<TransactionBroadcast>());
        }
    }
}
