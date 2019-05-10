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
using System.Linq;
using System.Threading;
using Catalyst.Common.Interfaces.Modules.Mempool;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using SharpRepository.Repository;
using Xunit;
using Catalyst.Node.Core.Modules.Ledger;
using SharpRepository.InMemoryRepository;

namespace Catalyst.Node.Core.UnitTest.Modules.Mempool
{
    public sealed class LedgerTests
    {
        private IRepository<Account> _accounts;
        private readonly Ledger _ledger;

        public LedgerTests()
        {
            _accounts = Substitute.For<IRepository<Account>>();

            var logger = Substitute.For<ILogger>();

            _ledger = new Ledger(_accounts, logger);
        }

        [Fact]
        public void Save_Account_State_To_Ledger_Repository()
        {
            const int numAccounts = 10;
            for (var i = 0; i < numAccounts; i++)
            {
                var account = AccountHelper.GetAccount(AmountConfirmed: (uint) i * 5);
                _ledger.SaveAccountState(account);
            }

            _ledger.Accounts.ReceivedCalls().Count().Should().Be(11);
        }
    }
}
