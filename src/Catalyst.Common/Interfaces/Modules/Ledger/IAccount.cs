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


using SharpRepository.Repository;
using System.Numerics;


namespace Catalyst.Common.Interfaces.Modules.Ledger
{
    /// <summary>
    /// This class represent a user account of which there can be the following types:
    /// confidential account, non-confidential account and smart contract account
    /// </summary>
    public interface IAccount
    {
        /// <summary>Gets or sets the primary key identifier.</summary>
        /// <value>The primary key identifier.</value>
        [RepositoryPrimaryKey(Order = 1)]
        int PkId { get; set; }

        /// <summary>
        /// Gets or sets the public address.
        /// </summary>
        /// <value>
        /// The public address.
        /// </value>
        string PublicAddress { get; set; }

        /// <summary>
        /// Gets or sets the type of the coin.
        /// </summary>
        /// <value>
        /// The type of the coin.
        /// </value>
        uint CoinType { get; set; }

        /// <summary>
        /// Gets or sets the type of the account.
        /// </summary>
        /// <value>
        /// The type of the account.
        /// </value>
        uint AccountType { get; set; }

        /// <summary>
        /// Gets or sets the amount.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        BigInteger Balance { get; set; }

        /// <summary>
        /// Gets or sets the state root.
        /// Encodes the storage contents of the account.
        /// </summary>
        /// <value>
        /// The state root.
        /// </value>
        byte[] StateRoot { get; set; }

    }
}
