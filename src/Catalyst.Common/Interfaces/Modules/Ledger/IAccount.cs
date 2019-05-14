using Catalyst.Common.Util;

namespace Catalyst.Common.Interfaces.Modules.Ledger
{
    public interface IAccount
    {
        /// <summary>
        /// Gets or sets the type of the coin.
        /// </summary>
        /// <value>
        /// The type of the coin.
        /// </value>
        uint CoinType { get; set; }

        /// <summary>
        /// Gets or sets the type of the account.
        /// </summary>
        /// <value>
        /// The type of the account.
        /// </value>
        uint AccountType { get; set; }

        /// <summary>
        /// The amount that has enough confirmations to be already spendable.
        /// </summary>
        BigDecimal Balance { get; set; }
        
        byte[] StateRoot { get; set; }
    }
}
