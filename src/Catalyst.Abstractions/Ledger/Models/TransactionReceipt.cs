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

using Lib.P2P;
using Nethermind.Core;
using Newtonsoft.Json;
using SharpRepository.Repository;
using System.ComponentModel.DataAnnotations;

namespace Catalyst.Abstractions.Ledger.Models
{
    public class TransactionReceipts
    {
        public TransactionReceipt[] Receipts { get; set; }

        [RepositoryPrimaryKey(Order = 1)]
        [Key]
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class TransactionReceipt
    {
        public long Index { get; set; }
        public Cid DeltaHash { get; set; }
        public long DeltaNumber { get; set; }
        public long GasUsedTotal { get; set; }
        public long GasUsed { get; set; }
        public string Sender { get; set; }
        public string Recipient { get; set; }
        public string ContractAddress { get; set; }
        public byte StatusCode { get; set; }
        public LogEntry[] Logs { get; set; }
    }

    public class TransactionToDelta
    {
        [RepositoryPrimaryKey(Order = 1)]
        [Key]
        [JsonProperty("id")]
        public string Id { get; set; }

        public Cid DeltaHash { get; set; }
    }
}
