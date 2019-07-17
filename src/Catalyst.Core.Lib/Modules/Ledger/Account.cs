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

using Catalyst.Common.Config;
using Catalyst.Common.Util;
using SharpRepository.Repository;
using Catalyst.Common.Interfaces.Modules.Ledger;
using System.Text;
using Newtonsoft.Json;

namespace Catalyst.Core.Lib.Modules.Ledger
{
    /// <inheritdoc />
    public sealed class Account : IAccount
    {
        /// <inheritdoc />
        [RepositoryPrimaryKey(Order = 1)]
        [JsonProperty("id")]
        public string PkId => DocumentId;

        /// <inheritdoc />
        public string PublicAddress { get; set; }

        /// <inheritdoc />
        public uint CoinType { get; set; }

        /// <inheritdoc />
        public AccountTypes AccountType { get; set; }

        /// <inheritdoc />
        public BigDecimal Balance { get; set; }

        /// <inheritdoc />
        public byte[] StateRoot { get; set; } = Constants.EmptyTrieHash;

        public string DocumentId => Encoding.UTF8.GetBytes($"{PublicAddress}-{CoinType}-{AccountType.Name}").ToByteString().ToBase64();
        
    }
}
