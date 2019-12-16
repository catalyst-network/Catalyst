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
using Catalyst.Abstractions.Repository;
using Nethermind.Core;
using Nethermind.Core.Crypto;

namespace Catalyst.Abstractions.Ledger.Models
{
    public class TransactionReceipt : IDocument
    {
        public long Index { get; set; }
        public Keccak DeltaHash { get; set; }
        public long DeltaNumber { get; set; }
        public long GasUsedTotal { get; set; }
        public long GasUsed { get; set; }
        public Address Sender { get; set; }
        public Address Recipient { get; set; }
        public Address ContractAddress { get; set; }
        public byte StatusCode { get; set; }
        public LogEntry[] Logs { get; set; }

        // TODO: replace by correct hash
        public Keccak CalculateHash() => Keccak.Compute(Guid.NewGuid().ToByteArray());

        public string DocumentId => CalculateHash().ToString();
    }
}
