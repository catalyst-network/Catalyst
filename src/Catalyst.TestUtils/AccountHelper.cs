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

using Catalyst.Abstractions.Enumerator;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Ledger.Models;
using Catalyst.Core.Util;

namespace Catalyst.TestUtils
{
    public static class AccountHelper
    {
        public static Account GetAccount(BigDecimal balance,
            uint coinType = 0,
            string accountType = "NonConfidentialAccount",
            string publicAddress = "0x32Be343B94f860124dC4fEe278FDCBD38C102D88")
        {
            var account = new Account
            {
                CoinType = coinType,
                AccountType = Enumeration.Parse<AccountTypes>(accountType),
                PublicAddress = publicAddress,
                Balance = balance
            };
            return account;
        }
    }
}
