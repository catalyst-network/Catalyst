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

using System.Linq;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.UnitTest.TestUtils;
using NSubstitute;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using SharpRepository.InMemoryRepository;
using FluentAssertions;
using LedgerService = Catalyst.Node.Core.Modules.Ledger.Ledger;
using Account = Catalyst.Node.Core.Modules.Ledger.Account;

namespace Catalyst.Node.Core.UnitTest.Modules.Ledger
{
    public sealed class LedgerTests
    {
        private IRepository<Account> _accounts;
        private readonly LedgerService _ledger;

        public LedgerTests()
        {
            _accounts = new InMemoryRepository<Account>();

            var logger = Substitute.For<ILogger>();

            _ledger = new Catalyst.Node.Core.Modules.Ledger.Ledger(_accounts, logger);
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
