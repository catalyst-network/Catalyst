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
using Lib.P2P;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Json;
using Nethermind.Dirichlet.Numerics;
using Newtonsoft.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class BlockForRpc
    {
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

        [JsonConverter(typeof(CidJsonConverter))]
        public Cid Hash { get; set; }
        
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

        [JsonConverter(typeof(CidJsonConverter))]
        public Cid ParentHash { get; set; }
        
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
