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

using System.Text;
using Catalyst.Abstractions.Types;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Config;
using Catalyst.Core.Util;
using Newtonsoft.Json;
using SharpRepository.Repository;

namespace Catalyst.Core.Ledger.Models
{
    /// <inheritdoc />
    public sealed class Account : IAccount
    {
        /// <inheritdoc />
        public string PublicAddress { get; set; }

        /// <inheritdoc />
        public uint CoinType { get; set; }

        /// <inheritdoc />
        public AccountTypes AccountType { get; set; }

        /// <inheritdoc />
        public IBigDecimal Balance { get; set; }

        /// <inheritdoc />
        public byte[] StateRoot { get; set; } = Constants.EmptyTrieHash;

        [RepositoryPrimaryKey(Order = 1)]
        [JsonProperty("id")]
        public string DocumentId =>
            Encoding.UTF8.GetBytes($"{PublicAddress}-{CoinType}-{AccountType?.Name}")
              ?.ToByteString()?.ToBase64();
    }
}
