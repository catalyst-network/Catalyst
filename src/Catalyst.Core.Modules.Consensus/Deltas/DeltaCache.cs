#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using System.Threading;
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Service;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Kvm;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Lib.P2P;
using Microsoft.Extensions.Caching.Memory;
using Nethermind.Core;
using MultiFormats;
using Nethermind.Db;
using Nethermind.State;
using Serilog;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;

namespace Catalyst.Core.Modules.Consensus.Deltas
{
    /// <inheritdoc cref="IDeltaCache" />
    /// <inheritdoc cref="IDisposable" />
    public class DeltaCache : IDeltaCache, IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDeltaDfsReader _dfsReader;
        private readonly IDeltaIndexService _deltaIndexService;
        private readonly ILogger _logger;
        private readonly Func<MemoryCacheEntryOptions> _entryOptions;
        public Cid GenesisHash { get; set; }

        public static string GetLocalDeltaCacheKey(CandidateDeltaBroadcast candidate) { return nameof(DeltaCache) + "-LocalDelta-" + MultiBase.Encode(candidate.Hash.ToByteArray(), "base32"); }

        public static Address TruffleTestAccount = new("0xb77aec9f59f9d6f39793289a09aea871932619ed");

        public static Address CatalystTruffleTestAccount = new("0x58BeB247771F0B6f87AA099af479aF767CcC0F00");

        public DeltaCache(IHashProvider hashProvider,
            IMemoryCache memoryCache,
            IDeltaDfsReader dfsReader,
            IDeltaCacheChangeTokenProvider changeTokenProvider,
            IStorageProvider storageProvider,
            IStateProvider stateProvider,
            ISnapshotableDb stateDb,
            ISnapshotableDb codeDb,
            IDeltaIndexService deltaIndexService,
            ILogger logger)
        {
            _deltaIndexService = deltaIndexService;
            _dfsReader = dfsReader;
            _logger = logger;
            _entryOptions = () => new MemoryCacheEntryOptions()
               .AddExpirationToken(changeTokenProvider.GetChangeToken())
               .RegisterPostEvictionCallback(EvictionCallback);

            _memoryCache = memoryCache;

            stateProvider.CreateAccount(TruffleTestAccount, 1_000_000_000.Kat());
            stateProvider.CreateAccount(CatalystTruffleTestAccount, 1_000_000_000.Kat());

            storageProvider.Commit();
            stateProvider.Commit(CatalystGenesisSpec.Instance);

            storageProvider.CommitTrees();
            stateProvider.CommitTree();

            stateDb.Commit();
            codeDb.Commit();

            var genesisDelta = new Delta
            {
                TimeStamp = Timestamp.FromDateTime(DateTime.UnixEpoch),
                StateRoot = ByteString.CopyFrom(stateProvider.StateRoot.Bytes),
            };

            GenesisHash = hashProvider.ComputeMultiHash(genesisDelta).ToCid();
            _memoryCache.Set(GenesisHash, genesisDelta);
        }

        private void EvictionCallback(object key, object value, EvictionReason reason, object state) { _logger.Debug("Evicted Delta {0} from cache.", key); }

        /// <inheritdoc />
        public bool TryGetOrAddConfirmedDelta(Cid cid,
            out Delta delta,
            CancellationToken cancellationToken = default)
        {
            //this calls for a TryGetOrCreate IMemoryCache extension function
            if (_memoryCache.TryGetValue(cid, out delta))
            {
                return true;
            }

            if (!_dfsReader.TryReadDeltaFromDfs(cid, out delta, cancellationToken))
            {
                return false;
            }

            _memoryCache.Set(cid, delta, _entryOptions());
            return true;
        }

        public bool TryGetLocalDelta(CandidateDeltaBroadcast candidate, out Delta delta)
        {
            var tryGetLocalDelta = _memoryCache.TryGetValue(GetLocalDeltaCacheKey(candidate), out delta);
            _logger.Verbose("Retrieved full details {delta}", delta?.ToString() ?? "nothing");
            return tryGetLocalDelta;
        }

        public void AddLocalDelta(Cid cid, Delta delta)
        {
            _logger.Verbose("Adding local details of delta with CID {cid}", cid);
            _memoryCache.Set(cid, delta, _entryOptions());
            if (!TryGetOrAddConfirmedDelta(cid, out Delta retrieved, CancellationToken.None))
            {
                throw new InvalidOperationException();
            }
        }

        public void AddLocalDelta(CandidateDeltaBroadcast localCandidate, Delta delta)
        {
            _logger.Verbose("Adding full details of candidate delta {candidate}", localCandidate);
            _memoryCache.Set(GetLocalDeltaCacheKey(localCandidate), delta, _entryOptions());
        }

        protected virtual void Dispose(bool disposing) { _memoryCache.Dispose(); }

        public void Dispose() { Dispose(true); }
    }
}
