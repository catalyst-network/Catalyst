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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Multiformats.Hash;
using Nito.Comparers;
using Serilog;

namespace Catalyst.Node.Core.Modules.Consensus.Delta
{
    /// <inheritdoc />
    public class DeltaHashProvider : IDeltaHashProvider
    {
        private readonly IDeltaCache _deltaCache;
        private readonly ILogger _logger;

        private readonly ReplaySubject<Multihash> _deltaHashUpdatesSubject;
        private readonly SortedList<uint, Multihash> _hashesByTimeDescending;
        private readonly int _capacity;

        public IObservable<Multihash> DeltaHashUpdates => _deltaHashUpdatesSubject.AsObservable();

        public DeltaHashProvider(IDeltaCache deltaCache, 
            ILogger logger,
            int capacity = 10_000)
        {
            _deltaCache = deltaCache;
            _logger = logger;
            _deltaHashUpdatesSubject = new ReplaySubject<Multihash>(0);
            var comparer = ComparerBuilder.For<uint>().OrderBy(u => u, descending: true);
            _capacity = capacity;
            _hashesByTimeDescending = new SortedList<uint, Multihash>(comparer)
            {
                Capacity = _capacity,
            };
        }

        /// <inheritdoc />
        public bool TryUpdateLatestHash(Multihash previousHash, Multihash newHash)
        {
            var foundNewDelta = _deltaCache.TryGetDelta(newHash, out var newDelta);
            var foundPreviousDelta = _deltaCache.TryGetDelta(previousHash, out var previousDelta);

            if (!foundNewDelta 
             || !foundPreviousDelta
             || newDelta.PreviousDeltaDfsHash.ToMultihashString() != previousHash
             || previousDelta.TimeStamp >= newDelta.TimeStamp)
            {
                _logger.Warning("Failed to update latest hash from {previousHash} to {newHash}",
                    previousHash, newHash);
                return false;
            }

            _logger.Debug("Successfully to updated latest hash from {previousHash} to {newHash}",
                previousHash, newHash);

            lock (_hashesByTimeDescending)
            {
                _hashesByTimeDescending.Add(newDelta.TimeStamp, newHash);
                if (_hashesByTimeDescending.Count > _capacity)
                {
                    _hashesByTimeDescending.RemoveAt(_capacity);
                }
            }
            
            _deltaHashUpdatesSubject.OnNext(newHash);

            return true;
        }

        /// <inheritdoc />
        public Multihash GetLatestDeltaHash(DateTime? asOf = null)
        {
            if (!asOf.HasValue)
            {
                return _hashesByTimeDescending.FirstOrDefault().Value;
            }

            var dateTimeAsUint = (uint) asOf.Value.ToOADate();
            var hash = _hashesByTimeDescending
               .SkipWhile(p => p.Key > dateTimeAsUint)
               .FirstOrDefault();

            //todo: do we want to start walking down
            //the history of hashes and get them from IPFS
            //if they are not found here?
            //https://github.com/catalyst-network/Catalyst.Node/issues/615
            return hash.Value;
        }
    }
}
