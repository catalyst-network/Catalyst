#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Int256;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class FilterLog
    {
        public bool Removed { get; }
        public UInt256 LogIndex { get; }
        public long BlockNumber { get; }
        public Hash256 BlockHash { get; }
        public Hash256 TransactionHash { get; }
        public UInt256 TransactionIndex { get; }
        public Address Address { get; }
        public byte[] Data { get; }
        public Hash256[] Topics { get; }

        public FilterLog(long logIndex, TxReceipt txReceipt, LogEntry logEntry)
            : this((UInt256) logIndex, txReceipt, logEntry) { }

        public FilterLog(UInt256 logIndex, TxReceipt txReceipt, LogEntry logEntry)
            : this(
                logIndex,
                txReceipt.BlockNumber,
                txReceipt.BlockHash,
                (UInt256) txReceipt.Index,
                txReceipt.TxHash,
                logEntry.LoggersAddress,
                logEntry.Data,
                logEntry.Topics) { }

        public FilterLog(UInt256 logIndex, long blockNumber, Hash256 blockHash, UInt256 transactionIndex, Hash256 transactionHash, Address address, byte[] data, Hash256[] topics, bool removed = false)
        {
            Removed = removed;
            LogIndex = logIndex;
            BlockNumber = blockNumber;
            BlockHash = blockHash;
            TransactionIndex = transactionIndex;
            TransactionHash = transactionHash;
            Address = address;
            Data = data;
            Topics = topics;
        }
    }
}
