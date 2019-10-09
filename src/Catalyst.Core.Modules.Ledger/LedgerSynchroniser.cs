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
using Catalyst.Core.Lib.Extensions;
using Multiformats.Hash;
using Serilog;

namespace Catalyst.Core.Modules.Ledger
{
    /// <inheritdoc />
    public class LedgerSynchroniser : ILedgerSynchroniser
    {
        private readonly ILogger _logger;

        public LedgerSynchroniser(IDeltaCache deltaCache, ILogger logger)
        {
            DeltaCache = deltaCache;
            _logger = logger;
        }

        /// <inheritdoc />
        public IDeltaCache DeltaCache { get; }

        /// <inheritdoc />
        public IEnumerable<Multihash> CacheDeltasBetween(Multihash latestKnownDeltaHash,
            Multihash targetDeltaHash,
            CancellationToken cancellationToken)
        {
            var thisHash = targetDeltaHash;

            do
            {
                if (!DeltaCache.TryGetOrAddConfirmedDelta(thisHash.AsBase32Address(), out var retrievedDelta, cancellationToken))
                {
                    yield break;
                }

                var previousDfsHash = retrievedDelta.PreviousDeltaDfsHash.AsMultihash();

                _logger.Debug("Retrieved delta {previous} as predecessor of {current}",
                    previousDfsHash, thisHash);

                yield return thisHash;

                thisHash = previousDfsHash;
            } while ((thisHash.AsBase32Address() != latestKnownDeltaHash.AsBase32Address())
             && !cancellationToken.IsCancellationRequested);

            yield return thisHash;
        }
    }
}
