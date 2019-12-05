using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MultiFormats;

namespace Lib.P2P
{
    /// <summary>
    ///   A collection of filters that are not approved.
    /// </summary>
    /// <remarks>
    ///   Only targets that do match a filter will pass.
    /// </remarks>
    public class MultiAddressBlackList : ICollection<MultiAddress>, IPolicy<MultiAddress>
    {
        private ConcurrentDictionary<MultiAddress, MultiAddress> filters =
            new ConcurrentDictionary<MultiAddress, MultiAddress>();

        /// <inheritdoc />
        public bool IsAllowed(MultiAddress target) { return !filters.Any(kvp => Matches(kvp.Key, target)); }

        private bool Matches(MultiAddress filter, MultiAddress target)
        {
            return filter
               .Protocols
               .All(fp => target.Protocols.Any(tp => tp.Code == fp.Code && tp.Value == fp.Value));
        }

        /// <inheritdoc />
        public bool Remove(MultiAddress item) { return filters.TryRemove(item, out _); }

        /// <inheritdoc />
        public int Count => filters.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(MultiAddress item) { filters.TryAdd(item, item); }

        /// <inheritdoc />
        public void Clear() { filters.Clear(); }

        /// <inheritdoc />
        public bool Contains(MultiAddress item) { return filters.Keys.Contains(item); }

        /// <inheritdoc />
        public void CopyTo(MultiAddress[] array, int arrayIndex) { filters.Keys.CopyTo(array, arrayIndex); }

        /// <inheritdoc />
        public IEnumerator<MultiAddress> GetEnumerator() { return filters.Keys.GetEnumerator(); }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() { return filters.Keys.GetEnumerator(); }
    }
}
