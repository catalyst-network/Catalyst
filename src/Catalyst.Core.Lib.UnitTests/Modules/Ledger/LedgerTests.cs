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

using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using SharpRepository.InMemoryRepository;
using SharpRepository.Repository;
using Xunit;
using LedgerService = Catalyst.Core.Lib.Modules.Ledger.Ledger;
using Account = Catalyst.Core.Lib.Modules.Ledger.Account;

namespace Catalyst.Core.Lib.UnitTests.Modules.Ledger
{
    public sealed class LedgerTests
    {
        private readonly LedgerService _ledger;

        public LedgerTests()
        {
            IRepository<Account, string> accounts = new InMemoryRepository<Account, string>();

            var logger = Substitute.For<ILogger>();

            _ledger = new Catalyst.Core.Lib.Modules.Ledger.Ledger(accounts, logger);
        }

        [Fact]
        public void Save_Account_State_To_Ledger_Repository()
        {
            const int numAccounts = 10;
            for (var i = 0; i < numAccounts; i++)
            {
                var account = AccountHelper.GetAccount(balance: i * 5);
                _ledger.SaveAccountState(account);
            }

            _ledger.Accounts.GetAll().Should().HaveCount(10);
            _ledger.Accounts.GetAll().Should().NotContainNulls();            
        }
    }
}
