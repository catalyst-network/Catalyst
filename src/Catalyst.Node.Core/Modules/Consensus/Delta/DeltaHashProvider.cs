using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Microsoft.Extensions.Caching.Memory;

namespace Catalyst.Node.Core.Modules.Consensus.Delta
{
    /// <inheritdoc />
    public class DeltaHashProvider : IDeltaHashProvider
    {
        private readonly IDfs _dfs;
        private readonly IDeltaCache _deltaCache;
        private const string LatestDeltaKey = "LatestDelta";
        private readonly SortedList<DateTime, string> _hashesByTimeDescending;

        private readonly object _latestDeltaLock = new object();

        private class TimeStampedHash
        {
            public DateTime TimeStamp { get; }
            public string Hash { get; }

            public TimeStampedHash(string hash, DateTime timeStamp)
            {
                Hash = hash;
                TimeStamp = timeStamp;
            }
        }

        public DeltaHashProvider(IDfs dfs, IDeltaCache deltaCache)
        {
            _dfs = dfs;
            _deltaCache = deltaCache;
            _hashesByTimeDescending = new SortedList<DateTime, string>();
        }

        // <inheritdoc />
        public bool TryUpdateLatestHash(string previousHash, string newHash)    
        {
            _hashesByTimeDescending.ContainsKey()
        }

        /// <inheritdoc />
        public string GetLatestDeltaHash(DateTime? asOf = null) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public IObservable<string> DeltaHashUpdates { get; }
    }
}
