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
using Catalyst.Abstractions.Consensus;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Extensions;
using Catalyst.Core.Util;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Dawn;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Multiformats.Hash.Algorithms;
using Serilog;

namespace Catalyst.Core.Consensus.Deltas
{
    /// <inheritdoc />
    public class DeltaBuilder : IDeltaBuilder
    {
        private readonly IDeltaTransactionRetriever _transactionRetriever;
        private readonly IDeterministicRandomFactory _randomFactory;
        private readonly IMultihashAlgorithm _hashAlgorithm;
        private readonly IPeerIdentifier _producerUniqueId;
        private readonly IDeltaCache _deltaCache;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger _logger;

        public DeltaBuilder(IDeltaTransactionRetriever transactionRetriever,
            IDeterministicRandomFactory randomFactory,
            IMultihashAlgorithm hashAlgorithm,
            IPeerIdentifier producerUniqueId,
            IDeltaCache deltaCache, 
            IDateTimeProvider dateTimeProvider,
            ILogger logger)
        {
            _transactionRetriever = transactionRetriever;
            _randomFactory = randomFactory;
            _hashAlgorithm = hashAlgorithm;
            _producerUniqueId = producerUniqueId;
            _deltaCache = deltaCache;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        ///<inheritdoc />
        public CandidateDeltaBroadcast BuildCandidateDelta(byte[] previousDeltaHash)
        {
            _logger.Debug("Building candidate delta locally");

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
            var summedFees = includedTransactions
               .Sum(t => t.TransactionFees);

            //∆Ln,j = L(f/E) + dn + E(xf, j)
            var coinbaseEntry = new CoinbaseEntry
            {
                Amount = summedFees,
                PubKey = _producerUniqueId.PublicKey.ToByteString(),
                Version = 1
            };
            var globalLedgerStateUpdate = shuffledEntriesBytes
               .Concat(signaturesInOrder)
               .Concat(coinbaseEntry.ToByteArray())
               .ToArray();

            //hj
            var candidate = new CandidateDeltaBroadcast
            {
                // h∆j
                Hash = globalLedgerStateUpdate.ComputeMultihash(_hashAlgorithm).ToBytes().ToByteString(),

                // Idj
                ProducerId = _producerUniqueId.PeerId,
                PreviousDeltaDfsHash = previousDeltaHash.ToByteString()
            };

            _logger.Debug("Building full delta locally");

            var producedDelta = new Delta
            {
                PreviousDeltaDfsHash = previousDeltaHash.ToByteString(),
                MerkleRoot = candidate.Hash,
                CBEntries = {coinbaseEntry},
                STEntries = {includedTransactions.SelectMany(t => t.STEntries)},
                Version = 1,
                TimeStamp = Timestamp.FromDateTime(_dateTimeProvider.UtcNow)
            };

            _logger.Debug("Adding local candidate delta");

            _deltaCache.AddLocalDelta(candidate, producedDelta);

            return candidate;
        }

        private IEnumerable<byte> GetSaltFromPreviousDelta(byte[] previousDeltaHash)
        {
            var isaac = _randomFactory.GetDeterministicRandomFromSeed(previousDeltaHash);
            return BitConverter.GetBytes(isaac.NextInt());
        }

        private sealed class RawEntryWithSaltedAndHashedEntry
        {
            public STTransactionEntry RawEntry { get; }
            public byte[] SaltedAndHashedEntry { get; }

            public RawEntryWithSaltedAndHashedEntry(STTransactionEntry rawEntry, IEnumerable<byte> salt, IMultihashAlgorithm hashAlgorithm)
            {
                RawEntry = rawEntry;
                SaltedAndHashedEntry = rawEntry.ToByteArray().Concat(salt).ComputeRawHash(hashAlgorithm);
            }
        }

        /// <summary>
        /// Gets the valid transactions for delta.
        /// This method can be used to extract the collection of transactions that meet the criteria for validating delta.
        /// </summary>
        private IList<TransactionBroadcast> GetValidTransactionsForDelta(IList<TransactionBroadcast> allTransactions)
        {
            //lock time equals 0 or less than ledger cycle time
            //we assume all transactions are of type non-confidential for now

            var validTransactionsForDelta = allTransactions.Where(m => m.LockTime <= 0 && m.Version == 1).ToList();
            var rejectedTransactions = allTransactions.Except(validTransactionsForDelta);
            _logger.Debug("Delta builder rejected the following transactions {rejectedTransactions}", rejectedTransactions);
            return validTransactionsForDelta;
        }
    }
}
