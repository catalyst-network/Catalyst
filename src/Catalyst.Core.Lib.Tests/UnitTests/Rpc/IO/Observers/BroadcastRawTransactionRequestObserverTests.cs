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
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Core.Modules.Rpc.Server.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    public class BroadcastRawTransactionRequestObserverTests
    {
        private BroadcastRawTransactionRequestObserver _broadcastRawTransactionRequestObserver;
        private ITransactionReceivedEvent _transactionReceivedEvent;
        private ILibP2PPeerClient _peerClient;

        [SetUp]
        public void Init()
        {
            _peerClient = Substitute.For<ILibP2PPeerClient>();
            _transactionReceivedEvent = Substitute.For<ITransactionReceivedEvent>();

            var peerSettings = MultiAddressHelper.GetAddress("Test").ToSubstitutedPeerSettings();

            _broadcastRawTransactionRequestObserver = new BroadcastRawTransactionRequestObserver(
                Substitute.For<ILogger>(),
                peerSettings,
                _peerClient,
                _transactionReceivedEvent);
        }

        [Theory]
        [TestCase(ResponseCode.Pending)]
        [TestCase(ResponseCode.Error)]
        [TestCase(ResponseCode.Successful)]
        [TestCase(ResponseCode.Failed)]
        [TestCase(ResponseCode.Finished)]
        public void Can_Respond_With_Correct_Response(ResponseCode expectedResponse)
        {
            var channelContext = Substitute.For<IChannelHandlerContext>();
            var channel = Substitute.For<IChannel>();
            channelContext.Channel.Returns(channel);

            _transactionReceivedEvent.OnTransactionReceived(Arg.Any<ProtocolMessage>())
               .Returns(expectedResponse);
            _broadcastRawTransactionRequestObserver
               .OnNext(new ObserverDto(channelContext,
                    new BroadcastRawTransactionRequest { Transaction = new TransactionBroadcast() }.ToProtocolMessage(
                        MultiAddressHelper.GetAddress("FakeSender"))));
            _peerClient.Received(1).SendMessageAsync(Arg.Is<IMessageDto<ProtocolMessage>>(transactionObj =>
                    ((MessageDto) transactionObj)
                   .Content.FromProtocolMessage<BroadcastRawTransactionResponse>()
                   .ResponseCode == expectedResponse));
        }
    }
}
