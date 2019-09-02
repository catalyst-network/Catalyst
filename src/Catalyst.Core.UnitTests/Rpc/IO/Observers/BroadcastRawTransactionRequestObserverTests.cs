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

using Catalyst.Abstractions.IO.Events;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Dto;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc.IO.Observers
{
    public class BroadcastRawTransactionRequestObserverTests
    {
        private readonly BroadcastRawTransactionRequestObserver _broadcastRawTransactionRequestObserver;
        private readonly ITransactionReceivedEvent _transactionReceivedEvent;

        public BroadcastRawTransactionRequestObserverTests()
        {
            _transactionReceivedEvent = Substitute.For<ITransactionReceivedEvent>();

            _broadcastRawTransactionRequestObserver = new BroadcastRawTransactionRequestObserver(
                Substitute.For<ILogger>(),
                PeerIdentifierHelper.GetPeerIdentifier("Test"),
                _transactionReceivedEvent);
        }

        [Theory]
        [InlineData(ResponseCode.Pending)]
        [InlineData(ResponseCode.Error)]
        [InlineData(ResponseCode.Successful)]
        [InlineData(ResponseCode.Failed)]
        [InlineData(ResponseCode.Finished)]
        public void Can_Respond_With_Correct_Response(ResponseCode expectedResponse)
        {
            var channelContext = Substitute.For<IChannelHandlerContext>();
            var channel = Substitute.For<IChannel>();
            channelContext.Channel.Returns(channel);

            _transactionReceivedEvent.OnTransactionReceived(Arg.Any<TransactionBroadcast>())
               .Returns(expectedResponse);
            _broadcastRawTransactionRequestObserver
               .OnNext(new ObserverDto(channelContext,
                    new TransactionBroadcast().ToProtocolMessage(PeerIdHelper.GetPeerId("FakeSender"))));
            channelContext.Channel.Received(1).WriteAndFlushAsync(
                Arg.Is<object>(transactionObj =>
                    ((MessageDto) transactionObj)
                   .Content.FromProtocolMessage<BroadcastRawTransactionResponse>()
                   .ResponseCode == expectedResponse));
        }
    }
}
