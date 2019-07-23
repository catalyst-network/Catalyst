using System;
using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Modules.Mempool;
using Catalyst.Protocol.Transaction;
using Catalyst.TestUtils;
using NSubstitute;

namespace Catalyst.Modules.Lib.IntegrationTests.Consensus
{
    /// <summary>
    /// This is a mempool to be used only for integration testing. It is a minimal implementation
    /// mostly relying on a Substitute, except that it will return changing content for the
    /// <see cref="GetMemPoolContent" /> method.
    /// </summary>
    public class AutoFillingMempool : IMempool
    {
        private readonly IMempool _fakeMempool = Substitute.For<IMempool>();

        public IEnumerable<IMempoolDocument> GetMemPoolContent()
        {
            _fakeMempool.GetMemPoolContent().Returns(_ =>
            {
                var utcNow = DateTime.UtcNow;  
                var tenSecondSlot = 1 + utcNow.Second / 10;
                var tx = TransactionHelper.GetTransaction(timeStamp: (ulong) utcNow.ToOADate());
                var transactionBroadcasts = Enumerable.Repeat(new MempoolDocument {Transaction = tx}, tenSecondSlot);

                return transactionBroadcasts;
            });

            return _fakeMempool.GetMemPoolContent();
        }

        public List<byte[]> GetMemPoolContentEncoded() { return _fakeMempool.GetMemPoolContentEncoded(); }
        public bool SaveMempoolDocument(IMempoolDocument mempoolDocument) { throw new NotImplementedException(); }
        public IMempoolDocument GetMempoolDocument(TransactionSignature key) { throw new NotImplementedException(); }
    }
}
