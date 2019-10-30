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

using System.Linq;
using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Events;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Wire;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Events
{
    public sealed class TransactionReceivedEventTests
    {
        private readonly IMempool<TransactionBroadcastDao> _mempool;
        private readonly ITransactionValidator _transactionValidator;
        private readonly IBroadcastManager _broadcastManager;
        private readonly TransactionReceivedEvent _transactionReceivedEvent;

        public TransactionReceivedEventTests()
        {
            var mapperProvider = new TestMapperProvider();

            _mempool = Substitute.For<IMempool<TransactionBroadcastDao>>();
            _transactionValidator = Substitute.For<ITransactionValidator>();
            _broadcastManager = Substitute.For<IBroadcastManager>();
            _transactionReceivedEvent = new TransactionReceivedEvent(_transactionValidator,
                _mempool,
                _broadcastManager,
                mapperProvider,
                Substitute.For<ILogger>());
        }

        [Fact]
        public void Can_Send_Error_To_Invalid_Transaction()
        {
            _transactionValidator.ValidateTransaction(Arg.Any<TransactionBroadcast>())
               .Returns(false);
            _transactionReceivedEvent.OnTransactionReceived(new TransactionBroadcast()
                   .ToProtocolMessage(PeerIdHelper.GetPeerId(), CorrelationId.GenerateCorrelationId())).Should()
               .Be(ResponseCode.Error);
            _broadcastManager.DidNotReceiveWithAnyArgs().BroadcastAsync(default);
        }

        [Fact]
        public void Can_Send_Error_If_Mempool_Contains_Transaction()
        {
            var transaction = TransactionHelper.GetPublicTransaction();

            _transactionValidator.ValidateTransaction(Arg.Any<TransactionBroadcast>())
               .Returns(true);

            _mempool.Repository.TryReadItem(Arg.Any<string>()).Returns(true);

            _transactionReceivedEvent
               .OnTransactionReceived(transaction.ToProtocolMessage(PeerIdHelper.GetPeerId(),
                    CorrelationId.GenerateCorrelationId()))
               .Should().Be(ResponseCode.Error);
            _broadcastManager.DidNotReceiveWithAnyArgs().BroadcastAsync(default);
            _mempool.Repository.DidNotReceiveWithAnyArgs().CreateItem(default);
        }

        [Fact]
        public void Can_Broadcast_And_Save_Valid_Transaction()
        {
            var transaction = TransactionHelper.GetPublicTransaction();

            _transactionValidator.ValidateTransaction(Arg.Any<TransactionBroadcast>())
               .Returns(true);
            _transactionReceivedEvent
               .OnTransactionReceived(transaction.ToProtocolMessage(PeerIdHelper.GetPeerId(),
                    CorrelationId.GenerateCorrelationId()))
               .Should().Be(ResponseCode.Successful);

            _mempool.Repository.Received(1).CreateItem(Arg.Any<TransactionBroadcastDao>());
            _broadcastManager.Received(1).BroadcastAsync(Arg.Is<ProtocolMessage>(
                broadcastedMessage => broadcastedMessage.Value.ToByteArray().SequenceEqual(transaction.ToByteArray())));
        }
    }
}
