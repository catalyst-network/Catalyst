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
            Assert.That(ledger.Peer, Is.Null);
            Assert.That(ledger.BlocksExchanged, Is.EqualTo(0ul));
            Assert.That(ledger.DataReceived, Is.EqualTo(0ul));
            Assert.That(ledger.DataSent, Is.EqualTo(0ul));
            Assert.That(ledger.DebtRatio, Is.EqualTo(0f));
            Assert.That(ledger.IsInDebt, Is.True);
        }

        [Test]
        public void DebtRatio_Positive()
        {
            var ledger = new BitswapLedger
            {
                DataSent = 1024 * 1024
            };
            Assert.That(ledger.DebtRatio >= 1, Is.True);
            Assert.That(ledger.IsInDebt, Is.False);
        }

        [Test]
        public void DebtRatio_Negative()
        {
            var ledger = new BitswapLedger
            {
                DataReceived = 1024 * 1024
            };
            Assert.That(ledger.DebtRatio < 1, Is.True);
            Assert.That(ledger.IsInDebt, Is.True);
        }
    }
}
