#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Abstractions.Dfs.CoreApi;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class BitswapLedgerTest
    {
        [Test]
        public void Defaults()
        {
            var ledger = new BitswapLedger();
            Assert.Null(ledger.Peer);
            Assert.AreEqual(0ul, ledger.BlocksExchanged);
            Assert.AreEqual(0ul, ledger.DataReceived);
            Assert.AreEqual(0ul, ledger.DataSent);
            Assert.AreEqual(0f, ledger.DebtRatio);
            Assert.True(ledger.IsInDebt);
        }

        [Test]
        public void DebtRatio_Positive()
        {
            var ledger = new BitswapLedger
            {
                DataSent = 1024 * 1024
            };
            Assert.True(ledger.DebtRatio >= 1);
            Assert.False(ledger.IsInDebt);
        }

        [Test]
        public void DebtRatio_Negative()
        {
            var ledger = new BitswapLedger
            {
                DataReceived = 1024 * 1024
            };
            Assert.True(ledger.DebtRatio < 1);
            Assert.True(ledger.IsInDebt);
        }
    }
}
