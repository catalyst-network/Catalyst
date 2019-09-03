#region LICENSE
// 
// Copyright (c) 2019 Catalyst Network
// 
// This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
// 
// Catalyst.Node is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// Catalyst.Node is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
#endregion

using System.Collections.Generic;
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Core.Extensions;
using Catalyst.Protocol.Deltas;
using Multiformats.Hash;
using Serilog;

namespace Catalyst.Core.Ledger
{
    /// <inheritdoc />
    public class LedgerSynchroniser : ILedgerSynchroniser
    {
        private readonly IDeltaCache _deltaCache;
        private readonly ILogger _logger;

        public LedgerSynchroniser(IDeltaCache deltaCache, ILogger logger)
        {
            _deltaCache = deltaCache;
            _logger = logger;
        }

        /// <inheritdoc />
        public IEnumerable<ChainedDeltaHashes> RetrieveDeltasBetween(Multihash latestKnownDeltaHash,
            Multihash targetDeltaHash,
            CancellationToken cancellationToken)
        {
            Multihash nextHash = default;
            var thisHash = targetDeltaHash;

            Delta retrievedDelta = null;
            while ((retrievedDelta == null 
                 || retrievedDelta.PreviousDeltaDfsHash.AsBase32Address() 
                 != latestKnownDeltaHash.AsBase32Address())
             && !cancellationToken.IsCancellationRequested)
            {
                if (_deltaCache.TryGetConfirmedDelta(thisHash, out retrievedDelta))
                {
                    var previousDfsHash = retrievedDelta.PreviousDeltaDfsHash.AsMultihash();

                    var chainedDelta = new ChainedDeltaHashes(previousDfsHash,
                        thisHash, nextHash);

                    _logger.Debug("Retrieved new chained delta {chainedDelta}", chainedDelta);

                    yield return chainedDelta;

                    nextHash = thisHash;
                    thisHash = previousDfsHash;
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}
