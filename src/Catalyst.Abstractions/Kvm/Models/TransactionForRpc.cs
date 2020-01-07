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

using System.Numerics;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class TransactionForRpc
    {
        public Keccak Hash { get; set; }
        public BigInteger? Nonce { get; set; }
        public Keccak BlockHash { get; set; }
        public BigInteger? BlockNumber { get; set; }
        public BigInteger? TransactionIndex { get; set; }
        public Address From { get; set; }
        public Address To { get; set; }
        public UInt256? Value { get; set; }
        public UInt256? GasPrice { get; set; }
        public UInt256? Gas { get; set; }
        public byte[] Data { get; set; }
        public byte[] Input { get; set; }
        public UInt256? V { get; set; }

        public byte[] S { get; set; }

        public byte[] R { get; set; }
    }
}
