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
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Util;
using Google.Protobuf.WellKnownTypes;
using Multiformats.Hash;
using Nito.Comparers;
using Serilog;

namespace Catalyst.Core.Lib.Modules.Consensus.Deltas
{
    /// <inheritdoc />
    public class DeltaHashProvider : IDeltaHashProvider
    {
        private readonly IDeltaCache _deltaCache;
        private readonly ILogger _logger;

        private readonly ReplaySubject<Multihash> _deltaHashUpdatesSubject;
        private readonly SortedList<Timestamp, Multihash> _hashesByTimeDescending;
        private readonly int _capacity;

        public IObservable<Multihash> DeltaHashUpdates => _deltaHashUpdatesSubject.AsObservable();
        public Multihash GenesisAddress { get; }

        public DeltaHashProvider(IDeltaCache deltaCache, 
            ILogger logger,
            int capacity = 10_000)
        {
            GenesisAddress = Multihash.Parse("oOQCIKFCdksuNerofEzIB4BnQHEDUl9Mr9o9lR5wVMAlutnD");

            _deltaCache = deltaCache;
            _logger = logger;
            _deltaHashUpdatesSubject = new ReplaySubject<Multihash>(0);
            var comparer = ComparerBuilder.For<Timestamp>().OrderBy(u => u, descending: true);
            _capacity = capacity;
            _hashesByTimeDescending = new SortedList<Timestamp, Multihash>(comparer)
            {
                Capacity = _capacity,
            };
            _hashesByTimeDescending.Add(Timestamp.FromDateTime(DateTime.MinValue.ToUniversalTime()), GenesisAddress);
        }

        /// <inheritdoc />
        public bool TryUpdateLatestHash(Multihash previousHash, Multihash newHash)
        {
            _logger.Debug("New hash {hash} received for previous hash {previousHash}", 
                newHash.AsBase64UrlString(), previousHash.AsBase64UrlString());
            var foundNewDelta = _deltaCache.TryGetConfirmedDelta(newHash.AsBase64UrlString(), out var newDelta);
            var foundPreviousDelta = _deltaCache.TryGetConfirmedDelta(previousHash.AsBase64UrlString(), out var previousDelta);

            if (!foundNewDelta 
             || !foundPreviousDelta
             || newDelta.PreviousDeltaDfsHash != previousHash.ToBytes().ToByteString()
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
            _logger.Verbose("Trying to retrieve latest delta as of {asOf}", asOf);
            if (!asOf.HasValue)
            {
                return _hashesByTimeDescending.FirstOrDefault().Value;
            }

            var timestamp = Timestamp.FromDateTime(asOf.Value);
            var hash = _hashesByTimeDescending
               .SkipWhile(p => p.Key > timestamp)
               .FirstOrDefault();

            //todo: do we want to start walking down
            //the history of hashes and get them from IPFS
            //if they are not found here?
            //https://github.com/catalyst-network/Catalyst.Node/issues/615
            return hash.Value;
        }
    }
}
