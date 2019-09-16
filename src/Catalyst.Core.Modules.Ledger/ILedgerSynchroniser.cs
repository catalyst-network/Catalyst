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

using System.Collections.Generic;
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;

namespace Catalyst.Core.Modules.Ledger
{
    public interface ILedgerSynchroniser
    {
        /// <summary>
        /// Starts a process that retrieves the deltas between the <seealso cref="latestKnownDeltaHash"/>
        /// and the <seealso cref="targetDeltaHash"/>, and adds them to the cache.
        /// </summary>
        /// <param name="latestKnownDeltaHash">Hash of the latest known Delta seen on the protocol,
        /// from the point of view of this node.</param>
        /// <param name="targetDeltaHash">The hash of the delta up to which we want to synchronise the ledger.</param>
        /// <param name="cancellationToken">Provides a way to cancel the synchronisation task before it ends.</param>
        IEnumerable<string> CacheDeltasBetween(string latestKnownDeltaHash, string targetDeltaHash, CancellationToken cancellationToken);

        /// <summary>
        /// A cache used to store the full Delta object when a synchronisation is triggered.
        /// </summary>
        IDeltaCache DeltaCache { get; }
    }
}
