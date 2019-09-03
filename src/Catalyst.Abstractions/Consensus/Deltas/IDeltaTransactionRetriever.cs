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
using Catalyst.Protocol.Transaction;

namespace Catalyst.Abstractions.Consensus.Deltas
{
    /// <summary>
    /// The service in charge of finding the transactions that should be included in a ledger update (aka Delta)
    /// for a given cycle.
    /// </summary>
    public interface IDeltaTransactionRetriever
    {
        /// <summary>
        /// A comparer used to order transactions, notably in the mempool, by descending order
        /// of priority. In Catalyst network, all delta producers need to agree on the set of
        /// transactions that should be included in the next ledger update. This interface should
        /// be used to order transactions and decide whether they should be included or not.
        /// </summary>
        ITransactionComparer TransactionComparer { get; }

        /// <summary>
        /// Use this method to retrieve the top <see cref="maxCount"/> transactions in order of
        /// priority. The priority is the order in which transactions should be included in the
        /// next candidate delta, and relies on the implementation of the <see cref="TransactionComparer"/> 
        /// </summary>
        /// <param name="maxCount">The maximum number of transactions to be returned by the method.</param>
        /// <returns>The top <see cref="maxCount"/> transactions in order of </returns>
        IList<TransactionBroadcast> GetMempoolTransactionsByPriority(int maxCount = int.MaxValue);
    }
}
