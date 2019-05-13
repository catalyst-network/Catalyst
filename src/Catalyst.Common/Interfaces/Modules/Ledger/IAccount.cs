using System;
using System.Collections.Generic;
using System.Text;
using SharpRepository.Repository;
using Catalyst.Common;

namespace Catalyst.Common.Interfaces.Modules.Ledger
{
    /// <summary>
    /// This class represent a user account of which there can be the following types:
    /// confidential account, non-confidential account and smart contract account
    /// </summary>
    public interface IAccount
    {
        /// <summary>Gets or sets the primary key identifier.</summary>
        /// <value>The primary key identifier.</value>
        [RepositoryPrimaryKey(Order = 1)]
        int PkId { get; set; }

        /// <summary>
        /// Gets or sets the public address.
        /// </summary>
        /// <value>
        /// The public address.
        /// </value>
        string PublicAddress { get; set; }

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
        /// Gets or sets the amount.
        /// </summary>
        /// <value>
        /// The amount.
        /// </value>
        decimal Balance { get; set; }

        /// <summary>
        /// Gets or sets the state root.
        /// Encodes the storage contents of the account.
        /// </summary>
        /// <value>
        /// The state root.
        /// </value>
        byte[] StateRoot { get; set; }

    }
}
