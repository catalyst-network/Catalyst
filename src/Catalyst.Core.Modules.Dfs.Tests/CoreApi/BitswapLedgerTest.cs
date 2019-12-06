using Catalyst.Abstractions.Dfs.CoreApi;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.CoreApi
{
    public class BitswapLedgerTest
    {
        [Fact]
        public void Defaults()
        {
            var ledger = new BitswapLedger();
            Assert.Null(ledger.Peer);
            Assert.Equal(0ul, ledger.BlocksExchanged);
            Assert.Equal(0ul, ledger.DataReceived);
            Assert.Equal(0ul, ledger.DataSent);
            Assert.Equal(0f, ledger.DebtRatio);
            Assert.True(ledger.IsInDebt);
        }

        [Fact]
        public void DebtRatio_Positive()
        {
            var ledger = new BitswapLedger
            {
                DataSent = 1024 * 1024
            };
            Assert.True(ledger.DebtRatio >= 1);
            Assert.False(ledger.IsInDebt);
        }

        [Fact]
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
