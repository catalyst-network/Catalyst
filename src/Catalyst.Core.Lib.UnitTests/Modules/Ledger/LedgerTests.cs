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

using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.Modules.Ledger.Models;
using Catalyst.TestUtils;
using NSubstitute;
using Serilog;
using Xunit;
using LedgerService = Catalyst.Core.Lib.Modules.Ledger.Ledger;

namespace Catalyst.Core.Lib.UnitTests.Modules.Ledger
{
    public sealed class LedgerTests
    {
        private readonly LedgerService _ledger;
        private readonly IAccountRepository _fakeRepository;

        public LedgerTests()
        {
            _fakeRepository = Substitute.For<IAccountRepository>();

            var logger = Substitute.For<ILogger>();
            _ledger = new LedgerService(_fakeRepository, logger);
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

            _fakeRepository.Received(10).Add(Arg.Any<Account>());
        }
    }
}
