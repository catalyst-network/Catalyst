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
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Protocol.Transaction;
using Dawn;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.Deltas.Building
{
    internal sealed class TransactionRetrieverStep : IDeltaBuilderStep
    {
        private readonly ILogger _logger;
        private readonly IDeltaTransactionRetriever _retriever;
        private readonly IDeltaConfig _deltaConfig;
        private readonly ITransactionConfig _transactionConfig;

        public TransactionRetrieverStep(IDeltaTransactionRetriever retriever, IDeltaConfig deltaConfig, ITransactionConfig transactionConfig, ILogger logger)
        {
            Guard.Argument(retriever, nameof(retriever)).NotNull();
            Guard.Argument(deltaConfig, nameof(deltaConfig)).NotNull();
            Guard.Argument(transactionConfig, nameof(transactionConfig)).NotNull();
            Guard.Argument(logger, nameof(logger)).NotNull();

            _retriever = retriever;
            _deltaConfig = deltaConfig;
            _transactionConfig = transactionConfig;
            _logger = logger;
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

            List<PublicEntry> validTransactionsForDelta = new List<PublicEntry>();
            List<PublicEntry> rejectedTransactions = new List<PublicEntry>();

            int allTransactionsCount = allTransactions.Count;
            for (int i = 0; i < allTransactionsCount; i++)
            {
                PublicEntry currentItem = allTransactions[i];
                if (!IsTransactionOfAcceptedType(currentItem))
                {
                    rejectedTransactions.Add(currentItem);
                    continue;
                }

                validTransactionsForDelta.Add(currentItem);
            }

            validTransactionsForDelta.Sort(AveragePriceComparer.InstanceDesc);

            ulong totalLimit = 0UL;
            int allValidCount = validTransactionsForDelta.Count;
            int rejectedCountBeforeLimitChecks = rejectedTransactions.Count;
            for (int i = 0; i < allValidCount; i++)
            {
                PublicEntry currentItem = validTransactionsForDelta[i];
                ulong remainingLimit = _deltaConfig.DeltaGasLimit - totalLimit;
                if (remainingLimit < _transactionConfig.MinTransactionEntryGasLimit)
                {
                    for (int j = i; j < allValidCount; j++)
                    {
                        currentItem = validTransactionsForDelta[j];
                        rejectedTransactions.Add(currentItem);
                    }

                    break;
                }

                ulong currentItemGasLimit = currentItem.GasLimit;
                if (remainingLimit < currentItemGasLimit)
                {
                    rejectedTransactions.Add(currentItem);
                }
                else
                {
                    totalLimit += validTransactionsForDelta[i].GasLimit;
                }
            }

            for (int i = rejectedCountBeforeLimitChecks; i < rejectedTransactions.Count; i++)
            {
                validTransactionsForDelta.Remove(rejectedTransactions[i]);
            }

            _logger.Debug("Delta builder rejected the following transactions {rejectedTransactions}",
                rejectedTransactions);

            return validTransactionsForDelta;
        }
    }
}
