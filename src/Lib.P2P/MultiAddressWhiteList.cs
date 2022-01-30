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

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MultiFormats;

namespace Lib.P2P
{
    /// <summary>
    ///   A sequence of filters that are approved.
    /// </summary>
    /// <remarks>
    ///   Only targets that are a subset of any filters will pass.  If no filters are defined, then anything
    ///   passes.
    /// </remarks>
    public class MultiAddressWhiteList : ICollection<MultiAddress>, IPolicy<MultiAddress>
    {
        private ConcurrentDictionary<MultiAddress, MultiAddress> _filters =
            new();

        /// <inheritdoc />
        public bool IsAllowed(MultiAddress target)
        {
            if (_filters.IsEmpty)
                return true;

            return _filters.Any(kvp => Matches(kvp.Key, target));
        }

        private bool Matches(MultiAddress filter, MultiAddress target)
        {
            return filter
               .Protocols
               .All(fp => target.Protocols.Any(tp => tp.Code == fp.Code && tp.Value == fp.Value));
        }

        /// <inheritdoc />
        public bool Remove(MultiAddress item) { return _filters.TryRemove(item, out _); }

        /// <inheritdoc />
        public int Count => _filters.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(MultiAddress item) { _filters.TryAdd(item, item); }

        /// <inheritdoc />
        public void Clear() { _filters.Clear(); }

        /// <inheritdoc />
        public bool Contains(MultiAddress item) { return _filters.Keys.Contains(item); }

        /// <inheritdoc />
        public void CopyTo(MultiAddress[] array, int arrayIndex) { _filters.Keys.CopyTo(array, arrayIndex); }

        /// <inheritdoc />
        public IEnumerator<MultiAddress> GetEnumerator() { return _filters.Keys.GetEnumerator(); }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() { return _filters.Keys.GetEnumerator(); }
    }
}
