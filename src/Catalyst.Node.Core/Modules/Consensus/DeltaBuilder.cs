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
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Protocol.Transaction;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using Nethereum.Hex.HexConvertors.Extensions;
using Catalyst.Common.Extensions;
using Catalyst.Common.Util;
using Google.Protobuf;
using Catalyst.Common.Interfaces.P2P;
using System;
using Nethereum.RLP;
using Catalyst.Common.Config;
using Dawn;

namespace Catalyst.Node.Core.Modules.Consensus
{
    /// <inheritdoc />
    public class DeltaBuilder : IDeltaBuilder
    {
        private readonly IMempool _mempool;

        private readonly IPeerIdentifier _producerUniqueId;

        private readonly byte[] _previousValidLedgerStateUpdate;

        public DeltaBuilder(IMempool mempool, IPeerIdentifier producerUniqueId, byte[] previousValidLedgerStateUpdate)
        {
            Guard.Argument(mempool, nameof(mempool)).NotNull();
            Guard.Argument(producerUniqueId, nameof(producerUniqueId)).NotNull();


            _mempool = mempool;
            _producerUniqueId = producerUniqueId;
            _previousValidLedgerStateUpdate = previousValidLedgerStateUpdate;
        }

        ///<inheritdoc />
        public IDeltaEntity BuildDelta()
        {
            var allTransactions = _mempool.GetMemPoolContent();
            Guard.Argument(allTransactions, nameof(allTransactions)).NotNull();

            var allValidatedTransactions = ValidityCheck(allTransactions);
            Guard.Argument(allValidatedTransactions, nameof(allValidatedTransactions)).NotNull();

            if (allValidatedTransactions.Any())
            {
                var transactionSignature = allValidatedTransactions.FirstOrDefault().Signature;
                var selectedSTEntries = allValidatedTransactions.SelectMany(ste => ste.STEntries).ToList();

                var transationEntryListLexiOrder = SortHashByLexiOrder(selectedSTEntries);

                return CreateDeltaEntity(transationEntryListLexiOrder, transactionSignature);
            }
            return null;
        }

        private DeltaEntity CreateDeltaEntity(List<STTransactionEntry> transationEntryListLexiOrder, TransactionSignature transactionSignature)
        {
            var transationEntryListByteArray = transationEntryListLexiOrder.SelectMany(lo => lo.ToByteArray()).ToArray();

            var deltaState = ByteUtil.CombineByteArrays(transationEntryListByteArray, transactionSignature.ToByteArray());

            var localHash = CreateLocalHash(transationEntryListByteArray, transactionSignature.ToByteArray());

            var localLedgerStateUpdate = ByteUtil.CombineByteArrays(localHash, _producerUniqueId.PeerId.ToByteArray());

            return new DeltaEntity() { LocalLedgerState = localLedgerStateUpdate, Delta = deltaState, DeltaHash = localHash };
        }

        private List<STTransactionEntry> SortHashByLexiOrder(List<STTransactionEntry> selectedSTEntries)
        {
            var transationEntryHashPairList = new SortedDictionary<byte[], STTransactionEntry>();

            selectedSTEntries.ForEach(en => transationEntryHashPairList.Add(CreateTransactionEntryHash(en), en));

            return transationEntryHashPairList.Select(m => m.Value).ToList();
        }

        private byte[] CreateTransactionEntryHash(STTransactionEntry transEnty)
        {
            var transEntyByte = transEnty.ToByteArray();

            var hexTransactionEntry = HexByteConvertorExtensions.ToHex(transEntyByte).HexToByteArray();

            var seed = Multihash.Encode<BLAKE2B_256>(RLP.EncodeElement(_previousValidLedgerStateUpdate));

            var merkleSeedInt = Convert.ToInt32(BitConverter.ToInt32(seed.Take(Constants.MerkleTreeFirstStandardBits).ToArray(), 0));

            var random = new Random(merkleSeedInt);
            var salt = new byte[Constants.StandardSaltSize];
            random.NextBytes(salt);

            var transEntryHash = Multihash.Encode<BLAKE2B_256>(ByteUtil.CombineByteArrays(salt, hexTransactionEntry));
            return transEntryHash.ToByteString().ToArray();
        }

        private byte[] CreateLocalHash(byte[] sortedTransationEntryList, byte[] transactionSignatureHash)
        {
            var localhash = Multihash.Encode<BLAKE2B_256>(ByteUtil.CombineByteArrays(sortedTransationEntryList, transactionSignatureHash));

            return localhash.ToByteString().ToArray();
        }


        private IList<Transaction> ValidityCheck(IEnumerable<Transaction> allTransactions)
        {
            //lock time equals 0 or less than ledger cycle time
            //we assume all transactions are of type non-confidential for now
            return allTransactions.Where(m => m.LockTime <= 0 && m.Version == 1).ToList();
        }
    }
}
