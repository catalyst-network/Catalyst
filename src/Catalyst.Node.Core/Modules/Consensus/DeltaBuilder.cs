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
using Newtonsoft.Json;
using Nethereum.Hex.HexConvertors.Extensions;
using Catalyst.Common.Extensions;
using Catalyst.Common.Util;
using Google.Protobuf;
using Catalyst.Common.Interfaces.P2P;

namespace Catalyst.Node.Core.Modules.Consensus
{
    /// <inheritdoc />
    public class DeltaBuilder : IDeltaBuilder
    {
        private readonly IMempool _mempool;

        /// <inheritdoc />
        private readonly IPeerIdentifier _producerUniqueId;

        public DeltaBuilder(IMempool mempool, IPeerIdentifier producerUniqueId)
        {
            _mempool = mempool;
            _producerUniqueId = producerUniqueId;
        }

        ///<inheritdoc />
        public IDeltaEntity BuildDelta()
        {
            var allTransactions = _mempool.GetMemPoolContent();

            var allValidatedTransactions = ValidityCheck(allTransactions);
            var transactionSignture = allValidatedTransactions.FirstOrDefault().Signature;

            var selectedSTEntries = allValidatedTransactions.Select(ste => ste.STEntries).FirstOrDefault().ToList();

            var transationEntryListLexiOrder = SortHashByLexiOrder(selectedSTEntries);

            var masterByteArray = transationEntryListLexiOrder.SelectMany(lo => lo.ToByteArray()).ToArray();
            var deltaState = string.Concat(masterByteArray, transactionSignture.ToByteArray()).ToUtf8ByteString().ToArray();

            var localHash = CreateLocalHash(masterByteArray, transactionSignture.ToByteArray());

            var localLedgerStateUpdate = string.Concat(localHash, _producerUniqueId.PeerId.ToByteArray()).ToUtf8ByteString().ToArray();

            return new DeltaEntity() { LocalLedgerState = localLedgerStateUpdate, Delta = deltaState, DeltaHash = localHash };
        }

        private List<STTransactionEntry> SortHashByLexiOrder(List<STTransactionEntry> selectedSTEntries)
        {
            var transationEntryHashPairList = new SortedDictionary<byte[], STTransactionEntry>();

            selectedSTEntries.ForEach(en => transationEntryHashPairList.Add(CreateTransactionEntryHash(en), en));

            return transationEntryHashPairList.Select(m => m.Value).ToList();
        }

        private SortedDictionary<byte[], STTransactionEntry> GenerateHashPairingA(List<STTransactionEntry> selectedSTEntries)
        {
            var transationEntryHashPairList = new SortedDictionary<byte[], STTransactionEntry>();

            selectedSTEntries.ForEach(en => transationEntryHashPairList.Add(CreateTransactionEntryHash(en), en));

            return transationEntryHashPairList;
        }

        private byte[] CreateTransactionEntryHash(STTransactionEntry transEnty)
        {
            var transEntyByte = transEnty.ToByteArray();

            var hexTransactionEntry = HexByteConvertorExtensions.ToHex(transEntyByte);

            //salt code outstanding
            var transEntryHash = Multihash.Encode<BLAKE2B_256>(string.Concat("salt", hexTransactionEntry).ToUtf8ByteString().ToByteArray());

            return transEntryHash.ToByteString().ToArray();
        }

        private byte[] CreateLocalHash(byte[] sortedTransationEntryList, byte[] transactionSignatureHash)
        {
            var localhash = Multihash.Encode<BLAKE2B_256>(string.Concat(sortedTransationEntryList, transactionSignatureHash).ToUtf8ByteString().ToByteArray());

            return localhash.ToByteString().ToArray();
        }


        private IList<Transaction> ValidityCheck(IEnumerable<Transaction> allTransactions)
        {
            //lock time equals 0 or less than ledger cycle time
            //we assume all transactions are of type  for now
            return allTransactions.Where(m => m.LockTime == 9876 && m.Version == 1).ToList();
        }
    }
}
