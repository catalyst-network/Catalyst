#region LICENSE

// 
// Copyright (c) 2019 Catalyst Network
// 
// This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
// 
// Catalyst.Node is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// Catalyst.Node is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.

#endregion

using System.Numerics;
using Nethermind.Core;
using Nethermind.Core.Crypto;

namespace Catalyst.Core.Modules.Kvm.JsonRpc {
    public class LogEntryForRpc
    {
        public LogEntryForRpc()
        {
        }

        public LogEntryForRpc(LogEntry logEntry)
        {
            Removed = false;
            Address = logEntry.LoggersAddress;
            Data = logEntry.Data;
            Topics = logEntry.Topics;
        }

        public LogEntryForRpc(TxReceipt receipt, LogEntry logEntry, int index)
        {
            Removed = false;
            LogIndex = index;
            TransactionIndex = receipt.Index;
            TransactionHash = receipt.TxHash;
            BlockHash = receipt.BlockHash;
            BlockNumber = receipt.BlockNumber;
            Address = logEntry.LoggersAddress;
            Data = logEntry.Data;
            Topics = logEntry.Topics;
        }

        public bool? Removed { get; set; }
        public BigInteger? LogIndex { get; set; }
        public BigInteger? TransactionIndex { get; set; }
        public Keccak TransactionHash { get; set; }
        public Keccak BlockHash { get; set; }
        public BigInteger? BlockNumber { get; set; }
        public Address Address { get; set; }
        public byte[] Data { get; set; }
        public Keccak[] Topics { get; set; }
    }
}
