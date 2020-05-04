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

using Catalyst.Abstractions.Repository;
using Catalyst.Abstractions.Types;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Abstractions.Ledger.Models
{
    /// <summary>
    /// This class represent a user account of which there can be the following types:
    /// confidential account, non-confidential account and smart contract account
    /// </summary>
    public interface IAccount
    {
        /// <summary>
        /// The address used to identify the account.
        /// </summary>
        string PublicAddress { get; }

        /// <summary>
        /// Type of the account: public or confidential.
        /// </summary>
        AccountTypes AccountType { get; }

        /// <summary>
        /// The balance of the account.
        /// </summary>
        UInt256 Balance { get; set; }
    }
}
