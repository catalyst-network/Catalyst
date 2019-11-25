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
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Mempool.Models;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.DAO;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Dawn;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using LibP2P;
using Nethermind.Dirichlet.Numerics;
using Serilog;
using TheDotNetLeague.MultiFormats.MultiBase;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc />
    public sealed class DeltaBuilder : IDeltaBuilder
    {
        private const ulong DeltaGasLimit = 8_000_000;
        private const ulong MinTransactionEntryGasLimit = 21_000;
        
        private readonly IDeltaTransactionRetriever _transactionRetriever;
        private readonly IDeterministicRandomFactory _randomFactory;
        private readonly IHashProvider _hashProvider;
        private readonly PeerId _producerUniqueId;
        private readonly IDeltaCache _deltaCache;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger _logger;

        public DeltaBuilder(IDeltaTransactionRetriever transactionRetriever,
            IDeterministicRandomFactory randomFactory,
            IHashProvider hashProvider,
            IPeerSettings peerSettings,
            IDeltaCache deltaCache,
            IDateTimeProvider dateTimeProvider,
            ILogger logger)
        {
            _transactionRetriever = transactionRetriever;
            _randomFactory = randomFactory;
            _hashProvider = hashProvider;
            _producerUniqueId = peerSettings.PeerId;
            _deltaCache = deltaCache;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        ///<inheritdoc />
        public CandidateDeltaBroadcast BuildCandidateDelta(Cid previousDeltaHash)
        {
            _logger.Debug("Building candidate delta locally");

            var allTransactions = _transactionRetriever.GetMempoolTransactionsByPriority();

            Guard.Argument(allTransactions, nameof(allTransactions))
               .NotNull("Mempool content returned null, check the mempool is actively running");

            var includedTransactions = GetValidTransactionsForDelta(allTransactions);
            var salt = GetSaltFromPreviousDelta(previousDeltaHash);

            var rawAndSaltedEntriesBySignature = includedTransactions.Select(
                x => new RawEntryWithSaltedAndHashedEntry(x, salt, _hashProvider));

            //var rawAndSaltedEntriesBySignature = includedTransactions.SelectMany(
            //   t => t.PublicEntries.Select(e =>
            //       new RawEntryWithSaltedAndHashedEntry(e, salt, _hashProvider)));

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
            var summedFees = includedTransactions.Sum(t => t.SummedEntryFees());

            //∆Ln,j = L(f/E) + dn + E(xf, j)
            var coinbaseEntry = new CoinbaseEntry
            {
                Amount = summedFees.ToUint256ByteString(),
                ReceiverPublicKey = _producerUniqueId.PublicKey.ToByteString()
            };
            var globalLedgerStateUpdate = shuffledEntriesBytes
               .Concat(signaturesInOrder)
               .Concat(coinbaseEntry.ToByteArray())
               .ToArray();

            //hj
            var candidate = new CandidateDeltaBroadcast
            {
                // h∆j
                Hash = MultiBase.Decode(CidHelper.CreateCid(_hashProvider.ComputeMultiHash(globalLedgerStateUpdate)))
                   .ToByteString(),

                // Idj
                ProducerId = _producerUniqueId,
                PreviousDeltaDfsHash = previousDeltaHash.ToArray().ToByteString()
            };

            _logger.Debug("Building full delta locally");

            var publicEntries = includedTransactions.Select(x => x.ToProtoBuff<MempoolItem, TransactionBroadcast>());

            var producedDelta = new Delta
            {
                PreviousDeltaDfsHash = previousDeltaHash.ToArray().ToByteString(),
                MerkleRoot = candidate.Hash,
                CoinbaseEntries = {coinbaseEntry},
                PublicEntries = {includedTransactions.SelectMany(t => t.PublicEntries).Select(x => x)},
                TimeStamp = Timestamp.FromDateTime(_dateTimeProvider.UtcNow)
            };

            _logger.Debug("Adding local candidate delta");

            _deltaCache.AddLocalDelta(candidate, producedDelta);

            return candidate;
        }

        private IEnumerable<byte> GetSaltFromPreviousDelta(Cid previousDeltaHash)
        {
            var isaac = _randomFactory.GetDeterministicRandomFromSeed(previousDeltaHash.ToArray());
            return BitConverter.GetBytes(isaac.NextInt());
        }

        private sealed class RawEntryWithSaltedAndHashedEntry
        {
            public PublicEntry RawEntry { get; }

            public byte[] SaltedAndHashedEntry { get; }

            public RawEntryWithSaltedAndHashedEntry(PublicEntry rawEntry,
                IEnumerable<byte> salt,
                IHashProvider hashProvider)
            {
                RawEntry = rawEntry;
                SaltedAndHashedEntry = hashProvider.ComputeMultiHash(rawEntry.ToByteArray().Concat(salt)).ToArray();
            }
        }

        private sealed class AveragePriceComparer : IComparer<TransactionBroadcast>
        {
            private readonly int _multiplier;

            private AveragePriceComparer(int multiplier) { _multiplier = multiplier; }
            
            public int Compare(TransactionBroadcast x, TransactionBroadcast y)
            {
                return _multiplier * Comparer<UInt256?>.Default.Compare(x?.AverageGasPrice, y?.AverageGasPrice);
            }
            
            public static AveragePriceComparer InstanceDesc { get; } = new AveragePriceComparer(-1);
            public static AveragePriceComparer InstanceAsc { get; } = new AveragePriceComparer(1);
        }
        
        //private static bool IsTransactionOfAcceptedType(MempoolItem transaction) { return transaction.IsPublicTransaction || transaction.IsContractCall || transaction.IsContractDeployment; }
        
        /// <summary>
        ///     Gets the valid transactions for delta.
        ///     This method can be used to extract the collection of transactions that meet the criteria for validating delta.
        /// </summary>
        private IList<MempoolItem> GetValidTransactionsForDelta(IList<MempoolItem> allTransactions)
        {
            //lock time equals 0 or less than ledger cycle time
            //we assume all transactions are of type non-confidential for now

            var validTransactionsForDelta = new List<MempoolItem>();
            var rejectedTransactions = new List<MempoolItem>();

            var allTransactionsCount = allTransactions.Count;
            for (var i = 0; i < allTransactionsCount; i++)
            {
                var currentItem = allTransactions[i];
                //if (!IsTransactionOfAcceptedType(currentItem) || !currentItem.HasValidEntries())
                //{
                //    rejectedTransactions.Add(currentItem);
                //    continue;
                //}

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
                
                var currentItemGasLimit = currentItem.TotalGasLimit;
                if (remainingLimit < currentItemGasLimit)
                {
                    rejectedTransactions.Add(currentItem);
                }
                else
                {
                    totalLimit += validTransactionsForDelta[i].TotalGasLimit;    
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
