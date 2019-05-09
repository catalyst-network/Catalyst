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

using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Interfaces.Modules.Ledger;

namespace Catalyst.Node.Core.Modules.Ledger
{
    /// <summary>
    /// This class represent a user account of which there can be the following types:
    /// confidential account, non-confidential account and smart contract account
    /// </summary>
    public class Account : IAccount
    {
        public int AccountType { get; set; }

        /// <summary>
        /// The balance of confirmed transactions.
        /// </summary>
        public double AmountConfirmed { get; set; }

        /// <summary>
        /// The balance of unconfirmed transactions.
        /// </summary>
        public double AmountUnconfirmed { get; set; }

        /// <summary>
        /// The amount that has enough confirmations to be already spendable.
        /// </summary>
        public double SpendableAmount { get; set; }

    }
}
