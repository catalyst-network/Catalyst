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
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Mempool;

using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.DAO.Transaction;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Dawn;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc />
    public class DeltaTransactionRetriever : IDeltaTransactionRetriever
    {
        private readonly IMempool<PublicEntryDao> _mempool;
        private readonly IMapperProvider _mapperProvider;

        /// <inheritdoc />
        public ITransactionComparer TransactionComparer { get; }

        public DeltaTransactionRetriever(IMempool<PublicEntryDao> mempool,
            IMapperProvider mapperProvider,
            ITransactionComparer transactionComparer)
        {
            _mempool = mempool;
            _mapperProvider = mapperProvider;
            TransactionComparer = transactionComparer;
        }

        /// <inheritdoc />
        public IList<PublicEntry> GetMempoolTransactionsByPriority(int maxCount = 2147483647)
        {
            Guard.Argument(maxCount, nameof(maxCount)).NotNegative().NotZero();

            var allTransactions = _mempool.Service.GetAll().Select(x => x.ToProtoBuff<PublicEntryDao, PublicEntry>(_mapperProvider));
            var mempoolPrioritised = allTransactions.OrderByDescending(t => t, TransactionComparer)
               .Take(maxCount).Select(t => t).ToList();

            return mempoolPrioritised;
        }
    }
}
