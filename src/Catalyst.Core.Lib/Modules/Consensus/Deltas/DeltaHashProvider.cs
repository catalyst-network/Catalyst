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
using Multiformats.Base;
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
        public int GetDeltaHashCount => _hashesByTimeDescending.Count;
        public Multihash GenesisAddress { get; }

        public DeltaHashProvider(IDeltaCache deltaCache, 
            ILogger logger,
            int capacity = 10_000)
        {
            GenesisAddress = Multihash.Parse("ydsaeqfmry4m547hwbiasfmgkygthnbynk2hfurmuy4xwnilsuv3tlkt3g5zpoq6oatk5vscg5e6s5yzu4thpm6qv3tk3odvjy6ptzqcwklas");

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

        public string ToFileAddress(Multihash hash)
        {
            return SimpleBase.Base32.Rfc4648.Encode(hash.ToBytes(), false).ToLowerInvariant();
        }

        /// <inheritdoc />
        public bool TryUpdateLatestHash(Multihash previousHash, Multihash newHash)
        {
            var newAddress = ToFileAddress(newHash);
            var previousAddress = ToFileAddress(previousHash);
            _logger.Debug("New hash {hash} received for previous hash {previousHash}", 
                newAddress, previousAddress);
            var foundNewDelta = _deltaCache.TryGetConfirmedDelta(newAddress, out var newDelta);
            var foundPreviousDelta = _deltaCache.TryGetConfirmedDelta(previousAddress, out var previousDelta);

            if (!foundNewDelta 
             || !foundPreviousDelta
             || newDelta.PreviousDeltaDfsHash != previousHash.ToBytes().ToByteString()
             || previousDelta.TimeStamp >= newDelta.TimeStamp)
            {
                _logger.Warning("Failed to update latest hash from {previousHash} to {newHash}",
                    previousAddress, newAddress);
                return false;
            }

            _logger.Debug("Successfully to updated latest hash from {previousHash} to {newHash}",
                previousAddress, newAddress);

            lock (_hashesByTimeDescending)
            {
                try
                {
                    if (!_hashesByTimeDescending.ContainsKey(newDelta.TimeStamp))
                        _hashesByTimeDescending.Add(newDelta.TimeStamp, newAddress);
                    if (_hashesByTimeDescending.Count > _capacity)
                    {
                        _hashesByTimeDescending.RemoveAt(_capacity);
                    }
                }
                catch (Exception) {}
            }
            
            _deltaHashUpdatesSubject.OnNext(newAddress);

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
