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
using Catalyst.Common.Interfaces.Modules.Consensus.Delta;
using Catalyst.Common.Interfaces.Modules.Dfs;

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
            throw new NotImplementedException();
            //_hashesByTimeDescending.ContainsKey();
        }

        /// <inheritdoc />
        public string GetLatestDeltaHash(DateTime? asOf = null) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public IObservable<string> DeltaHashUpdates { get; }
    }
}
