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

using Catalyst.Abstractions.Mempool;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.Mempool.Documents;
using Catalyst.Core.Mempool.Models;
using Catalyst.Core.Rpc.IO.Observers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Rpc.IO.Observers
{
    public class BroadcastRawTransactionRequestObserverTests
    {
        private readonly BroadcastRawTransactionRequestObserver _broadcastRawTransactionRequestObserver;
        private readonly IMempool<MempoolDocument> _mempool;
        private readonly IPeerIdentifier _fakePeerIdentifier;
        private readonly IChannelHandlerContext _fakeContext;

        public BroadcastRawTransactionRequestObserverTests()
        {
            var logger = Substitute.For<ILogger>();
            _fakePeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("test");
            _mempool = Substitute.For<IMempool<MempoolDocument>>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var broadcastManager = Substitute.For<IBroadcastManager>();
            _broadcastRawTransactionRequestObserver = new BroadcastRawTransactionRequestObserver(
                logger,
                _fakePeerIdentifier,
                _mempool,
                broadcastManager);
        }

        [Fact]
        public void Can_Send_Success_Response_If_Mempool_Contains_Transaction()
        {
            SendTransactionToHandler(true);
            _mempool.Repository.DidNotReceiveWithAnyArgs().CreateItem(default);
        }

        [Fact]
        public void Can_Add_Transaction_To_Mempool()
        {
            SendTransactionToHandler(false);
            _mempool.Repository.ReceivedWithAnyArgs(1).CreateItem(Arg.Any<TransactionBroadcast>());
        }

        private void SendTransactionToHandler(bool mempoolContainsTransaction)
        {
            _mempool.Repository.TryReadItem(Arg.Any<TransactionSignature>()).Returns(mempoolContainsTransaction);

            var transactionBroadcast = GetTransactionBroadcastMessage();
            transactionBroadcast.SendToHandler(_fakeContext, _broadcastRawTransactionRequestObserver);
        }

        private ProtocolMessage GetTransactionBroadcastMessage()
        {
            return new BroadcastRawTransactionRequest
            {
                Transaction = new TransactionBroadcast
                {
                    Signature = new TransactionSignature
                    {
                        SchnorrSignature = ByteString.CopyFromUtf8("Test1"),
                        SchnorrComponent = ByteString.CopyFromUtf8("Test2")
                    }
                }
            }.ToProtocolMessage(_fakePeerIdentifier.PeerId, CorrelationId.GenerateCorrelationId());
        }
    }
}
