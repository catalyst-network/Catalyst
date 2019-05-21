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
        public static IDeltaEntity EmptyDeltaEntity { get; } = new DeltaEntity() { Delta = new byte[0], DeltaHash = new byte[0], LocalLedgerState = new byte[0] };
    
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
            Guard.Argument(allTransactions, nameof(allTransactions))
                .NotNull("Mempool content returned null, check the mempool is actively running");

            var allValidatedTransactions = GetValidTransactionsForDelta(allTransactions);

            if (allValidatedTransactions.Any())
            {
                var transactionSignature = allValidatedTransactions.First().Signature;
                var selectedSTEntries = allValidatedTransactions.SelectMany(ste => ste.STEntries).ToList();

                //Sorted O1< O2< ... < Oβ< ... < OM
                var transationEntryListLexiOrder = SortHashByLexiOrder(selectedSTEntries);

                //h∆j = blake2b256(∆Ln,j)
                return CreateDeltaEntity(transationEntryListLexiOrder, transactionSignature);
            }
            return EmptyDeltaEntity;
        }

        private DeltaEntity CreateDeltaEntity(List<STTransactionEntry> transationEntryListLexiOrder, TransactionSignature transactionSignature)
        {
            //L(f/E)
            var transationEntryListByteArray = transationEntryListLexiOrder.SelectMany(lo => lo.ToByteArray()).ToArray();

            //∆Ln,j = L(f/E) + dn
            var deltaState = ByteUtil.CombineByteArrays(transationEntryListByteArray, transactionSignature.ToByteArray());
            
            //hj = h∆j + Idj
            var localHash = Multihash.Encode<BLAKE2B_256>(deltaState);

            //wj = hj + Idj
            var localLedgerStateUpdate = ByteUtil.CombineByteArrays(localHash, _producerUniqueId.PeerId.ToByteArray());

            return new DeltaEntity(){LocalLedgerState = localLedgerStateUpdate, Delta = deltaState, DeltaHash = localHash};
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

            //HEX(Eα)
            var hexTransactionEntry = HexByteConvertorExtensions.ToHex(transEntyByte);

            var seed = Multihash.Encode<BLAKE2B_256>(RLP.EncodeElement(_previousValidLedgerStateUpdate));

            var merkleSeedInt = Convert.ToInt32(BitConverter.ToInt32(seed.Take(Constants.MerkleTreeFirstStandardBits).ToArray(), 0));
            var random = new Random(merkleSeedInt);
   
            //s + HEX(Eα)
            var saltHexConcat = string.Concat(random.Next().ToString(), hexTransactionEntry).ToUtf8ByteString().ToArray();

            // Oα = blake2b256[s + HEX(Eα)]
            var transEntryHash = Multihash.Encode<BLAKE2B_256>(saltHexConcat);
            return transEntryHash;
        }

        public static IList<Transaction> GetValidTransactionsForDelta(IEnumerable<Transaction> allTransactions)
        {
            //lock time equals 0 or less than ledger cycle time
            //we assume all transactions are of type non-confidential for now
            return allTransactions.Where(m => m.LockTime <= 0 && m.Version == 1).ToList();
        }
    }
}
