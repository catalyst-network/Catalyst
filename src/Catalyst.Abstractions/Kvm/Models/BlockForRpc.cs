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
using System.Numerics;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Encoding;
using Nethermind.Core.Extensions;
using Nethermind.Core.Json;
using Nethermind.Dirichlet.Numerics;
using Newtonsoft.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class BlockForRpc
    {
        private readonly BlockDecoder _blockDecoder = new BlockDecoder();

        public BlockForRpc() { }
        
        public BlockForRpc(Block block, bool includeFullTransactionData)
        {
            var isAuRaBlock = block.Header.AuRaSignature != null;
            Author = block.Author;
            Difficulty = block.Difficulty;
            ExtraData = block.ExtraData;
            GasLimit = block.GasLimit;
            GasUsed = block.GasUsed;
            Hash = block.Hash;
            LogsBloom = block.Bloom;
            Miner = block.Beneficiary;
            if (!isAuRaBlock)
            {
                MixHash = block.MixHash;
                Nonce = ((BigInteger) block.Nonce).ToBigEndianByteArray().PadLeft(8);
            }
            else
            {
                Step = block.Header.AuRaStep;
                Signature = block.Header.AuRaSignature;
            }

            Number = block.Number;
            ParentHash = block.ParentHash;
            ReceiptsRoot = block.ReceiptsRoot;
            Sha3Uncles = block.OmmersHash;
            Size = _blockDecoder.GetLength(block, RlpBehaviors.None);
            StateRoot = block.StateRoot;

            Timestamp = block.Timestamp;
            TotalDifficulty = block.TotalDifficulty ?? 0;
            Transactions = includeFullTransactionData
                ? block.Transactions.Select((t, idx) => { return new TransactionForRpc(block.Hash, block.Number, idx, t); }).ToArray()
                : (object[]) block.Transactions.Select(t => t.Hash).AsEnumerable();

            TransactionsRoot = block.TransactionsRoot;
            Uncles = block.Ommers.Select(o => o.Hash);
        }

        [JsonConverter(typeof(AddressConverter))]
        public Address Author { get; set; }
        
        [JsonConverter(typeof(UInt256Converter))]
        public UInt256 Difficulty { get; set; }
        
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] ExtraData { get; set; }
        
        [JsonConverter(typeof(LongConverter))]
        public long GasLimit { get; set; }
        
        [JsonConverter(typeof(LongConverter))]
        public long GasUsed { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak Hash { get; set; }
        
        [JsonConverter(typeof(BloomConverter))]
        public Bloom LogsBloom { get; set; }
        
        [JsonConverter(typeof(AddressConverter))]
        public Address Miner { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak MixHash { get; set; }
        
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Nonce { get; set; }
        
        [JsonConverter(typeof(LongConverter))]
        public long Number { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak ParentHash { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak ReceiptsRoot { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak Sha3Uncles { get; set; }
        
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Signature { get; set; }
        
        [JsonConverter(typeof(LongConverter))]
        public long Size { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak StateRoot { get; set; }
        
        [JsonConverter(typeof(NullableLongConverter))]
        public long? Step { get; set; }
        
        [JsonConverter(typeof(UInt256Converter))]
        public UInt256 TotalDifficulty { get; set; }
        
        [JsonConverter(typeof(UInt256Converter))]
        public UInt256 Timestamp { get; set; }
        
        public IEnumerable<object> Transactions { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak TransactionsRoot { get; set; }
        
        [JsonProperty(ItemConverterType = typeof(KeccakConverter))]
        public IEnumerable<Keccak> Uncles { get; set; }
    }
}
