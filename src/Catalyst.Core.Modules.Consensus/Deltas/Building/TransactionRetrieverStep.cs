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
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Protocol.Transaction;
using Dawn;
using Nethermind.Core;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    internal sealed class TransactionRetrieverStep : IDeltaBuilderStep
    {
        [Todo(Improve.Refactor, "Introduce configuration for delta / voting for size")]
        private const ulong DeltaGasLimit = 8_000_000;

        [Todo(Improve.Refactor, "Introduce configuration for tx cost")]
        private const ulong MinTransactionEntryGasLimit = 21_000;

        private readonly ILogger _logger;
        private readonly IDeltaTransactionRetriever _retriever;

        public TransactionRetrieverStep(IDeltaTransactionRetriever retriever, ILogger logger)
        {
            _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Execute(DeltaBuilderContext context)
        {
            IList<PublicEntry> allTransactions = _retriever.GetMempoolTransactionsByPriority();

            Guard.Argument(allTransactions, nameof(allTransactions))
               .NotNull("Mempool content returned null, check the mempool is actively running");

            IList<PublicEntry> includedTransactions = GetValidTransactionsForDelta(allTransactions);
            context.Transactions = includedTransactions;
        }

        private static bool IsTransactionOfAcceptedType(PublicEntry transaction) { return transaction.IsPublicTransaction || transaction.IsContractCall || transaction.IsContractDeployment; }

        /// <summary>
        ///     Gets the valid transactions for delta.
        ///     This method can be used to extract the collection of transactions that meet the criteria for validating delta.
        /// </summary>
        private IList<PublicEntry> GetValidTransactionsForDelta(IList<PublicEntry> allTransactions)
        {
            //lock time equals 0 or less than ledger cycle time
            //we assume all transactions are of type non-confidential for now

            var validTransactionsForDelta = new List<PublicEntry>();
            var rejectedTransactions = new List<PublicEntry>();

            var allTransactionsCount = allTransactions.Count;
            for (var i = 0; i < allTransactionsCount; i++)
            {
                var currentItem = allTransactions[i];
                if (!IsTransactionOfAcceptedType(currentItem))
                {
                    rejectedTransactions.Add(currentItem);
                    continue;
                }

                validTransactionsForDelta.Add(currentItem);
            }

            validTransactionsForDelta.Sort(AveragePriceComparer.InstanceDesc);

            var totalLimit = 0UL;
            var allValidCount = validTransactionsForDelta.Count;
            var rejectedCountBeforeLimitChecks = rejectedTransactions.Count;
            for (var i = 0; i < allValidCount; i++)
            {
                var currentItem = validTransactionsForDelta[i];
                var remainingLimit = DeltaGasLimit - totalLimit;
                if (remainingLimit < MinTransactionEntryGasLimit)
                {
                    for (var j = i; j < allValidCount; j++)
                    {
                        currentItem = validTransactionsForDelta[j];
                        rejectedTransactions.Add(currentItem);
                    }

                    break;
                }

                var currentItemGasLimit = currentItem.GasLimit;
                if (remainingLimit < currentItemGasLimit)
                {
                    rejectedTransactions.Add(currentItem);
                }
                else
                {
                    totalLimit += validTransactionsForDelta[i].GasLimit;
                }
            }

            for (var i = rejectedCountBeforeLimitChecks; i < rejectedTransactions.Count; i++)
            {
                validTransactionsForDelta.Remove(rejectedTransactions[i]);
            }

            _logger.Debug("Delta builder rejected the following transactions {rejectedTransactions}",
                rejectedTransactions);

            return validTransactionsForDelta;
        }
    }
}
