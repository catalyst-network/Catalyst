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

using System.Text;
using Catalyst.Abstractions.Types;
using Google.Protobuf;
using Nethermind.Dirichlet.Numerics;
using Newtonsoft.Json;
using SharpRepository.Repository;

namespace Catalyst.Abstractions.Ledger.Models
{
    /// <inheritdoc />
    public sealed class Account : IAccount
    {
        public Account()
        {
            // this constructor only exists to allow SharpRepository
            // to store instances of this class
        }

        public Account(string publicAddress, 
            AccountTypes accountType,
            UInt256 balance = default)
        {
            PublicAddress = publicAddress;
            AccountType = accountType;
            Balance = balance == (UInt256) default ? 0 : balance;
        }

        /// <inheritdoc />
        public string PublicAddress { get; }

        /// <inheritdoc />
        public AccountTypes AccountType { get; }

        /// <inheritdoc />
        public UInt256 Balance { get; set; }

        [RepositoryPrimaryKey(Order = 1)]
        [JsonProperty("id")]
        public string Id =>
            ByteString.CopyFrom(Encoding.UTF8.GetBytes($"{PublicAddress}-{AccountType?.Name}"))?.ToBase64();
    }
}
