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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.IO.Handlers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels.Embedded;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Exceptions;
using NSubstitute.ReceivedExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.IO.Messaging.Broadcast
{
    public class BroadcastHandlerTests
    {
        private readonly IBroadcastManager _fakeBroadcastManager;
        private readonly BroadcastHandler _broadcastHandler;

        public BroadcastHandlerTests()
        {
            _fakeBroadcastManager = Substitute.For<IBroadcastManager>();
            _broadcastHandler = new BroadcastHandler(_fakeBroadcastManager);
        }

        [Fact]
        public async Task Broadcast_Handler_Can_Notify_Manager_On_Incoming_Broadcast()
        {
            var peerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("1");
            var recipientIdentifier = Substitute.For<IPeerIdentifier>();
            var fakeIp = IPAddress.Any;
            var guid = CorrelationId.GenerateCorrelationId();

            recipientIdentifier.Ip.Returns(fakeIp);
            recipientIdentifier.IpEndPoint.Returns(new IPEndPoint(fakeIp, 10));

            EmbeddedChannel channel = new EmbeddedChannel(
                _broadcastHandler,
                new ObservableServiceHandler()
            );

            var transaction = new TransactionBroadcast();
            var anySigned = transaction.ToProtocolMessage(peerIdentifier.PeerId, guid)
               .ToProtocolMessage(peerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());

            channel.WriteInbound(anySigned);

            await _fakeBroadcastManager.Received(Quantity.Exactly(1))
               .ReceiveAsync(Arg.Any<ProtocolMessage>());
        }

        [Fact]
        public async Task Broadcast_Can_Execute_Proto_Handler()
        {
            var handler = new TestMessageObserver<TransactionBroadcast>(Substitute.For<ILogger>());

            var protoDatagramChannelHandler = new ObservableServiceHandler();
            handler.StartObserving(protoDatagramChannelHandler.MessageStream);

            var channel = new EmbeddedChannel(_broadcastHandler, protoDatagramChannelHandler);

            var anySignedGossip = new TransactionBroadcast()
               .ToProtocolMessage(PeerIdHelper.GetPeerId("Sender"))
               .ToProtocolMessage(PeerIdHelper.GetPeerId("Sender"));

            channel.WriteInbound(anySignedGossip);

            var result = await TaskHelper.WaitForAsync(() =>
            {
                try
                {
                    handler.SubstituteObserver.Received(1).OnNext(Arg.Any<TransactionBroadcast>());
                    return true;
                }
                catch (ReceivedCallsException) { }

                return false;
            }, TimeSpan.FromSeconds(5));
            result.Should().BeTrue();
        }
    }
}
