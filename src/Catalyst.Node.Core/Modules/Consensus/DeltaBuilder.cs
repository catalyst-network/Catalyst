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
using Catalyst.Protocol.Transaction;
using Multiformats.Hash.Algorithms;
using Catalyst.Common.Extensions;
using Catalyst.Common.Util;
using Google.Protobuf;
using Catalyst.Common.Interfaces.P2P;
using System;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Protocol.Delta;
using Dawn;

namespace Catalyst.Node.Core.Modules.Consensus
{
    /// <inheritdoc />
    public class DeltaBuilder : IDeltaBuilder
    {
        private readonly IDeltaTransactionRetriever _transactionRetriever;
        private readonly IDeterministicRandomFactory _randomFactory;
        private readonly IMultihashAlgorithm _hashAlgorithm;
        private readonly IPeerIdentifier _producerUniqueId;
        
        public DeltaBuilder(IDeltaTransactionRetriever transactionRetriever,
            IDeterministicRandomFactory randomFactory,
            IMultihashAlgorithm hashAlgorithm,
            IPeerIdentifier producerUniqueId)
        {
            _transactionRetriever = transactionRetriever;
            _randomFactory = randomFactory;
            _hashAlgorithm = hashAlgorithm;
            _producerUniqueId = producerUniqueId;
        }

        ///<inheritdoc />
        public CandidateDelta BuildCandidateDelta(byte[] previousDeltaHash)
        {
            var allTransactions = _transactionRetriever.GetMempoolTransactionsByPriority();

            Guard.Argument(allTransactions, nameof(allTransactions))
               .NotNull("Mempool content returned null, check the mempool is actively running");

            var includedTransactions = GetValidTransactionsForDelta(allTransactions);
            var salt = GetSaltFromPreviousDelta(previousDeltaHash);

            var rawAndSaltedEntriesBySignature = includedTransactions.SelectMany(
                t => t.STEntries.Select(e => new RawEntryWithSaltedAndHashedEntry(e, salt, _hashAlgorithm)));

            // (Eα;Oα)
            var shuffledEntriesBytes = rawAndSaltedEntriesBySignature
               .OrderBy(v => v.SaltedAndHashedEntry, ByteUtil.ByteListComparer.Default)
               .SelectMany(v => v.RawEntry.ToByteArray())
               .ToArray();

            // dn
            var signaturesInOrder = includedTransactions
               .Select(p => p.Signature.ToByteArray())
               .OrderBy(s => s, ByteUtil.ByteListComparer.Default)
               .SelectMany(b => b)
               .ToArray();

            // xf
            var summedFees = (ulong) includedTransactions
               .Sum(t => t.TransactionFees);

            //∆Ln,j = L(f/E) + dn + E(xf, j)
            var globalLedgerStateUpdate = shuffledEntriesBytes
               .Concat(signaturesInOrder)
               .Concat(new CoinbaseEntry()
                {
                    Amount = summedFees,
                    PubKey = _producerUniqueId.PublicKey.ToByteString(),
                    Version = 0
                }.ToByteArray())
               .ToArray();

            //hj
            var candidate = new CandidateDelta
            {
                // h∆j
                Hash = _hashAlgorithm.ComputeHash(globalLedgerStateUpdate).ToByteString(),

                // Idj
                ProducerId = _producerUniqueId.PeerId,
                PreviousDeltaDfsHash = previousDeltaHash.ToByteString()
            };

            return candidate;
        }

        private byte[] GetSaltFromPreviousDelta(byte[] previousDeltaHash)
        {
            var isaac = _randomFactory.GetDeterministicRandomFromSeed(previousDeltaHash);
            return BitConverter.GetBytes(isaac.NextInt());
        }

        private class RawEntryWithSaltedAndHashedEntry
        {
            public STTransactionEntry RawEntry { get; }
            public byte[] SaltedAndHashedEntry { get; }

            public RawEntryWithSaltedAndHashedEntry(STTransactionEntry rawEntry, byte[] salt, IMultihashAlgorithm hashAlgorithm)
            {
                RawEntry = rawEntry;
                SaltedAndHashedEntry = hashAlgorithm
                   .ComputeHash(rawEntry.ToByteArray().Concat(salt).ToArray());
            }
        }

        /// <summary>
        /// Gets the valid transactions for delta.
        /// This method can be used to extract the collection of transactions that meet the criteria for validating delta.
        /// </summary>
        /// <param name="allTransactions">All transactions.</param>
        /// <returns></returns>
        public static IList<Transaction> GetValidTransactionsForDelta(IEnumerable<Transaction> allTransactions)
        {
            //lock time equals 0 or less than ledger cycle time
            //we assume all transactions are of type non-confidential for now
            return allTransactions.Where(m => m.LockTime <= 0 && m.Version == 1).ToList();
        }
    }
}
