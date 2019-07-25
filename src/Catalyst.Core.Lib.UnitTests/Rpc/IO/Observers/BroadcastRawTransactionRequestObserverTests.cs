using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Interfaces.P2P.IO.Messaging.Broadcast;
using Catalyst.Core.Lib.Rpc.IO.Observers;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Core.Lib.UnitTests.Rpc.IO.Observers
{
    public class BroadcastRawTransactionRequestObserverTests
    {
        private readonly BroadcastRawTransactionRequestObserver _broadcastRawTransactionRequestObserver;
        private readonly IMempool _mempool;

        public BroadcastRawTransactionRequestObserverTests()
        {
            var logger = Substitute.For<ILogger>();
            var fakePeerIdentifier = PeerIdentifierHelper.GetPeerIdentifier("test");
            _mempool = Substitute.For<IMempool>();
            var broadcastManager = Substitute.For<IBroadcastManager>();
            _broadcastRawTransactionRequestObserver = new BroadcastRawTransactionRequestObserver(
                logger, 
                fakePeerIdentifier,
                _mempool, 
                broadcastManager);
        }

        [Fact]
        public void Can_Send_Pending_Response_If_Mempool_Contains_Transaction()
        {
            _mempool.ContainsDocument(Arg.Any<TransactionSignature>()).Returns(true);
        }

    }
}
