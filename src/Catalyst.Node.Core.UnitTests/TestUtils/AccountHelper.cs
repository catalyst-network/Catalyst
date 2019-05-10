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

using Catalyst.Common.Extensions;
using Catalyst.Protocol.Transaction;
using Catalyst.Node.Core.Modules.Ledger;

namespace Catalyst.Common.UnitTests.TestUtils
{
    public static class AccountHelper
    {
        public static Account GetAccount(uint CoinType = 0,
            uint AccountType = 0,
            double AmountConfirmed = 20.3,
            double AmountUnconfirmed = 145.8,
            double SpendableAmount = 20.3)
        {
            var account = new Account()
            {
                AccountType = AccountType,
                AmountConfirmed = AmountConfirmed,
                AmountUnconfirmed = AmountUnconfirmed,
                SpendableAmount = SpendableAmount
            };
            return account;
        }
    }
}
