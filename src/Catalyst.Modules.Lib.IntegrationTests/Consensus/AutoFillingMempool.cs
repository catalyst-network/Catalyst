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
using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.Modules.Mempool.Models;
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
                var tx = TransactionHelper.GetTransaction(timeStamp: (long) utcNow.ToOADate());
                var transactionBroadcasts = Enumerable.Repeat(new MempoolDocument {Transaction = tx}, tenSecondSlot);

                return transactionBroadcasts;
            });

            return _fakeMempool.GetMemPoolContent();
        }

        public bool ContainsDocument(TransactionSignature key) { throw new NotImplementedException(); }

        public List<byte[]> GetMemPoolContentEncoded() { return _fakeMempool.GetMemPoolContentEncoded(); }
        public bool SaveMempoolDocument(IMempoolDocument mempoolDocument) { throw new NotImplementedException(); }
        public IMempoolDocument GetMempoolDocument(TransactionSignature key) { throw new NotImplementedException(); }
        public void Delete(params string[] transactionSignatures) { _fakeMempool.Delete(transactionSignatures); }
    }
}
