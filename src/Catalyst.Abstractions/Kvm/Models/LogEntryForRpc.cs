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

using Catalyst.Abstractions.Ledger.Models;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Json;
using Newtonsoft.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class LogEntryForRpc
    {
        public LogEntryForRpc() { }

        public LogEntryForRpc(LogEntry logEntry)
        {
            Removed = false;
            Address = logEntry.LoggersAddress;
            Data = logEntry.Data;
            Topics = logEntry.Topics;
        }

        public LogEntryForRpc(TransactionReceipt receipt, LogEntry logEntry, int index)
        {
            Removed = false;
            LogIndex = index;
            TransactionIndex = receipt.Index;
            TransactionHash = receipt.DeltaHash;
            BlockHash = receipt.DeltaHash;
            BlockNumber = receipt.DeltaNumber;
            Address = logEntry.LoggersAddress;
            Data = logEntry.Data;
            Topics = logEntry.Topics;
        }

        public bool? Removed { get; set; }
        
        [JsonConverter(typeof(NullableLongConverter))]
        public long? LogIndex { get; set; }
        
        [JsonConverter(typeof(NullableLongConverter))]
        public long? TransactionIndex { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak TransactionHash { get; set; }
        
        [JsonConverter(typeof(KeccakConverter))]
        public Keccak BlockHash { get; set; }
        
        [JsonConverter(typeof(NullableLongConverter))]
        public long? BlockNumber { get; set; }
        
        [JsonConverter(typeof(AddressConverter))]
        public Address Address { get; set; }
        
        [JsonConverter(typeof(ByteArrayConverter))]
        public byte[] Data { get; set; }
        
        [JsonProperty(ItemConverterType = typeof(KeccakConverter))]
        public Keccak[] Topics { get; set; }
    }
}
