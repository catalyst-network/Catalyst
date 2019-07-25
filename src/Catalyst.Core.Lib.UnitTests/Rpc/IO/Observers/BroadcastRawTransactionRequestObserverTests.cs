using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Core.Lib.Rpc.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.Rpc.IO.Observers
{
    public class BroadcastRawTransactionRequestObserverTests
    {
        private readonly BroadcastRawTransactionRequestObserver _broadcastRawTransactionRequestObserver;
        private readonly IMempool _mempool;
        private readonly IPeerIdentifier _fakePeerIdentifier;
        private readonly IChannelHandlerContext _fakeContext;

        public BroadcastRawTransactionRequestObserverTests()
        {
            var logger = Substitute.For<ILogger>();
            _fakePeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("test");
            _mempool = Substitute.For<IMempool>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            var broadcastManager = Substitute.For<IBroadcastManager>();
            _broadcastRawTransactionRequestObserver = new BroadcastRawTransactionRequestObserver(
                logger,
                _fakePeerIdentifier,
                _mempool,
                broadcastManager);
        }

        [Fact]
        public void Can_Send_Pending_Response_If_Mempool_Contains_Transaction()
        {
            SendTransactionToHandler(true);
            _mempool.DidNotReceiveWithAnyArgs().SaveMempoolDocument(Arg.Any<IMempoolDocument>());
        }

        [Fact]
        public void Can_Add_Transaction_To_Mempool()
        {
            SendTransactionToHandler(false);
            _mempool.ReceivedWithAnyArgs(1).SaveMempoolDocument(Arg.Any<IMempoolDocument>());
        }

        private void SendTransactionToHandler(bool mempoolContainsTransaction)
        {
            _mempool.ContainsDocument(Arg.Any<TransactionSignature>()).Returns(mempoolContainsTransaction);

            var transactionBroadcast = GetTransactionBroadcastMessage();
            transactionBroadcast.SendToHandler(_fakeContext, _broadcastRawTransactionRequestObserver);

            _fakeContext.Channel.Received().WriteAndFlushAsync(
                Arg.Is<DefaultAddressedEnvelope<ProtocolMessage>>(response =>
                    response.Content.FromProtocolMessage<BroadcastRawTransactionResponse>().ResponseCode == ResponseCode.Pending));
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
