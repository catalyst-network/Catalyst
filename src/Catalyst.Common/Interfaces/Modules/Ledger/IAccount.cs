using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Common.Interfaces.Modules.Ledger
{
    public interface IAccount
    {
        int AccountType { get; set; }

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
