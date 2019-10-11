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

using Catalyst.Core.Modules.Ledger.Models;
using LibP2P;

namespace Catalyst.Core.Modules.Ledger
{
    /// <summary>
    ///     This represents the ledger used to represent and store the state of the Catalyst
    ///     network. It acts a unique source of truth which can be queried to retrieve account
    ///     balances and which can only be updated by Delta / Ledger State Update objects.
    /// </summary>
    public interface ILedger
    {
        /// <summary>
        ///     Saves the new state of a give <see cref="IAccount" /> on the ledger.
        /// </summary>
        /// <remarks>
        ///     This might need to become private if we consider that accounts should only be updated through
        ///     deltas as they emerge from the consensus mechanism.
        /// </remarks>
        /// <param name="account">The new state of the account, to be inserted or updated in the ledger.</param>
        /// <returns><c>true</c> if the update succeeded, <c>false</c> otherwise.</returns>
        bool SaveAccountState(Account account);

        /// <summary>
        ///     Digests the information contained in a Delta object and uses it to update the various accounts involved
        ///     in the transactions it contains. An update will trigger a potentially long running synchronisation if
        ///     the delta received is not immediately following the current <seealso cref="LatestKnownDelta" />.
        /// </summary>
        /// <param name="deltaHash">The address of the delta used to update the ledger on the Dfs.</param>
        void Update(Cid deltaHash);

        /// <summary>
        ///     The latest hash that was process
        /// </summary>
        Cid LatestKnownDelta { get; }

        /// <summary>
        ///     A boolean indicating whether a synchronisation of the ledger is in currently in process.
        /// </summary>
        bool IsSynchonising { get; }
    }
}
