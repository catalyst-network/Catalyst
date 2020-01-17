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

using LibP2P;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Json;
using Nethermind.Dirichlet.Numerics;
using Newtonsoft.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class TransactionForRpc
    {
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak Hash { get; set; }

        [JsonConverter(typeof(NullableUInt256Converter))]
        public UInt256? Nonce { get; set; }

        [JsonConverter(typeof(CidJsonConverter))]
        public Cid BlockHash { get; set; }

        [JsonConverter(typeof(NullableUInt256Converter))]
        public UInt256? BlockNumber { get; set; }

        [JsonConverter(typeof(NullableUInt256Converter))]
        public UInt256? TransactionIndex { get; set; }

        [JsonConverter(typeof(AddressConverter))]
        public Address From { get; set; }

        [JsonConverter(typeof(AddressConverter))]
        public Address To { get; set; }

        [JsonConverter(typeof(NullableUInt256Converter))]
        public UInt256? Value { get; set; }

        [JsonConverter(typeof(NullableUInt256Converter))]
        public UInt256? GasPrice { get; set; }

        [JsonConverter(typeof(NullableUInt256Converter))]
        public UInt256? Gas { get; set; }

        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data { get; set; }

        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Input { get; set; }

        [JsonConverter(typeof(NullableUInt256Converter))]
        public UInt256? V { get; set; }

        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] S { get; set; }

        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] R { get; set; }
    }
}
