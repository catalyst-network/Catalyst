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

using System.Linq;
using System.Numerics;
using Catalyst.Abstractions.Ledger.Models;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Json;
using Newtonsoft.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class ReceiptForRpc
    {
        public ReceiptForRpc() { }

        public ReceiptForRpc(Keccak txHash, TransactionReceipt receipt)
        {
            TransactionHash = txHash;
            TransactionIndex = receipt.Index;
            BlockHash = receipt.DeltaHash;
            BlockNumber = receipt.DeltaNumber;
            CumulativeGasUsed = receipt.GasUsedTotal;
            GasUsed = receipt.GasUsed;
            From = receipt.Sender;
            To = receipt.Recipient;
            ContractAddress = receipt.ContractAddress;
            Logs = receipt.Logs.Select((l, idx) => new LogEntryForRpc(receipt, l, idx)).ToArray();
            Status = receipt.StatusCode;
        }

        [JsonConverter(typeof(KeccakConverter))]
        public Keccak TransactionHash { get; set; }
        
        [JsonConverter(typeof(LongConverter))]
        public long TransactionIndex { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak BlockHash { get; set; }
        
        [JsonConverter(typeof(LongConverter))]
        public long BlockNumber { get; set; }
        
        [JsonConverter(typeof(LongConverter))]
        public long CumulativeGasUsed { get; set; }
        
        [JsonConverter(typeof(LongConverter))]
        public long GasUsed { get; set; }
        
        [JsonConverter(typeof(AddressConverter))]
        public Address From { get; set; }
        
        [JsonConverter(typeof(AddressConverter))]
        public Address To { get; set; }
        
        [JsonConverter(typeof(AddressConverter))]
        public Address ContractAddress { get; set; }
        
        public LogEntryForRpc[] Logs { get; set; }

        public BigInteger Status { get; set; }
    }
}
