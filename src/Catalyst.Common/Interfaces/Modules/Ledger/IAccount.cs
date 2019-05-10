using System;
using System.Collections.Generic;
using System.Text;

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
        /// The balance of confirmed transactions.
        /// </summary>
        double AmountConfirmed { get; set; }

        /// <summary>
        /// The balance of unconfirmed transactions.
        /// </summary>
        double AmountUnconfirmed { get; set; }

        /// <summary>
        /// The amount that has enough confirmations to be already spendable.
        /// </summary>
        double SpendableAmount { get; set; }
    }
}
