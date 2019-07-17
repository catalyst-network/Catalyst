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

using System.Collections.Generic;
using System.Linq;
using Catalyst.Common.Interfaces.Modules.Consensus;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Protocol.Transaction;
using Dawn;

namespace Catalyst.Node.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc />
    public class DeltaTransactionRetriever : IDeltaTransactionRetriever
    {
        private readonly IMempool _mempool;
        
        /// <inheritdoc />
        public ITransactionComparer TransactionComparer { get; }

        public DeltaTransactionRetriever(IMempool mempool,
            ITransactionComparer transactionComparer)
        {
            _mempool = mempool;
            TransactionComparer = transactionComparer;
        }

        /// <inheritdoc />
        public IList<TransactionBroadcast> GetMempoolTransactionsByPriority(int maxCount = 2147483647)
        {
            Guard.Argument(maxCount, nameof(maxCount)).NotNegative().NotZero();

            var allTransactions = _mempool.GetMemPoolContent();
            var mempoolPrioritised = allTransactions.OrderByDescending(t => t.Transaction, TransactionComparer)
               .Take(maxCount).Select(t => t.Transaction).ToList();

            return mempoolPrioritised;
        }
    }
}
