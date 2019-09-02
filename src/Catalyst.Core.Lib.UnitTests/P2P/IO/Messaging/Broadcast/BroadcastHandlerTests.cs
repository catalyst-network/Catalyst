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
using System.Net;
using System.Threading.Tasks;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.Keystore;
using Catalyst.Common.Interfaces.Modules.KeySigner;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.Cryptography.BulletProofs.Wrapper.Interfaces;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NSubstitute.Exceptions;
using NSubstitute.ReceivedExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.P2P.IO.Messaging.Broadcast
{
    public class BroadcastHandlerTests
    {
        private readonly IBroadcastManager _fakeBroadcastManager;
        private readonly BroadcastHandler _broadcastHandler;
        private readonly IKeySigner _keySigner;
        private readonly ProtocolMessageSigned _broadcastMessageSigned;
        private readonly ISigningContextProvider _signingContextProvider;

        public BroadcastHandlerTests()
        {
            ICryptoContext cryptoContext = new CryptoContext(new CryptoWrapper());
            _keySigner = Substitute.For<IKeySigner>();
            _keySigner.Verify(Arg.Any<ISignature>(), Arg.Any<byte[]>(), default).ReturnsForAnyArgs(true);
            _fakeBroadcastManager = Substitute.For<IBroadcastManager>();
            _broadcastHandler = new BroadcastHandler(_fakeBroadcastManager);

            var fakeSignature = Substitute.For<ISignature>();
            fakeSignature.SignatureBytes.Returns(ByteUtil.GenerateRandomByteArray(FFI.SignatureLength));

            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("Test");
            _broadcastMessageSigned =
                new ProtocolMessageSigned
                {
                    Message = new ProtocolMessageSigned
                    {
                        Message = new TransactionBroadcast().ToProtocolMessage(peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId()),
                        Signature = fakeSignature.SignatureBytes.ToByteString()
                    }.ToProtocolMessage(peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId()),
                    Signature = fakeSignature.SignatureBytes.ToByteString()
                };
            _signingContextProvider = Substitute.For<ISigningContextProvider>();
            _signingContextProvider.Network.Returns(Network.Devnet);
            _signingContextProvider.SignatureType.Returns(SignatureType.ProtocolPeer);
        }

        [Fact]
        public async Task Broadcast_Handler_Can_Notify_Manager_On_Incoming_Broadcast()
        {
            var recipientIdentifier = Substitute.For<IPeerIdentifier>();
            var fakeIp = IPAddress.Any;

            recipientIdentifier.Ip.Returns(fakeIp);
            recipientIdentifier.IpEndPoint.Returns(new IPEndPoint(fakeIp, 10));
            
            EmbeddedChannel channel = new EmbeddedChannel(
                new ProtocolMessageVerifyHandler(_keySigner, _signingContextProvider),
                _broadcastHandler,
                new ObservableServiceHandler()
            );

            channel.WriteInbound(_broadcastMessageSigned);

            await _fakeBroadcastManager.Received(Quantity.Exactly(1))
               .ReceiveAsync(Arg.Any<ProtocolMessageSigned>());
        }

        [Fact]
        public void Broadcast_Can_Execute_Proto_Handler()
        {
            var testScheduler = new TestScheduler();
            var handler = new TestMessageObserver<TransactionBroadcast>(Substitute.For<ILogger>());

            var protoDatagramChannelHandler = new ObservableServiceHandler(testScheduler);
            handler.StartObserving(protoDatagramChannelHandler.MessageStream);

            var channel = new EmbeddedChannel(new ProtocolMessageVerifyHandler(_keySigner, _signingContextProvider), _broadcastHandler, protoDatagramChannelHandler);
            channel.WriteInbound(_broadcastMessageSigned);

            testScheduler.Start();

            handler.SubstituteObserver.Received(1).OnNext(Arg.Any<TransactionBroadcast>());
        }
    }
}
